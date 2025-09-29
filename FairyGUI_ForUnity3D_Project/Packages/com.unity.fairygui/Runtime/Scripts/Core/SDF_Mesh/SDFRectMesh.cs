using UnityEngine;
using static FairyGUI.SDFShapeConstants;

namespace FairyGUI
{
    /// <summary>
    /// 描述圆角矩形，使用基于 SDF 的材质。
    /// 特别地，当边缘的颜色的透明度为 0，改为使用柔边
    /// </summary>
    public class SDFRectMesh : IMeshFactory, IHitTest
    {
        /* GGraph 的初始化
         *   生成 Shape 实例作为 displayObject
         *   Shape 生成一个 NGraphics 的实例
         *   从 fui.bytes 读取信息，初始化形状（Shape 的方法）
         *   
         * 从哪一步同步材质属性过去？
         */

        /// <summary>
        /// 
        /// </summary>
        public Rect? drawRect;

        /// <summary>
        /// 
        /// </summary>
        public float lineWidth;

        /// <summary>
        /// 
        /// </summary>
        public Color32 lineColor;

        /// <summary>
        /// 
        /// </summary>
        public Color32? fillColor;

        /// <summary>
        /// 
        /// </summary>
        public float topLeftRadius;

        /// <summary>
        /// 
        /// </summary>
        public float topRightRadius;

        /// <summary>
        /// 
        /// </summary>
        public float bottomLeftRadius;

        /// <summary>
        /// 
        /// </summary>
        public float bottomRightRadius;

        public bool enableEdgeAA;

        public SDFRectMesh()
        {
            lineColor = Color.black;
        }

        public void OnPopulateMesh(VertexBuffer vb)
        {
            var rect = drawRect != null ? (Rect)drawRect : vb.contentRect;
            var color = fillColor != null ? (Color32)fillColor : vb.vertexColor;

            // 使用距离场描述形状，网格总是生成四边形
            vb.AddQuad(rect, color);
            vb.AddTriangles();
        }

        public bool HitTest(Rect contentRect, Vector2 point)
        {
            return (drawRect ?? contentRect).Contains(point);
        }

        public static void ApplyMaterialProperties(Material mat, in Vector2 size, float lineSize,
            in Color lineColor, in Color fillColor,
            float topLeftRadius, float topRightRadius, float bottomLeftRadius, float bottomRightRadius,
            bool softEdge)
        {
            var pixelSize = size;
            bool largeWidth = pixelSize.x > pixelSize.y;
            float resolutionPixelSize = largeWidth ? pixelSize.y : pixelSize.x;
            var scaleXY = largeWidth
                ? new Vector2(pixelSize.x / pixelSize.y, 1)
                : new Vector2(1, pixelSize.y / pixelSize.x);
            float _edgeWidth =  lineSize / resolutionPixelSize;
            var _rawRectRadius = new Vector4(
                topRightRadius, bottomRightRadius,
                topLeftRadius, bottomLeftRadius);
            var rectRadius = (_rawRectRadius - Vector4.one * lineSize) / resolutionPixelSize;
            var extends = scaleXY * 0.5f - new Vector2(_edgeWidth, _edgeWidth);

            float edgeSolid = softEdge ? 0 : 1;
            mat.SetFloat(k_EdgeSolid, edgeSolid);
            mat.SetColor(k_EdgeColor, lineColor);
            mat.SetColor(k_Color, fillColor);
            mat.SetVector(k_Bounds, new Vector4(0, 0, extends.x, extends.y));
            mat.SetVector(k_RectRadius, rectRadius);
            mat.SetVector(k_Scale, scaleXY);
            mat.SetFloat(k_EdgeWidth, _edgeWidth);
        }
    }
}