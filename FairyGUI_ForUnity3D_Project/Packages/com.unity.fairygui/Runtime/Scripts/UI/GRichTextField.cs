using System;
using System.Collections.Generic;
using FairyGUI.Foundations.Collections;
using FairyGUI.Utils;

namespace FairyGUI
{
    /// <summary>
    /// GRichTextField class.
    /// </summary>
    public class GRichTextField : GTextField
    {
        /// <summary>
        /// 
        /// </summary>
        public RichTextField richTextField { get; private set; }

        public GRichTextField()
            : base()
        {
        }

        override protected void CreateDisplayObject()
        {
            richTextField = new RichTextField();
            richTextField.gOwner = this;
            displayObject = richTextField;

            _textField = richTextField.textField;
        }

        override protected void SetTextFieldText()
        {
            string str = _text;
            using var buffer = StringBuilderHandle.New();

            if (_templateVars != null)
            {
                ParseTemplate(str, buffer);
            }
            else
            {
                buffer.Append(str);
            }
            int bufferLength = buffer.Length;

            _textField.maxWidth = maxWidth;
            if (_ubbEnabled)
            {
#if UNITY_2021_3_OR_NEWER
                ReadOnlySpan<char> sp;
                if (bufferLength > smallTextLength)
                {
                    sp = buffer.ToString();
                }
                else
                {
                    unsafe
                    {
                        fixed (char* p = smallTextBuffer)
                        {
                            var _sp = new Span<char>(p, bufferLength);
                            buffer.Body.CopyTo(0, _sp, bufferLength);
                            sp = _sp;
                        }
                    }
                }
                var parser = new UBBParser1(sp);
                var dst = StringBuilderHandle.New();
                parser.Parse(dst);
                richTextField.htmlText = dst.ToString();
#else
                richTextField.htmlText = UBBParser.inst.Parse(buffer.ToString());
#endif
            }
            else
            {
                richTextField.htmlText = buffer.ToString();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<uint, Emoji> emojies
        {
            get { return richTextField.emojies; }
            set { richTextField.emojies = value; }
        }
    }
}
