#if !defined FGUI_SDF_EDGE_AA
#define FGUI_SDF_EDGE_AA

// https://www.ronja-tutorials.com/post/034-2d-sdf-basics/
inline float antialiasing(float dist)
{
    float fwidth_dist = fwidth(dist);
    float delta = fwidth(dist) * 0.5;  // 后面的乘数越大边缘的视觉效果越柔和，但可能造成外描边的颜色侵入里面
    float alpha = smoothstep(delta, -delta, dist);
    return alpha;
}

half4 edgeColor(float dis, float edgeWidth,
    in half4 col, in half4 edgeColor, float edgeSolid)
{
    float edgeVal = saturate((edgeWidth - dis) / edgeWidth);
    // 内边缘：抗锯齿
    col.rgba = lerp(edgeColor, col, antialiasing(dis));
    // 外边缘：抗锯齿
    float edgeAlpha = lerp(edgeVal, antialiasing(dis - edgeWidth), edgeSolid);
    float isEdge = 1 - edgeVal;
    col.a = lerp(col.a, edgeColor.a, isEdge) * edgeAlpha;

    return col;
}

#endif