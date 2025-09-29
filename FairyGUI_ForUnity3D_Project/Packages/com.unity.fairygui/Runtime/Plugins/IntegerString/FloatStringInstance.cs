using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FairyGUI.Foundations.Number
{
    /// <summary>
    /// 存储一小段字符串当作缓冲区，表示浮点数字符串，显示有限精度。
    /// <code>
    /// var instance = FloatStringInstance.Setup();  // allocate
    /// 
    /// instance.Modify(3.14159);  // write value
    /// print(instance.ToString())  // without gc
    /// </code>
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public sealed class FloatStringInstance
    {
        const int __pow10len = 16;
        static readonly double[] __pow10 = new double[]
        {
            1e0,
            1e1,
            1e2,
            1e3,

            1e4,
            1e5,
            1e6,
            1e7,

            1e8,
            1e9,
            1e10,
            1e11,

            1e12,
            1e13,
            1e14,
            1e15,
        };
        static readonly double[] __negaPow10 = new double[]
        {
            1e-1,
            1e-2,
            1e-3,
            1e-4,

            1e-5,
            1e-6,
            1e-7,
            1e-8,

            1e-9,
            1e-10,
            1e-11,
            1e-12,

            1e-13,
            1e-14,
            1e-15,
            1e-16,
        };

        readonly int bufferLen_left;
        readonly int bufferLen_right;
        readonly string buffer;

        int BufferLen => bufferLen_left + bufferLen_right + 2;

        public int Length { get; private set; }

        public FloatStringInstance(int leftBufferLength = 20, int rightBufferLength = 6)
        {
            bufferLen_left = leftBufferLength;
            if (rightBufferLength < 0 || rightBufferLength > 15)
            {
                throw new ArgumentException();
            }
            bufferLen_right = rightBufferLength;
            buffer = new string((char)0, BufferLen);
            Length = 0;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static FloatStringInstance Setup(int leftBufferLength = 20, int rightBufferLength = 6)
        {
            return new FloatStringInstance(leftBufferLength, rightBufferLength);
        }

        public override string ToString()
        {
            return buffer;
        }

        public override int GetHashCode() => buffer.GetHashCode();

        public unsafe ReadOnlySpan<char> AsSpan()
        {
            int p = 0;
            int l = BufferLen;
            for (; p < l; p++)
            {
                if (buffer[p] == '\0')
                {
                    break;
                }
            }
            return buffer.AsSpan(0, p);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public unsafe string Modify(double value)
        {
            bool negative = value < 0;
            if (negative)
            {
                value = -value;
            }
            value = Math.Round(value, bufferLen_right);

            fixed (char* p = buffer)
            {
                ClearMemory(p, BufferLen);
                if (value == 0)
                {
                    p[0] = '0';
                    p[1] = (char)0;
                    Length = 1;
                }
                else
                {
                    ulong value_left = (ulong)value;
                    double value_right = value - value_left;

                    char* stack = stackalloc char[BufferLen];
                    int stackHead;
                    int i = 0;

                    if (negative)
                    {
                        p[i++] = '-';
                    }

                    // 整数部分
                    if (value_left == 0)
                    {
                        p[i++] = '0';
                    }
                    else
                    {
                        stackHead = PushToStack(stack, value_left);
                        for (; stackHead != 0; stackHead--, i++)
                        {
                            p[i] = stack[stackHead - 1];
                        }
                    }

                    // 小数部分
                    if (value_right != 0)
                    {
                        p[i++] = '.';
                        // HACK: 小数部分在精度末尾加一点点，使显示结果正确
                        //value_right += Math.Pow(10, -bufferLen_right - 1) * 0.5;
                        // HACK: 10的负数次幂在数组中从 0.1 开始存储，下标比次方少 1
                        value_right += __negaPow10[bufferLen_right] * 0.5;
                        //ulong value_rightD = (ulong)Math.Round(value_right * Math.Pow(10, bufferLen_right));
                        ulong value_rightD = (ulong)Math.Round(value_right * __pow10[bufferLen_right]);

                        stackHead = PushToStack(stack, value_rightD);
                        for (int j = bufferLen_right - stackHead; j > 0; j--)
                        {
                            p[i++] = '0';
                        }
                        for (; stackHead != 0; stackHead--, i++)
                        {
                            char c = stack[stackHead - 1];
                            p[i] = c;
                        }
                        for (; p[i - 1] == '0' && i > 0; i--)
                        {
                            p[i - 1] = '\0';
                        }
                    }

                    Length = i;
                }
            }
            return ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double Pow10(int pow)
        {
            return pow < 0 ? __negaPow10[-pow] : __pow10[pow];
        }

        static unsafe int PushToStack(char* stack, ulong value)
        {
            int stackHead = 0;
            while (value != 0)
            {
                stack[stackHead++] = (char)('0' + value % 10);
                value /= 10;
            }
            return stackHead;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe void ClearMemory(char* head, int length)
        {
            for (int i = 0; i < length; i++)
            {
                head[i] = '\0';
            }
        }
    }
}