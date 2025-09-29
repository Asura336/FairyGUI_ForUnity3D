// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "FairyGUI/SDRect"
{
    Properties
    {
        _MainTex ("Base (RGB), Alpha (A)", 2D) = "black" {}

        [MainColor] _Color ("[RGBA] base color", Color) = (1,1,1,1)
        _EdgeColor ("[RGBA] edge color", Color) = (1,1,1,1)
        [Toggle] _EdgeSolid ("edge is solid color, 1 or 0", float) = 0
        _Scale ("scale of texture", Vector) = (1, 1, 0, 0)
        _EdgeWidth("edge width", float) = 0.1
        _Bounds("center and extends", Vector) = (0, 0, 0.5, 0.5)
        _RectRadius("radius of rect", Vector) = (0, 0, 0, 0)

        _ClipBox("ClipBox for SRP batcher, let them happy :)", Vector) = (-2, -2, 0, 0)
        _ClipSoftness("ClipSoftness for SRP batcher, let them happy :)", Vector) = (-2, -2, 0, 0)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        _BlendSrcFactor ("Blend SrcFactor", Float) = 5
        _BlendDstFactor ("Blend DstFactor", Float) = 10
    }
    
    SubShader
    {
        LOD 100

        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Fog { Mode Off }
        Blend [_BlendSrcFactor] [_BlendDstFactor], One One
        ColorMask [_ColorMask]

        Pass
        {
            Name "Unlit"
            CGPROGRAM
            #pragma multi_compile NOT_COMBINED COMBINED
            #pragma multi_compile NOT_GRAYED GRAYED COLOR_FILTER
            #pragma multi_compile NOT_CLIPPED CLIPPED SOFT_CLIPPED ALPHA_MASK
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            #include "FairyGUI-SDFEdgeAA.hlsl"
    
            struct appdata_t
            {
                float4 vertex : POSITION;
                //fixed4 color : COLOR;
                float4 texcoord : TEXCOORD0;
            };
    
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                fixed4 edgeColor : COLOR1;
                float4 texcoord : TEXCOORD0;

                #ifdef CLIPPED
                float2 clipPos : TEXCOORD1;
                #endif

                #ifdef SOFT_CLIPPED
                float2 clipPos : TEXCOORD1;
                #endif
            };

            sampler2D _MainTex;

            #ifdef COMBINED
            sampler2D _AlphaTex;
            #endif

            CBUFFER_START(UnityPerMaterial)
            fixed4 _Color;

            // 距离场圆角矩形，可选边缘
            float4 _Bounds;  // 图片像素尺寸为 xy, (0, 0, x, y)
            float4 _RectRadius;  // 圆角半径 => 图片像素尺寸为 xy, 圆角半径像素为 r, 值为 r / y 或者 r / x, 取决于 x 更大或者 y 更大
            float2 _Scale;  // uv 的乘数 => 图片像素尺寸为 xy, 值为 (x/y, 1) 或者 (1, y/x), 取决于 x 更大或者 y 更大
            fixed4 _EdgeColor;  // 边缘颜色
            float _EdgeWidth;  // 边缘宽度 => 边缘像素宽度 / 图片像素尺寸 xy 较小者
            float _EdgeSolid;  // 边缘是实色 => 1 或者 0, 使用 _EdgeColor.a 或者乘上使 alpha 沿距离减淡的因数

            //#ifdef CLIPPED
            //float4 _ClipBox = float4(-2, -2, 0, 0);
            //#endif

            //#ifdef SOFT_CLIPPED
            float4 _ClipBox = float4(-2, -2, 0, 0);
            float4 _ClipSoftness = float4(0, 0, 0, 0);
            //#endif
            CBUFFER_END

            #ifdef COLOR_FILTER
            float4x4 _ColorMatrix;
            float4 _ColorOffset;
            float _ColorOption = 0;
            #endif

            inline float sdRoundedBox(in float2 p, in float2 b, in float4 r)
            {
                r.xy = (p.x > 0.0) ? r.xy : r.zw;
                r.x = (p.y > 0.0) ? r.x : r.y;
                float2 q = abs(p) - b + r.x;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
            }

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                //#if !defined(UNITY_COLORSPACE_GAMMA) && (UNITY_VERSION >= 550)
                //o.color.rgb = GammaToLinearSpace(v.color.rgb);
                //o.color.a = v.color.a;
                //#else
                //o.color = v.color;
                //#endif
                o.color = _Color;
                o.edgeColor = _EdgeColor;

                #ifdef CLIPPED
                o.clipPos = mul(unity_ObjectToWorld, v.vertex).xy * _ClipBox.zw + _ClipBox.xy;
                #endif

                #ifdef SOFT_CLIPPED
                o.clipPos = mul(unity_ObjectToWorld, v.vertex).xy * _ClipBox.zw + _ClipBox.xy;
                #endif

                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.texcoord.xy / i.texcoord.w) * i.color;

                //--------
                // 描述距离场圆角矩形
                float2 center = _Bounds.xy;
                float2 extends = _Bounds.zw;
                float2 p = i.texcoord.xy + center - 0.5;
                float dis = sdRoundedBox(p * _Scale, extends, _RectRadius);
                col = edgeColor(dis, _EdgeWidth, col, i.edgeColor, _EdgeSolid);


                #ifdef COMBINED
                col.a *= tex2D(_AlphaTex, i.texcoord.xy / i.texcoord.w).g;
                #endif

                #ifdef GRAYED
                fixed grey = dot(col.rgb, fixed3(0.299, 0.587, 0.114));
                col.rgb = fixed3(grey, grey, grey);
                #endif

                #ifdef SOFT_CLIPPED
                float2 factor = float2(0,0);
                if(i.clipPos.x<0)
                    factor.x = (1.0-abs(i.clipPos.x)) * _ClipSoftness.x;
                else
                    factor.x = (1.0-i.clipPos.x) * _ClipSoftness.z;
                if(i.clipPos.y<0)
                    factor.y = (1.0-abs(i.clipPos.y)) * _ClipSoftness.w;
                else
                    factor.y = (1.0-i.clipPos.y) * _ClipSoftness.y;
                col.a *= clamp(min(factor.x, factor.y), 0.0, 1.0);
                #endif

                #ifdef CLIPPED
                float2 factor = abs(i.clipPos);
                col.a *= step(max(factor.x, factor.y), 1);
                #endif

                #ifdef COLOR_FILTER
                if (_ColorOption == 0)
                {
                    fixed4 col2 = col;
                    col2.r = dot(col, _ColorMatrix[0]) + _ColorOffset.x;
                    col2.g = dot(col, _ColorMatrix[1]) + _ColorOffset.y;
                    col2.b = dot(col, _ColorMatrix[2]) + _ColorOffset.z;
                    col2.a = dot(col, _ColorMatrix[3]) + _ColorOffset.w;
                    col = col2;
                }
                else //premultiply alpha
                    col.rgb *= col.a;
                #endif

                // 距离场材质一定使用透明度裁剪
                //#ifdef ALPHA_MASK
                clip(col.a - 0.001);
                //#endif

                return col;
            }
            ENDCG
        }
    }
}
