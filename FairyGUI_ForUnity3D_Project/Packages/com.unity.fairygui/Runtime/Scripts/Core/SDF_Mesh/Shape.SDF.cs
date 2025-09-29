#pragma warning disable IDE1006
#define FAIRYGUI_SDF
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;
using static FairyGUI.SDFShapeConstants;

namespace FairyGUI
{
    partial class Shape
    {
        public static bool UseSDFShape
        {
            get
            {
#if FAIRYGUI_SDF
                return true;
#else 
                return false;
#endif
            }
        }

        public bool isSDFShape { get; private set; } = false;
        /// <summary>
        /// 如果 SDF 形状生效，指示使用软边缘
        /// </summary>
        public bool sdf_softEdge { get; private set; } = false;
        /// <summary>
        /// SDF 图元的更新机制和原生的不太一样，让 NGraphics 每次轮询都触发检查 alpha 的回调，在这里缓存一份 alpha
        /// </summary>
        public float sdf_alpha { get; private set; } = -1;

        public override string shader
        {
            get => base.shader;
            set
            {
                // 从外部设置着色器，需要考虑从 SDF 图元退化，因为 SDF 材质本身对 FGUI 来说就是一种自定义材质
                if (graphics != null)
                {
                    FallbackSDF();
                    //graphics.shader = value;
                    EnsureMatByShaderName(graphics, value);
                }
            }
        }

        /// <summary>
        /// 从 SDF 图元退化，假设大小有效
        /// </summary>
        public void FallbackSDF()
        {
            if (!isSDFShape) { return; }

            var fillColor = graphics.color;
            switch (graphics.meshFactory)
            {
                case SDFRectMesh sdfRect:
                {
                    // to Rect
                    var lineWidth = sdfRect.lineWidth;
                    var lineColor = sdfRect.lineColor;
                    var radiusTL = sdfRect.topLeftRadius;
                    var radiusTR = sdfRect.topRightRadius;
                    var radiusBL = sdfRect.bottomLeftRadius;
                    var radiusBR = sdfRect.bottomRightRadius;

                    if (radiusTL == 0 && radiusTR == 0 && radiusBL == 0 && radiusBR == 0)
                    {
                        DrawRect(lineWidth, lineColor, fillColor);
                    }
                    else
                    {
                        DrawRoundRect(lineWidth, lineColor, fillColor,
                            radiusTL, radiusTR, radiusBL, radiusBR);
                    }
                }
                break;
                case SDFEllipseMesh sdfEllipse:
                {
                    // to Ellipse
                    var lineWidth = sdfEllipse.lineWidth;
                    var lineColor = sdfEllipse.lineColor;
                    var centerColor = sdfEllipse.centerColor;
                    var startDegree = sdfEllipse.startDegree;
                    var endDegree = sdfEllipse.endDegreee;

                    DrawEllipse(lineWidth,
                        centerColor ?? fillColor, lineColor, fillColor,
                        startDegree, endDegree);
                }
                break;
                default: throw new System.NotImplementedException();
            }

            // Finally:
            graphics.UpdateMesh();
            Assert.IsFalse(isSDFShape);
            Assert.AreEqual(ShaderConfig.imageShader, graphics.shader);
            graphics.SetMeshDirty();
        }


        private static void EnsureMatByShaderName(NGraphics graphics, string sdfShaderName)
        {
            if (graphics.shader != sdfShaderName)
            {
                graphics.shader = sdfShaderName;
                // 设置 SDF 材质时需要一个图元持有一个材质，不可以共享
                // 这么做在 URP 并无问题，对 BRP 来说可能增加性能负担
                graphics.material = new Material(ShaderConfig.Get(sdfShaderName));
            }
        }

        /// <summary>
        /// 距离场着色器的实现有计算精度问题，直接写入颜色时要修正颜色以实现正确的视觉效果
        /// </summary>
        /// <param name="fillColor"></param>
        /// <param name="lineColor"></param>
        /// <param name="edgePixelWidth"></param>
        private static void EnsureRingColor(ref Color fillColor, ref Color lineColor, float edgePixelWidth)
        {
            float rawFillColorAlpla = fillColor.a, rawLineColorAlpha = lineColor.a;
            if (rawFillColorAlpla < 1e-3f && rawLineColorAlpha < 1e-3f)
            {
                fillColor = Color.clear;
                lineColor = Color.clear;
                return;
            }

            if (edgePixelWidth < 1e-3f)
            {
                // 没有边缘
                set_rgba(ref lineColor, fillColor, 0);
            }
            else
            {
                // 有边缘
                if (rawFillColorAlpla < 1e-3f)
                {
                    // 只有边缘，内部全透明
                    set_rgba(ref fillColor, lineColor, 0);
                }
                else if (rawLineColorAlpha < 1e-3f)
                {
                    // 有边缘但边缘透明
                    set_rgba(ref lineColor, fillColor, 0);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void set_rgba(ref Color dst, in Color rgb, float a)
            {
                dst.r = rgb.r;
                dst.g = rgb.g;
                dst.b = rgb.b;
                dst.a = a;
            }
        }

        /// <summary>
        /// 使用距离场材质描述的圆角矩形，生成更少的顶点，兼容 SRP-Batching
        /// </summary>
        /// <param name="lineSize"></param>
        /// <param name="lineColor"></param>
        /// <param name="fillColor"></param>
        /// <param name="topLeftRadius"></param>
        /// <param name="topRightRadius"></param>
        /// <param name="bottomLeftRadius"></param>
        /// <param name="bottomRightRadius"></param>
        public void DrawSDFRoundRect(float lineSize, in Color lineColor, in Color fillColor,
            float topLeftRadius, float topRightRadius, float bottomLeftRadius, float bottomRightRadius,
            bool softEdge = false)
        {
            var mesh = graphics.GetMeshFactory<SDFRectMesh>();
            mesh.lineWidth = lineSize;
            mesh.lineColor = lineColor;
            mesh.fillColor = fillColor;
            mesh.topLeftRadius = topLeftRadius;
            mesh.topRightRadius = topRightRadius;
            mesh.bottomLeftRadius = bottomLeftRadius;
            mesh.bottomRightRadius = bottomRightRadius;

            graphics.color = fillColor;
            EnsureMatByShaderName(graphics, sdfRectShader);
            isSDFShape = true;
            graphics.SetMeshDirty();

            // update properties
            sdf_softEdge = softEdge;

            var _lineColor = lineColor; _lineColor.a *= alpha;
            var _fillColor = fillColor; _fillColor.a *= alpha;
            EnsureRingColor(ref _fillColor, ref _lineColor, lineSize);
            SDFRectMesh.ApplyMaterialProperties(graphics.material, size, lineSize,
                _lineColor, _fillColor,
                topLeftRadius, topRightRadius, bottomLeftRadius, bottomRightRadius, sdf_softEdge);

            graphics.sdf_onBeforePopulateMesh -= OnBeforePopulateMesh_sdfRect;
            graphics.sdf_onBeforePopulateMesh += OnBeforePopulateMesh_sdfRect;
            graphics.sdf_onChangeAlpha -= OnChangeAlpha_sdfRect;
            graphics.sdf_onChangeAlpha += OnChangeAlpha_sdfRect;

            sdf_alpha = -1;  // as dirty flag
        }
        void OnChangeAlpha_sdfRect(NGraphics graphics, float alpha)
        {
            var meshFactory = graphics.meshFactory;
            if (meshFactory is SDFRectMesh sdfRect)
            {
                if (sdf_alpha != alpha)
                {
                    sdf_alpha = alpha;

                    var fillColor = graphics.color; fillColor.a *= alpha;
                    var lineColor = (Color)sdfRect.lineColor; lineColor.a *= alpha;
                    EnsureRingColor(ref fillColor, ref lineColor, sdfRect.lineWidth);
                    graphics.material.SetColor(k_Color, fillColor);
                    graphics.material.SetColor(k_EdgeColor, lineColor);
                }
            }
            else
            {
                // pass
                // restore
                graphics.sdf_onChangeAlpha -= OnChangeAlpha_sdfRect;
            }
        }
        void OnBeforePopulateMesh_sdfRect(NGraphics graphics)
        {
            var meshFactory = graphics.meshFactory;
            if (meshFactory is SDFRectMesh sdfRect)
            {
                EnsureMatByShaderName(graphics, sdfRectShader);
                var fillColor = graphics.color; fillColor.a *= alpha;
                var lineColor = (Color)sdfRect.lineColor; lineColor.a *= alpha;
                EnsureRingColor(ref fillColor, ref lineColor, sdfRect.lineWidth);
                // 圆角矩形的属性应用在了材质上，需要一个元件带一个材质
                // 对 URP 来说有效，如果是 BRP 应该用材质属性块（但这么做基本上一定会中断合批）
                SDFRectMesh.ApplyMaterialProperties(graphics.material, this.size, sdfRect.lineWidth,
                    lineColor, fillColor,
                    sdfRect.topLeftRadius, sdfRect.topRightRadius, sdfRect.bottomLeftRadius, sdfRect.bottomRightRadius,
                    sdf_softEdge);
            }
            else
            {
                // restore
                graphics.shader = ShaderConfig.imageShader;  // NGraphics 的缺省着色器
                graphics.material = null;  // 这里不能用 Graphics::SetMaterial
                graphics.sdf_onBeforePopulateMesh -= OnBeforePopulateMesh_sdfRect;
                isSDFShape = false;
            }
        }

        /// <summary>
        /// 使用距离场材质描述的简单椭圆，生成更少的顶点，无法处理中心颜色和填充角度
        /// </summary>
        /// <param name="size"></param>
        /// <param name="lineSize"></param>
        /// <param name="lineColor"></param>
        /// <param name="fillColor"></param>
        public void DrawSDFSimpleEllipse(float lineSize,
            in Color centerColor, in Color lineColor, in Color fillColor,
            float startDegree, float endDegree,
            bool softEdge = false)
        {
            var mesh = graphics.GetMeshFactory<SDFEllipseMesh>();
            mesh.lineWidth = lineSize;
            mesh.centerColor = centerColor.Equals(fillColor) ? null : centerColor;
            mesh.lineColor = lineColor;
            mesh.fillColor = fillColor;
            mesh.startDegree = startDegree;
            mesh.endDegreee = endDegree;

            graphics.color = fillColor;
            EnsureMatByShaderName(graphics, sdfSimpleEllipseShader);
            isSDFShape = true;
            graphics.SetMeshDirty();

            // update properties
            this.sdf_softEdge = softEdge;
            var _lineColor = lineColor; _lineColor.a *= alpha;
            var _fillColor = fillColor; _fillColor.a *= alpha;
            EnsureRingColor(ref _fillColor, ref _lineColor, lineSize);
            SDFEllipseMesh.ApplyMaterialProperties(graphics.material, size, lineSize,
                _lineColor, _fillColor, centerColor, startDegree, endDegree, sdf_softEdge);

            graphics.sdf_onBeforePopulateMesh -= OnBeforePopulateMesh_sdfEllipse;
            graphics.sdf_onBeforePopulateMesh += OnBeforePopulateMesh_sdfEllipse;
            graphics.sdf_onChangeAlpha -= OnChangeAlpha_sdfEllipse;
            graphics.sdf_onChangeAlpha += OnChangeAlpha_sdfEllipse;

            sdf_alpha = -1;  // as dirty flag
        }
        void OnChangeAlpha_sdfEllipse(NGraphics graphics, float alpha)
        {
            var meshFactory = graphics.meshFactory;
            if (meshFactory is SDFEllipseMesh sdfEllipse)
            {
                if (sdf_alpha != alpha)
                {
                    sdf_alpha = alpha;

                    var fillColor = graphics.color; fillColor.a *= alpha;
                    var lineColor = (Color)sdfEllipse.lineColor; lineColor.a *= alpha;
                    EnsureRingColor(ref fillColor, ref lineColor, sdfEllipse.lineWidth);
                    graphics.material.SetColor(k_Color, fillColor);
                    graphics.material.SetColor(k_EdgeColor, lineColor);
                }
            }
            else
            {
                // pass
                // restore
                graphics.sdf_onChangeAlpha -= OnChangeAlpha_sdfEllipse;
            }
        }
        void OnBeforePopulateMesh_sdfEllipse(NGraphics graphics)
        {
            var meshFactory = graphics.meshFactory;
            if (meshFactory is SDFEllipseMesh sdfEllipse)
            {
                EnsureMatByShaderName(graphics, sdfSimpleEllipseShader);
                var lineColor = (Color)sdfEllipse.lineColor;
                lineColor.a *= alpha;
                var fillColor = graphics.color;
                fillColor.a *= alpha;
                var centerColor = (Color)(sdfEllipse.centerColor ?? fillColor);
                centerColor.a *= alpha;
                EnsureRingColor(ref fillColor, ref lineColor, sdfEllipse.lineWidth);
                // 椭圆的属性应用在了材质上，需要一个元件带一个材质
                // 对 URP 来说有效，如果是 BRP 应该用材质属性块（但这么做基本上一定会中断合批）
                SDFEllipseMesh.ApplyMaterialProperties(graphics.material,
                    this.size, sdfEllipse.lineWidth,
                    lineColor, fillColor, centerColor,
                    sdfEllipse.startDegree, sdfEllipse.endDegreee, sdf_softEdge);
            }
            else
            {
                // restore
                graphics.shader = ShaderConfig.imageShader;  // NGraphics 的缺省着色器
                graphics.material = null;  // 这里不能用 Graphics::SetMaterial
                graphics.sdf_onBeforePopulateMesh -= OnBeforePopulateMesh_sdfEllipse;
                isSDFShape = false;
            }
        }
    }
}
