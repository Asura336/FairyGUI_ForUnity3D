#if UNITY_2022_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using FairyGUI.Foundations.Collections;
using UnityEngine.Assertions;

namespace FairyGUI.Utils
{
    //https://github.com/zmmbreeze/UBBParser/blob/master/doc/UBB%E8%AF%AD%E6%B3%95.mkd
    //https://github.com/frontend9/fe9-library/issues/10
    public readonly ref struct UBBParser1
    {
        /* 
         * [tag]...[/tag]
         * [tag=...]...[/tag]
         * 
         * \[ => [
         * 
         */

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetUBBTagHash(ReadOnlySpan<char> src)
        {
            int len = src.Length;
            unchecked
            {
                //https://www.ietf.org/archive/id/draft-eastlake-fnv-25.html
                const int hash_prime = 16777619;
                int hash = (int)0x811C9DC5;
                for (int i = 0; i < len; i++)
                {
                    int v = src[i];
                    hash = (hash * hash_prime) ^ v;
                }
                return hash;
            }
        }


        public delegate void UBBTagHandler(in UBBToken token, ReadOnlySpan<char> src, StringBuilder dst);

        public static UBBTagHandler defaultTagHandler;

        /// <summary>
        /// 键通过 <see cref="GetUBBTagHash(ReadOnlySpan{char})"/> 计算，
        /// 内部实现中也使用此方法获取 UBB 标签对应的键，减少字符串分配。
        /// **需要留意返回值计算得到的 hash 冲突**
        /// </summary>
        public static readonly Dictionary<int, UBBTagHandler> ubbTagHandlers;

        public static int defaultImgWidth = 0;
        public static int defaultImgHeight = 0;

        static UBBParser1()
        {
            ubbTagHandlers = new Dictionary<int, UBBTagHandler>
            {
                { GetUBBTagHash("b"), OnSimpleTagFacory("b") },
                { GetUBBTagHash("i"), OnSimpleTagFacory("i") },
                { GetUBBTagHash("u"), OnSimpleTagFacory("u") },
                { GetUBBTagHash("sup"), OnSimpleTagFacory("sup") },
                { GetUBBTagHash("sub"), OnSimpleTagFacory("sub") },
                { GetUBBTagHash("strike"), OnSimpleTagFacory("strike") },

                { GetUBBTagHash("align"), OnSimpleTagWithAttributeFactory("font", "align") },
                { GetUBBTagHash("color"), OnSimpleTagWithAttributeFactory("font", "color") },
                { GetUBBTagHash("font"), OnSimpleTagWithAttributeFactory("font", "face") },
                { GetUBBTagHash("img"), OnImg },
                { GetUBBTagHash("size"), OnSimpleTagWithAttributeFactory("font", "size") },
                { GetUBBTagHash("url"), OnUrl },
            };
        }

        #region UBB tag handlers
        /// <summary>
        /// [tag]text[/tag]
        /// </summary>
        /// <param name="token"></param>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        public static UBBTagHandler OnSimpleTagFacory(string tagName)
        {
            void local(in UBBToken token, ReadOnlySpan<char> src, StringBuilder dst)
            {
                // <tag> or </tag>
                dst.Append('<');
                if (token.isEnd) { dst.Append('/'); }
                dst.Append(tagName);
                dst.Append('>');
            }
            return local;
        }

        /// <summary>
        /// [tag=attribute]text[/tag]
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public static UBBTagHandler OnSimpleTagWithAttributeFactory(string tagName, string attributeName)
        {
            void local(in UBBToken token, ReadOnlySpan<char> src, StringBuilder dst)
            {
                if (token.isEnd)
                {
                    // </tag>
                    dst.Append("</");
                    dst.Append(tagName);
                    dst.Append('>');
                }
                else
                {
                    // <tag attr="">
                    dst.Append('<');
                    dst.Append(tagName);
                    dst.Append(' ');
                    dst.Append(attributeName);
                    dst.Append("=\"");
                    token.attribute.WriteTo(src, dst);
                    dst.Append("\">");
                }
            }
            return local;
        }

        /// <summary>
        /// [Img]imgUrl[/img]
        /// </summary>
        /// <param name="token"></param>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        static void OnImg(in UBBToken token, ReadOnlySpan<char> src, StringBuilder dst)
        {
            if (!token.isEnd)
            {
                using var imgUrl = StringBuilderHandle.New();
                GetNextText(token, src, imgUrl);
                if (imgUrl.Length == 0) { return; }

                if (defaultImgWidth != 0)
                {
                    // <img src="url" width="" height=""/>
                    dst.Append("<img src=\"");
                    dst.Append(imgUrl);
                    dst.Append("\" width=\"");
                    dst.Append(defaultImgWidth);
                    dst.Append("\" height=\"");
                    dst.Append(defaultImgHeight);
                    dst.Append("\"/>");
                }
                else
                {
                    // <img src="url"/>
                    dst.Append("<img src=\"");
                    dst.Append(imgUrl);
                    dst.Append("\"/>");
                }
            }
        }

        /// <summary>
        /// [url]www[url]
        /// or
        /// [url=www]text[/url]
        /// </summary>
        /// <param name="token"></param>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        static void OnUrl(in UBBToken token, ReadOnlySpan<char> src, StringBuilder dst)
        {
            if (token.isEnd)
            {
                dst.Append("</a>");
            }
            else
            {
                // <a href="{attribute}" target="_blank">
                dst.Append("<a href=\"");
                if (token.attribute.IsEmpty)
                {
                    // value?
                    GetNextText(token, src, dst);
                }
                else
                {
                    token.attribute.WriteTo(src, dst);
                }
                dst.Append("\" target=\"_blank\">");
            }
        }

        public static void GetNextText(in UBBToken token, ReadOnlySpan<char> src, StringBuilder dst)
        {
            Assert.AreEqual(UBBTokenType.StartTag, token.type);
            var raw = token.value;
            int start = raw.offset + raw.length;
            for (int i = start; i < src.Length; i++)
            {
                if (src[i] == '[')
                {
                    if (src[i - 1] == '\\')
                    {
                        i++;
                    }
                    else
                    {
                        dst.Append(src.Slice(start, i - start));
                        return;
                    }
                }
            }
        }
        #endregion


        public readonly ReadOnlySpan<char> src;

        public UBBParser1(string src)
        {
            this.src = src;
        }

        public UBBParser1(ReadOnlySpan<char> src)
        {
            this.src = src;
        }

        public readonly void Parse(StringBuilder dst)
        {
            using var tokens = ListHandle<UBBToken>.New();
            Parse_RawTags(tokens, out int maxTagLength);
            // some pass...
            // 如果要补全标签或者修复交叉嵌套...
            //...

            // output...
            //WriteDebug(tokens, dst);
            WriteHtmlDirect(tokens, maxTagLength, dst);
        }

        readonly void Parse_RawTags(List<UBBToken> tokens, out int maxTagLength)
        {
            int len = src.Length;
            maxTagLength = 16;
            for (int i = 0; i < len; i++)
            {
                switch (src[i])
                {
                    case '\\':
                    {
                        // unescape
                        if (i + 1 < len)
                        {
                            switch (src[i + 1])
                            {
                                case '[':
                                case ']':
                                    i++;
                                    tokens.Add(UBBToken.Text(i, 1));
                                    continue;
                                default:
                                    Parse_NextValue(tokens, ref i);
                                    break;
                            }
                        }
                        // last '\'
                        else
                        {
                            tokens.Add(UBBToken.Text(i, 1));
                        }
                    }
                    break;

                    case '[':
                    {
                        // tag
                        if (i + 1 < len)
                        {
                            int pStart = i;
                            // [ or [/
                            bool endTag = src[i + 1] == '/';
                            if (endTag) { i++; }
                            for (; i < len && src[i] != ']'; i++)
                            {
                                // unescape
                                if (src[i] == '\\' && i + 1 < len)
                                {
                                    switch (src[i + 1])
                                    {
                                        case '[':
                                        case ']':
                                            i++;
                                            break;
                                    }
                                }
                            }
                            // 要不要处理前导和尾随空格？
                            if (i == len)
                            {
                                // EOF
                                // [bold
                                // [/bold
                                tokens.Add(UBBToken.Text(pStart, len - pStart));
                            }
                            else
                            {
                                // must tag
                                // [bold]
                                // [/bold]
                                // [color=#ffffff]
                                int rawStart = pStart;
                                pStart++;
                                Assert.AreEqual(']', src[i]);
                                int pAttr = pStart + 1;
                                for (; pAttr < i && src[pAttr] != '='; pAttr++) { }
                                if (pAttr != i)
                                {
                                    // must has attribute
                                    Assert.AreEqual('=', src[pAttr]);
                                    if (endTag) { pStart++; }

                                    int _tagLength = pAttr - pStart;
                                    tokens.Add(UBBToken.Tag(raw: (rawStart, i - rawStart + 1),
                                        tag: (pStart, _tagLength),
                                        isEnd: endTag,
                                        attribute: (pAttr + 1, i - pAttr - 1)));
                                    maxTagLength = Math.Max(_tagLength, maxTagLength);
                                }
                                else
                                {
                                    // simple tag
                                    if (endTag) { pStart++; }

                                    int _tagLength = i - pStart;
                                    tokens.Add(UBBToken.Tag(raw: (rawStart, i - rawStart + 1),
                                        tag: (pStart, _tagLength),
                                        isEnd: endTag));
                                    maxTagLength = Math.Max(_tagLength, maxTagLength);
                                }
                            }
                        }
                        // last '['
                        else
                        {
                            tokens.Add(UBBToken.Text(i, 1));
                        }
                    }
                    break;

                    default:
                        // fallback
                        Parse_NextValue(tokens, ref i);
                        break;
                }
            }
        }

        //readonly void SkipSpace(ref int i)
        //{
        //    int len = src.Length;
        //    for (; i < len && src[i] == ' '; i++) { }
        //}

        /// <summary>
        /// 读取下一个值，遇到标签起始字符就停下来
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="i"></param>
        readonly void Parse_NextValue(List<UBBToken> tokens, ref int i)
        {
            int len = src.Length;
            if (i >= len) { return; }
            if (i == len - 1)
            {
                tokens.Add(UBBToken.Text(i, 1));
                return;
            }

            int pStart = i;
            i++;
            for (; i < len; i++)
            {
                switch (src[i])
                {
                    case '[':
                        goto StopOnTag;
                    case '\\' when i + 1 < len:
                        switch (src[i + 1])
                        {
                            case '[':
                            case ']':
                                i++;
                                continue;
                        }
                        break;
                }
            }
StopOnTag:
            tokens.Add(UBBToken.Text(pStart, i - pStart));
            i--;
        }

        readonly void WriteDebug(List<UBBToken> tokens, StringBuilder dst)
        {
            foreach (var tag in tokens)
            {
                tag.WriteDebug(src, dst);
            }
        }

        const int WriteHtmlDirect_bufferLen = 256;
        static readonly char[] WriteHtmlDirect_buffer = new char[WriteHtmlDirect_bufferLen];
        readonly unsafe void WriteHtmlDirect(List<UBBToken> tokens, int maxTagLength, StringBuilder dst)
        {
            ArrayHandle<char> buffer = default;
            Span<char> bufferBody;
            if (maxTagLength > WriteHtmlDirect_bufferLen)
            {
                buffer = ArrayHandle<char>.New(maxTagLength);
                bufferBody = buffer.Body;
            }
            else
            {
                bufferBody = WriteHtmlDirect_buffer;
            }

            int count = tokens.Count;
            var readTokens = ToolSet.AsSpan(tokens);
            for (int i = 0; i < count; i++)
            {
                ref readonly var token = ref readTokens[i];
                switch (token.type)
                {
                    case UBBTokenType.StartTag:
                    case UBBTokenType.EndTag:
                    {
                        var (offs, len) = token.tag;
                        InternalToLower(src, offs, len, bufferBody);
                        int tagNameHash = GetUBBTagHash(bufferBody.Slice(0, len));

                        if (!ubbTagHandlers.TryGetValue(tagNameHash, out var process))
                        {
                            process = defaultTagHandler;
                        }
                        process?.Invoke(token, src, dst);
                    }
                    continue;
                }
                // Text
                token.value.WriteTo(src, dst);
            }
            buffer.Dispose();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InternalToLower(ReadOnlySpan<char> src, int offs, int length, Span<char> dst)
        {
            for (int i = 0; i < length; i++)
            {
                char ch = src[i + offs];
                const char delta = (char)('a' - 'A');
                if (ch >= 'A' && ch <= 'Z') { ch += delta; }
                dst[i] = ch;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public readonly struct StringSpan
    {
        public static readonly StringSpan empty = default;

        public readonly int offset;
        public readonly int length;

        public StringSpan(int offset, int length)
        {
            this.offset = offset;
            this.length = length;
        }

        public string ToString(string src)
        {
            return src.Substring(offset, length);
        }

        public string ToString(ReadOnlySpan<char> src)
        {
            return src.Slice(offset, length).ToString();
        }

        public void Deconstruct(out int offset, out int length)
        {
            offset = this.offset;
            length = this.length;
        }

        public void WriteTo(string src, StringBuilder dst)
        {
            if (!IsEmpty)
            {
                dst.Append(src, offset, length);
            }
        }

        public void WriteTo(ReadOnlySpan<char> src, StringBuilder dst)
        {
            if (!IsEmpty)
            {
                dst.Append(src.Slice(offset, length));
            }
        }

        public bool IsEmpty => length == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StringSpan((int offset, int length) arg) => new StringSpan(arg.offset, arg.length);
    }

    public enum UBBTokenType : byte
    {
        None,
        StartTag,
        EndTag,
        Text,
    }
    /// <summary>
    /// tag | endTag | value
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct UBBToken
    {
        public readonly StringSpan tag;
        public readonly StringSpan attribute;
        public readonly StringSpan value;
        public readonly UBBTokenType type;
        public readonly bool isEnd;

        public UBBToken(UBBTokenType type, StringSpan tag, bool isEnd, StringSpan attribute, StringSpan value)
        {
            this.type = type;
            this.tag = tag;
            this.isEnd = isEnd;
            this.attribute = attribute;
            this.value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UBBToken Tag(StringSpan raw, StringSpan tag, bool isEnd, StringSpan attribute)
        {
            return new UBBToken(isEnd ? UBBTokenType.EndTag : UBBTokenType.StartTag,
                tag, isEnd, attribute, raw);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UBBToken Tag(StringSpan raw, StringSpan tag, bool isEnd)
        {
            return new UBBToken(isEnd ? UBBTokenType.EndTag : UBBTokenType.StartTag,
                tag, isEnd, StringSpan.empty, raw);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UBBToken Text(int offset, int length)
        {
            return new UBBToken(UBBTokenType.Text, StringSpan.empty, false, StringSpan.empty, (offset, length));
        }

        public void WriteDebug(ReadOnlySpan<char> src, StringBuilder dst)
        {
            // debug, write raw UBB
            switch (type)
            {
                case UBBTokenType.StartTag:
                    if (attribute.IsEmpty)
                    {
                        dst.Append($"<tag {tag.ToString(src)}>");
                    }
                    else
                    {
                        dst.Append($"<tag {tag.ToString(src)}, attr={attribute.ToString(src)}>");
                    }
                    break;
                case UBBTokenType.EndTag:
                    dst.Append($"</tag {tag.ToString(src)}>");
                    break;
                default:  // just value
                    dst.Append($"<value {value.ToString(src)} />");
                    break;
            }
        }
    }
}
#endif