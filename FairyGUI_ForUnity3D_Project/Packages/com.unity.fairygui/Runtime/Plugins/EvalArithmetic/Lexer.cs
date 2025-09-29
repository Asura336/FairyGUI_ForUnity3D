using System;
using System.Linq;
using FairyGUI.Foundations.Collections;

namespace FairyGUI.Foundations.Arithmetic
{
    using static Constants;

    class Lexer : IDisposable
    {
        public enum TokenType : ushort
        {
            None,
            Err,
            End,
            Number,
            Plus,
            Minus,
            Multiply,
            Devide,
            Remainder,
            Power,
            Exp,
            LSeparator,
            RSeparator,
        }


        readonly StringBuilderHandle src;
        int point;

        readonly ArrayHandle<char> acceptNumberBuffer;

        public Lexer(ReadOnlySpan<char> source)
        {
            src = StringBuilderHandle.New();
            src.Append(source);
            acceptNumberBuffer = ArrayHandle<char>.New(source.Length);
            point = 0;
            Token = TokenType.None;
            Number = 0;

            Accept();
        }

        public override string ToString()
        {
            return src.ToString();
        }

        public void Dispose()
        {
            src.Dispose();
            acceptNumberBuffer.Dispose();
        }

        public int GetPoint() => point;

        public TokenType Token { get; private set; }
        public double Number { get; private set; }

        public int SourceLength => src.Length;


        public void Accept()
        {
            SkipWhite();
            if (point > src.Length - 1)
            {
                Token = TokenType.End;
                return;
            }

            switch (src[point])
            {
                case __add:
                    Token = TokenType.Plus;
                    ++point;
                    break;
                case __minus:
                    Token = TokenType.Minus;
                    ++point;
                    break;
                case __multiply:
                    Token = TokenType.Multiply;
                    ++point;
                    break;
                case __divide:
                    Token = TokenType.Devide;
                    ++point;
                    break;
                case __power:
                    Token = TokenType.Power;
                    ++point;
                    break;
                case __remainder:
                    Token = TokenType.Remainder;
                    ++point;
                    break;
                case __leftSeparator:
                    Token = TokenType.LSeparator;
                    ++point;
                    break;
                case __rightSeparator:
                    Token = TokenType.RSeparator;
                    ++point;
                    break;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case __point:
                    AcceptNumber(acceptNumberBuffer.Body);
                    break;
                default:
                    Token = TokenType.Err;
                    throw new ArithmeticException($"表达式 {src} 在位置 [{point}] 有非法字符 \'{src[point]}\'");
            }
        }

        void AcceptNumber(char[] _buffer)
        {
            bool _pointExist = false;
            bool _expExist = false;
            bool _middleMinus = false;
            int count = 0;
            for (; point < src.Length; point++)
            {
                var c = src[point];
                if (c == __point)
                {
                    if (_pointExist) { throw new ArithmeticException($"表达式 {src} 在位置 [{point}] 有重复的小数点"); }
                    _pointExist = true;
                    if (count == 0)
                    {
                        _buffer[count++] = '0';
                    }
                    _buffer[count++] = c;
                }
                else if (c == __exp)
                {
                    if (_expExist) { throw new ArithmeticException($"表达式 {src} 在位置 [{point}] 有重复的指数标记"); }
                    _expExist = true;
                    _buffer[count++] = c;
                }
                else if (c == __minus)
                {
                    if (point + 1 > src.Length - 1 || !__numbers.Contains(src[point + 1]) ||
                        _middleMinus || !_expExist)
                    {
                        break;
                    }
                    if (count == 0)
                    {
                        _middleMinus = true;
                    }
                    _buffer[count++] = c;
                }
                else if (__numbers.Contains(c))
                {
                    _buffer[count++] = c;
                }
                else
                {
                    break;
                }
            }

            var numberSpan = _buffer.AsSpan(0, count);
            if (double.TryParse(numberSpan, out var _n))
            {
                Number = _n;
                Token = TokenType.Number;
            }
            else
            {
                throw new ArithmeticException($"表达式 {src} 解析失败，字面量 \"{numberSpan.ToString()}\" 不是数字，在位置 [{point}]");
            }
        }

        void SkipWhite()
        {
            for (; point < src.Length; point++)
            {
                if (!char.IsWhiteSpace(src[point]))
                {
                    break;
                }
            }
        }
    }
}