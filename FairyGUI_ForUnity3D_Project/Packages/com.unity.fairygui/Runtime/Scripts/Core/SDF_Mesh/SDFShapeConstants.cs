using UnityEngine;

namespace FairyGUI
{
    internal static class SDFShapeConstants
    {
        public const string sdfSimpleEllipseShader = "FairyGUI/SDSimpleEllipse";
        public const string sdfRectShader = "FairyGUI/SDRect";

        public static readonly int k_Bounds = Shader.PropertyToID("_Bounds");
        public static readonly int k_Color = Shader.PropertyToID("_Color");
        public static readonly int k_CenterColor = Shader.PropertyToID("_CenterColor");
        public static readonly int k_EdgeColor = Shader.PropertyToID("_EdgeColor");
        public static readonly int k_EdgeSolid = Shader.PropertyToID("_EdgeSolid");
        public static readonly int k_EdgeWidth = Shader.PropertyToID("_EdgeWidth");
        public static readonly int k_PieAngle = Shader.PropertyToID("_PieAngle");
        public static readonly int k_RectRadius = Shader.PropertyToID("_RectRadius");
        public static readonly int k_RotateAngle = Shader.PropertyToID("_RotateAngle");
        public static readonly int k_Scale = Shader.PropertyToID("_Scale");
    }
}