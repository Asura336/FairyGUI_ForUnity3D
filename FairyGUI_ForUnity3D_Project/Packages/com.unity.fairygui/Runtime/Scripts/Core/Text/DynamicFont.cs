// 注释掉下一行就和原始的 FairyGUI 实现一样了
#define FONT_OVERSAMPLING

using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    /// 
    /// </summary>
    public class DynamicFont : BaseFont
    {
#if FONT_OVERSAMPLING
        /* 为字符应用超采样，后续从字体文件生成位图时请求的分辨率增加，但最终生成的四边形位置和大小维持原状
         */

        const int k_size_limit_0 = 25;
        const int k_size_limit_1 = 50;

        const int k_oversampling_0 = 4;
        const int k_oversampling_1 = 2;

        const float k_inv_oversampling_0 = 0.25f;
        const float k_inv_oversampling_1 = 0.5f;

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static int GetOverSampling(int fontSize)
        {
            if (fontSize <= k_size_limit_0) { return k_oversampling_0; }
            else if (fontSize <= k_size_limit_1) { return k_oversampling_1; }
            return 1;
        }
        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static float GetInvOverSamling(int fontSize)
        {
            if (fontSize <= k_size_limit_0) { return k_inv_oversampling_0; }
            else if (fontSize <= k_size_limit_1) { return k_inv_oversampling_1; }
            return 1;
        }
#endif

        static readonly float inv_SupScale = 1f * inv_SupScale;

        Font _font;
        int _size;
        float _ascent;
        float _lineHeight;
        float _scale;
        TextFormat _format;
        FontStyle _style;
        bool _boldVertice;
        CharacterInfo _char;
        CharacterInfo _lineChar;
        bool _gotLineChar;

        public DynamicFont()
        {
            this.canTint = true;
            this.keepCrisp = true;
            this.customOutline = true;
            this.shader = ShaderConfig.textShader;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="font"></param>
        /// <returns></returns>
        public DynamicFont(string name, Font font) : this()
        {
            this.name = name;
            this.nativeFont = font;
        }

        override public void Dispose()
        {
            Font.textureRebuilt -= textureRebuildCallback;
        }

        public Font nativeFont
        {
            get { return _font; }
            set
            {
                if (_font != null)
                    Font.textureRebuilt -= textureRebuildCallback;

                _font = value;
                Font.textureRebuilt += textureRebuildCallback;
                _font.hideFlags = DisplayObject.hideFlags;
                _font.material.hideFlags = DisplayObject.hideFlags;
                _font.material.mainTexture.hideFlags = DisplayObject.hideFlags;

                if (mainTexture != null)
                    mainTexture.Dispose();
                mainTexture = new NTexture(_font.material.mainTexture);
                mainTexture.destroyMethod = DestroyMethod.None;

                // _ascent = _font.ascent;
                // _lineHeight = _font.lineHeight;
                _ascent = _font.fontSize;
                _lineHeight = _font.fontSize * 1.25f;
            }
        }

        override public void SetFormat(TextFormat format, float fontSizeScale)
        {
            _format = format;
            float size = format.size * fontSizeScale;
            if (keepCrisp)
                size *= UIContentScaler.scaleFactor;
            if (_format.specialStyle == TextFormat.SpecialStyle.Subscript || _format.specialStyle == TextFormat.SpecialStyle.Superscript)
                size *= SupScale;
            _size = Mathf.FloorToInt(size);
            if (_size == 0)
                _size = 1;
            _scale = (float)_size / _font.fontSize;

            if (format.bold && !customBold)
            {
                if (format.italic)
                {
                    if (customBoldAndItalic)
                        _style = FontStyle.Italic;
                    else
                        _style = FontStyle.BoldAndItalic;
                }
                else
                    _style = FontStyle.Bold;
            }
            else
            {
                if (format.italic)
                    _style = FontStyle.Italic;
                else
                    _style = FontStyle.Normal;
            }

            _boldVertice = format.bold && (customBold || (format.italic && customBoldAndItalic));
            format.FillVertexColors(vertexColors);
        }

        override public void PrepareCharacters(string text, TextFormat format, float fontSizeScale)
        {
            SetFormat(format, fontSizeScale);
#if FONT_OVERSAMPLING
            int __size = _size * GetOverSampling(_size);
            _font.RequestCharactersInTexture(text, __size, _style);
#else
            _font.RequestCharactersInTexture(text, _size, _style);
#endif
        }

        override public bool GetGlyph(char ch, out float width, out float height, out float baseline)
        {
            int __size =
#if FONT_OVERSAMPLING
                _size * GetOverSampling(_size)
#else
                _size
#endif
                ;

            if (!_font.GetCharacterInfo(ch, out _char, __size, _style))
            {
                if (ch == ' ')
                {
                    //space may not be prepared, try again
                    _font.RequestCharactersInTexture(" ", __size, _style);
                    _font.GetCharacterInfo(ch, out _char, __size, _style);
                }
                else
                {
                    width = height = baseline = 0;
                    return false;
                }
            }

            width = _char.advance;
            height = _lineHeight * _scale;
            baseline = _ascent * _scale;
            if (_boldVertice)
                width++;

            if (_format.specialStyle == TextFormat.SpecialStyle.Subscript)
            {
                height *= inv_SupScale;
                baseline *= inv_SupScale;
            }
            else if (_format.specialStyle == TextFormat.SpecialStyle.Superscript)
            {
                height = height * inv_SupScale + baseline * SupOffset;
                baseline *= (SupOffset + 1 * inv_SupScale);
            }

            height = Mathf.RoundToInt(height);
            baseline = Mathf.RoundToInt(baseline);

            if (keepCrisp)
            {
                // Unity Mono 的实现下单精度浮点数除法很慢
                float inv_scaleFactor = 1f / UIContentScaler.scaleFactor;
                width *= inv_scaleFactor;
                height *= inv_scaleFactor;
                baseline *= inv_scaleFactor;
            }

#if FONT_OVERSAMPLING
            float invOverSampling = GetInvOverSamling(_size);
            width *= invOverSampling;
            float prevHeight = height;
            height *= invOverSampling;
            baseline += (height - prevHeight) * 0.25f * invOverSampling;
#endif

            return true;
        }

        static Vector3 bottomLeft;
        static Vector3 topLeft;
        static Vector3 topRight;
        static Vector3 bottomRight;

        static Vector2 uvBottomLeft;
        static Vector2 uvTopLeft;
        static Vector2 uvTopRight;
        static Vector2 uvBottomRight;

        static Color32[] vertexColors = new Color32[4];

        static Vector3[] BOLD_OFFSET = new Vector3[]
        {
            new Vector3(-0.5f, 0f, 0f),
            new Vector3(0.5f, 0f, 0f),
            new Vector3(0f, -0.5f, 0f),
            new Vector3(0f, 0.5f, 0f)
        };

        override public void DrawGlyph(VertexBuffer vb, float x, float y)
        {
            topLeft.x = _char.minX;
            topLeft.y = _char.maxY;
            bottomRight.x = _char.maxX;
            if (_char.glyphWidth == 0) //zero width, space etc
                bottomRight.x = topLeft.x + _size / 2;
            bottomRight.y = _char.minY;

            if (keepCrisp)
            {
                float inv_scaleFactor = 1f / UIContentScaler.scaleFactor;
                topLeft.x *= inv_scaleFactor;
                topLeft.y *= inv_scaleFactor;
                bottomRight.x *= inv_scaleFactor;
                bottomRight.y *= inv_scaleFactor;
            }

            if (_format.specialStyle == TextFormat.SpecialStyle.Subscript)
                y = y - Mathf.RoundToInt(_ascent * _scale * SupOffset);
            else if (_format.specialStyle == TextFormat.SpecialStyle.Superscript)
                y = y + Mathf.RoundToInt(_ascent * _scale * (1 * inv_SupScale - 1 + SupOffset));

#if FONT_OVERSAMPLING
            float invOverSampling = GetInvOverSamling(_size);
            topLeft.x *= invOverSampling;
            topLeft.y *= invOverSampling;
            bottomRight.x *= invOverSampling;
            bottomRight.y *= invOverSampling;
#endif

            topLeft.x += x;
            topLeft.y += y;
            bottomRight.x += x;
            bottomRight.y += y;

            topRight.x = bottomRight.x;
            topRight.y = topLeft.y;
            bottomLeft.x = topLeft.x;
            bottomLeft.y = bottomRight.y;

            vb.vertices.Add(bottomLeft);
            vb.vertices.Add(topLeft);
            vb.vertices.Add(topRight);
            vb.vertices.Add(bottomRight);

            uvBottomLeft = _char.uvBottomLeft;
            uvTopLeft = _char.uvTopLeft;
            uvTopRight = _char.uvTopRight;
            uvBottomRight = _char.uvBottomRight;

            vb.uvs.Add(uvBottomLeft);
            vb.uvs.Add(uvTopLeft);
            vb.uvs.Add(uvTopRight);
            vb.uvs.Add(uvBottomRight);

            vb.colors.Add(vertexColors[0]);
            vb.colors.Add(vertexColors[1]);
            vb.colors.Add(vertexColors[2]);
            vb.colors.Add(vertexColors[3]);

            if (_boldVertice)
            {
                for (int b = 0; b < 4; b++)
                {
                    ref readonly Vector3 boldOffset = ref BOLD_OFFSET[b];

                    vb.vertices.Add(bottomLeft + boldOffset);
                    vb.vertices.Add(topLeft + boldOffset);
                    vb.vertices.Add(topRight + boldOffset);
                    vb.vertices.Add(bottomRight + boldOffset);

                    vb.uvs.Add(uvBottomLeft);
                    vb.uvs.Add(uvTopLeft);
                    vb.uvs.Add(uvTopRight);
                    vb.uvs.Add(uvBottomRight);

                    vb.colors.Add(vertexColors[0]);
                    vb.colors.Add(vertexColors[1]);
                    vb.colors.Add(vertexColors[2]);
                    vb.colors.Add(vertexColors[3]);
                }
            }
        }

        override public void DrawLine(VertexBuffer vb, float x, float y, float width, int fontSize, int type)
        {
            if (!_gotLineChar)
            {
                _gotLineChar = true;
                _font.RequestCharactersInTexture("_", 50, FontStyle.Normal);
                _font.GetCharacterInfo('_', out _lineChar, 50, FontStyle.Normal);
            }

            float thickness;
            float offset;

            thickness = Mathf.Max(1, fontSize / 16f); //guest underline size
            if (type == 0)
                offset = Mathf.RoundToInt(_lineChar.minY * (float)fontSize / 50 + thickness);
            else
                offset = Mathf.RoundToInt(_ascent * 0.4f * fontSize / _font.fontSize);
            if (thickness < 1)
                thickness = 1;

            topLeft.x = x;
            topLeft.y = y + offset;
            bottomRight.x = x + width;
            bottomRight.y = topLeft.y - thickness;

            topRight.x = bottomRight.x;
            topRight.y = topLeft.y;
            bottomLeft.x = topLeft.x;
            bottomLeft.y = bottomRight.y;

            vb.vertices.Add(bottomLeft);
            vb.vertices.Add(topLeft);
            vb.vertices.Add(topRight);
            vb.vertices.Add(bottomRight);

            uvBottomLeft = _lineChar.uvBottomLeft;
            uvTopLeft = _lineChar.uvTopLeft;
            uvTopRight = _lineChar.uvTopRight;
            uvBottomRight = _lineChar.uvBottomRight;

            //取中点的UV
            Vector2 u0;
            if (_lineChar.uvBottomLeft.x != _lineChar.uvBottomRight.x)
                u0.x = (_lineChar.uvBottomLeft.x + _lineChar.uvBottomRight.x) * 0.5f;
            else
                u0.x = (_lineChar.uvBottomLeft.x + _lineChar.uvTopLeft.x) * 0.5f;

            if (_lineChar.uvBottomLeft.y != _lineChar.uvTopLeft.y)
                u0.y = (_lineChar.uvBottomLeft.y + _lineChar.uvTopLeft.y) * 0.5f;
            else
                u0.y = (_lineChar.uvBottomLeft.y + _lineChar.uvBottomRight.y) * 0.5f;

            vb.uvs.Add(u0);
            vb.uvs.Add(u0);
            vb.uvs.Add(u0);
            vb.uvs.Add(u0);

            vb.colors.Add(vertexColors[0]);
            vb.colors.Add(vertexColors[1]);
            vb.colors.Add(vertexColors[2]);
            vb.colors.Add(vertexColors[3]);

            if (_boldVertice)
            {
                for (int b = 0; b < 4; b++)
                {
                    Vector3 boldOffset = BOLD_OFFSET[b];

                    vb.vertices.Add(bottomLeft + boldOffset);
                    vb.vertices.Add(topLeft + boldOffset);
                    vb.vertices.Add(topRight + boldOffset);
                    vb.vertices.Add(bottomRight + boldOffset);

                    vb.uvs.Add(u0);
                    vb.uvs.Add(u0);
                    vb.uvs.Add(u0);
                    vb.uvs.Add(u0);

                    vb.colors.Add(vertexColors[0]);
                    vb.colors.Add(vertexColors[1]);
                    vb.colors.Add(vertexColors[2]);
                    vb.colors.Add(vertexColors[3]);
                }
            }
        }

        override public bool HasCharacter(char ch)
        {
            return _font.HasCharacter(ch);
        }

        override public int GetLineHeight(int size)
        {
            return Mathf.RoundToInt(_lineHeight * size / _font.fontSize);
        }

        void textureRebuildCallback(Font targetFont)
        {
            if (_font != targetFont)
                return;

            if (mainTexture == null || !Application.isPlaying)
            {
                mainTexture = new NTexture(_font.material.mainTexture);
                mainTexture.destroyMethod = DestroyMethod.None;
            }
            else
                mainTexture.Reload(_font.material.mainTexture, null);

            _gotLineChar = false;

            textRebuildFlag = true;
            version++;

            //Debug.Log("Font texture rebuild: " + name + "," + mainTexture.width + "," + mainTexture.height);
        }
    }
}
