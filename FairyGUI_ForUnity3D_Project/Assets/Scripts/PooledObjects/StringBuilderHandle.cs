#pragma warning disable CS3021 // 由于程序集没有 CLSCompliant 特性，因此类型或成员不需要 CLSCompliant 特性
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace FairyGUI.Foundations.Collections
{
    /// <summary>
    /// 从全局对象池取出的 <see cref="StringBuilder"/> 句柄，配合 using 语法使用，线程安全。
    /// 见：<see cref="New()"/>
    /// </summary>
    internal readonly struct StringBuilderHandle : IDisposable, ISerializable
    {
        const int defaultCapacity = 1024;
        static readonly InternalObjectPool<StringBuilder> s_pool = new InternalObjectPool<StringBuilder>(
            factory: () => new StringBuilder(defaultCapacity),
            releaseMethod: self => self.Length = 0,
            maxSize: 256);

        readonly StringBuilder m_body;
        StringBuilderHandle(StringBuilder body) => m_body = body;
        public static StringBuilderHandle New() => new StringBuilderHandle(s_pool.Begin());
        public static StringBuilderHandle New(string src)
        {
            var obj = s_pool.Begin();
            obj.Append(src);
            return new StringBuilderHandle(obj);
        }
        public static StringBuilderHandle New(string src, int startIndex, int count)
        {
            var obj = s_pool.Begin();
            obj.Append(src, startIndex, count);
            return new StringBuilderHandle(obj);
        }

        public override string ToString()
        {
            return m_body?.ToString() ?? string.Empty;
        }

        public string ToStringThenDispose()
        {
            string result = this.ToString();
            this.Dispose();
            return result;
        }

        public void Dispose()
        {
            if (m_body != null) { s_pool.End(m_body); }
        }

        #region string builder
        public StringBuilder Append(char value, int repeatCount) => m_body.Append(value, repeatCount);
        [CLSCompliant(false)]
        public unsafe StringBuilder Append(char* value, int valueCount) => m_body.Append(value, valueCount);
        public StringBuilder Append(byte value) => m_body.Append(value);
        public StringBuilder Append(bool value) => m_body.Append(value);
        [CLSCompliant(false)]
        public StringBuilder Append(ulong value) => m_body.Append(value);
        [CLSCompliant(false)]
        public StringBuilder Append(uint value) => m_body.Append(value);
        [CLSCompliant(false)]
        public StringBuilder Append(ushort value) => m_body.Append(value);
        public StringBuilder Append(char value) => m_body.Append(value);
        public StringBuilder Append(StringBuilder value) => m_body.Append(value);
        public StringBuilder Append(string value, int startIndex, int count) => m_body.Append(value, startIndex, count);
        public StringBuilder Append(string value) => m_body.Append(value);
        public StringBuilder Append(StringBuilder value, int startIndex, int count)
        {
            for (int i = startIndex; i < count; i++)
            {
                m_body.Append(value[i]);
            }
            return this;
        }

        [CLSCompliant(false)]
        public StringBuilder Append(sbyte value) => m_body.Append(value);
        public StringBuilder Append(ReadOnlySpan<char> value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                m_body.Append(value[i]);
            }
            return this;
        }

        public StringBuilder Append(object value) => m_body.Append(value);
        public StringBuilder Append(long value) => m_body.Append(value);
        public StringBuilder Append(int value) => m_body.Append(value);
        public StringBuilder Append(short value) => m_body.Append(value);
        public StringBuilder Append(double value) => m_body.Append(value);
        public StringBuilder Append(char[] value) => m_body.Append(value);
        public StringBuilder Append(char[] value, int startIndex, int charCount) => m_body.Append(value, startIndex, charCount);
        public StringBuilder Append(float value) => m_body.Append(value);
        public StringBuilder Append(decimal value) => m_body.Append(value);
        public StringBuilder AppendFormat(string format, params object[] args) => m_body.AppendFormat(format, args);
        public StringBuilder AppendFormat(string format, object arg0, object arg1, object arg2) => m_body.AppendFormat(format, arg0, arg1, arg2);
        public StringBuilder AppendFormat(IFormatProvider provider, string format, object arg0, object arg1, object arg2) => m_body.AppendFormat(provider, format, arg0, arg1, arg2);
        public StringBuilder AppendFormat(string format, object arg0) => m_body.AppendFormat(format, arg0);
        public StringBuilder AppendFormat(IFormatProvider provider, string format, params object[] args) => m_body.AppendFormat(provider, format, args);
        public StringBuilder AppendFormat(IFormatProvider provider, string format, object arg0, object arg1) => m_body.AppendFormat(provider, format, arg0, arg1);
        public StringBuilder AppendFormat(IFormatProvider provider, string format, object arg0) => m_body.AppendFormat(provider, format, arg0);
        public StringBuilder AppendFormat(string format, object arg0, object arg1) => m_body.AppendFormat(format, arg0, arg1);
        public StringBuilder AppendJoin<T>(string separator, IEnumerable<T> values) => m_body.Append(string.Join(separator, values));
        public StringBuilder AppendJoin(string separator, params string[] values) => m_body.Append(string.Join(separator, values));
        public StringBuilder AppendJoin(string separator, params object[] values) => m_body.Append(string.Join(separator, values));
        public StringBuilder AppendJoin(char separator, params object[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                m_body.Append(values[i]);
                if (i != values.Length - 1)
                {
                    m_body.Append(separator);
                }
            }
            return this;
        }
        public StringBuilder AppendJoin(char separator, params string[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                m_body.Append(values[i]);
                if (i != values.Length - 1)
                {
                    m_body.Append(separator);
                }
            }
            return this;
        }
        public StringBuilder AppendJoin<T>(char separator, IEnumerable<T> values)
        {
            int count = values.Count();
            int index = 0;
            foreach (var s in values)
            {
                m_body.Append(s);
                if (index != count - 1)
                {
                    m_body.Append(separator);
                }
                index++;
            }
            return this;
        }
        public StringBuilder AppendLine() => m_body.AppendLine();
        public StringBuilder AppendLine(string value) => m_body.AppendLine(value);
        public StringBuilder Clear() => m_body.Clear();
        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) => m_body.CopyTo(sourceIndex, destination, destinationIndex, count);
        public void CopyTo(int sourceIndex, Span<char> destination, int count)
        {
            for (int i = 0; i < count; i++)
            {
                destination[i] = m_body[sourceIndex + i];
            }
        }

        public int EnsureCapacity(int capacity) => m_body.EnsureCapacity(capacity);
        #endregion

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ((ISerializable)m_body).GetObjectData(info, context);
        }

        public StringBuilder Body => m_body;
        public bool IsCreated => m_body != null;
        public int Length
        {
            get => m_body.Length;
            set => m_body.Length = value;
        }

        public char this[int index]
        {
            get => m_body[index];
            set => m_body[index] = value;
        }

        public static implicit operator StringBuilder(StringBuilderHandle self) => self.m_body;
    }
}
