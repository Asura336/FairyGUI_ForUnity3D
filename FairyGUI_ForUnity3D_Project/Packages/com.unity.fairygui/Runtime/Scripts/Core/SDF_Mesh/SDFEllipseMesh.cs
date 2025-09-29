using FairyGUI.Utils;
using UnityEngine;
using static FairyGUI.SDFShapeConstants;

namespace FairyGUI
{
    /// <summary>
    /// 描述简单的椭圆图形，使用基于 SDF 的材质。
    /// 特别地，当边缘的颜色的透明度为 0，改为使用柔边
    /// </summary>
    public class SDFEllipseMesh : IMeshFactory, IHitTest
    {
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
        public Color32? centerColor;

        /// <summary>
        /// 
        /// </summary>
        public Color32? fillColor;

        /// <summary>
        /// 
        /// </summary>
        public float startDegree;

        /// <summary>
        /// 
        /// </summary>
        public float endDegreee;

        public SDFEllipseMesh()
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
            if (!contentRect.Contains(point))
                return false;

            float radiusX = contentRect.width * 0.5f;
            float raduisY = contentRect.height * 0.5f;
            float xx = point.x - radiusX - contentRect.x;
            float yy = point.y - raduisY - contentRect.y;
            if (ToolSet.Pow2(xx / radiusX) + ToolSet.Pow2(yy / raduisY) < 1)
            {
                if (startDegree != 0 || endDegreee != 360)
                {
                    float deg = Mathf.Atan2(yy, xx) * Mathf.Rad2Deg;
                    if (deg < 0)
                        deg += 360;
                    return deg >= startDegree && deg <= endDegreee;
                }
                else
                    return true;
            }

            return false;
        }

        public static void ApplyMaterialProperties(Material mat,
            in Vector2 size, float lineSize,
            in Color lineColor, in Color fillColor, in Color centerColor,
            float startDegree, float endDegree,
            bool softEdge)
        {
            // HACK: 和着色器实现有关，为了视觉效果正确增加了校正系数
            // FairyGUI 的填充规则是右方为 0 度角，顺时针是正方向
            // 着色器实现中的旋转规则是下方为 0 度角，逆时针是正方向
            InternalApplyMaterialProperties(mat, size,
                lineSize,
                lineColor,
                fillColor,
                centerColor,
                (endDegree - startDegree) * Mathf.Deg2Rad,
                (270 - ((startDegree + endDegree) * 0.5f)) * Mathf.Deg2Rad,
                softEdge);
        }
        static void InternalApplyMaterialProperties(Material mat,
            in Vector2 size, float lineSize,
            in Color lineColor, in Color fillColor, in Color centerColor,
            float pieAngle, float rotateAngle,
            bool softEdge = false)
        {
            var pixelSize = size;
            bool largeWidth = pixelSize.x > pixelSize.y;
            float resolutionPixelSize = largeWidth ? pixelSize.y : pixelSize.x;
            var scaleXY = largeWidth
                ? new Vector2(pixelSize.x / pixelSize.y, 1)
                : new Vector2(1, pixelSize.y / pixelSize.x);
            float _edgeWidth = Mathf.Max(lineSize, 0) / resolutionPixelSize;
            var extends = scaleXY * 0.5f - new Vector2(_edgeWidth, _edgeWidth);

            float edgeSolid = softEdge ? 0 : 1;
            mat.SetFloat(k_EdgeSolid, edgeSolid);
            mat.SetColor(k_EdgeColor, lineColor);
            mat.SetColor(k_Color, fillColor);
            mat.SetColor(k_CenterColor, centerColor);
            mat.SetVector(k_Bounds, new Vector4(0, 0, extends.x, extends.y));
            mat.SetVector(k_Scale, scaleXY);
            mat.SetFloat(k_EdgeWidth, _edgeWidth);
            mat.SetFloat(k_PieAngle, pieAngle);
            mat.SetFloat(k_RotateAngle, rotateAngle);
        }
    }
}