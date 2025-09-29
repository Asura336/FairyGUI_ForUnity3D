using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FairyGUI.Foundations.Number
{
    /// <summary>
    /// 存储一小段字符串当作缓冲区，用来表示整数字符串。
    /// <code>
    /// var instance = IntegerStringInstance.Setup();  // allocate
    /// 
    /// instance.Modify(514);  // write value
    /// print(instance.ToString());  // without gc
    /// </code>
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public sealed class IntegerStringInstance
    {
        // Int64 最小值字符串不超过 20
        const int int64Capacity = 20;
        string buffer;

        public int Length { get; private set; }


        public IntegerStringInstance()
        {
            buffer = new string((char)0, int64Capacity);
            Length = 0;
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public static IntegerStringInstance Setup()
        {
            return new IntegerStringInstance
            {
                Length = 0,
                buffer = new string((char)0, int64Capacity)
            };
        }

        public override string ToString()
        {
            return buffer;
        }

        public override int GetHashCode() => buffer.GetHashCode();

        public unsafe ReadOnlySpan<char> AsSpan()
        {
            int p = 0;
            int l = int64Capacity;
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
        public unsafe string Modify(long value)
        {
            fixed (char* p = buffer)
            {
                ClearMemory(p, int64Capacity);
                if (value == 0)
                {
                    p[0] = '0';
                    p[1] = (char)0;
                    Length = 1;
                }
                else
                {
                    char* stack = stackalloc char[int64Capacity];
                    int stackHead = PushToStack(stack, value);

                    Length = stackHead;
                    int i = 0;
                    for (; stackHead != 0; stackHead--, i++)
                    {
                        p[i] = stack[stackHead - 1];
                    }
                    if (i < int64Capacity) { p[i] = (char)0; }
                }
            }
            return ToString();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public unsafe string Modify(ulong value)
        {
            fixed (char* p = buffer)
            {
                ClearMemory(p, int64Capacity);
                if (value == 0)
                {
                    p[0] = '0';
                    p[1] = (char)0;
                    Length = 1;
                }
                else
                {
                    char* stack = stackalloc char[int64Capacity];
                    int stackHead = PushToStack(stack, value);

                    Length = stackHead;
                    int i = 0;
                    for (; stackHead != 0; stackHead--, i++)
                    {
                        p[i] = stack[stackHead - 1];
                    }
                    if (i < int64Capacity) { p[i] = (char)0; }
                }
            }
            return ToString();
        }


        static unsafe int PushToStack(char* stack, long value)
        {
            int stackHead = 0;
            bool negative = value < 0;
            if (negative)
            {
                value = -value;
            }
            while (value != 0)
            {
                stack[stackHead++] = (char)('0' + value % 10);
                value /= 10;
            }
            if (negative)
            {
                stack[stackHead++] = '-';
            }
            return stackHead;
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