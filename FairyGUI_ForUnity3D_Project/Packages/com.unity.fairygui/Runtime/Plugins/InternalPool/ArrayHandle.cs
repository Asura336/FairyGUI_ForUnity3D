using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;

namespace FairyGUI.Foundations.Collections
{
    /// <summary>
    /// 从全局对象池取出的数组句柄，配合 using 语法使用，线程安全。
    /// 见：<see cref="New(int)"/>,
    /// <see cref="ArrayPool{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal readonly struct ArrayHandle<T> : IDisposable, IList<T>, IReadOnlyList<T>
    {
        static readonly ArrayPool<T> s_pool = ArrayPool<T>.Shared;

        readonly T[] m_body;
        readonly int m_count;

        ArrayHandle(T[] values, int count)
        {
            m_body = values;
            m_count = count;
        }

        /// <summary>
        /// 从池中取出一个数组，指定其长度。
        /// 数组成员可能未初始化
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public static ArrayHandle<T> New(int count)
        {
            return new ArrayHandle<T>(s_pool.Rent(count), count);
        }

        /// <summary>
        /// 从池中取出一个数组，指定其长度和填充的初始值
        /// </summary>
        /// <param name="count"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ArrayHandle<T> New(int count, T value)
        {
            var array = s_pool.Rent(count);
            for (int i = 0; i < count; i++)
            {
                array[i] = value;
            }
            return new ArrayHandle<T>(array, count);
        }

        public void Dispose()
        {
            if (m_body != null) { s_pool.Return(m_body); }
        }

        #region IList<T>
        public int IndexOf(T item)
        {
            return ((IList<T>)m_body).IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            ((IList<T>)m_body).Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            ((IList<T>)m_body).RemoveAt(index);
        }

        public T this[int index] { get => m_body[index]; set => m_body[index] = value; }

        public void Add(T item)
        {
            ((ICollection<T>)m_body).Add(item);
        }

        public void Clear()
        {
            ((ICollection<T>)m_body).Clear();
        }

        public bool Contains(T item)
        {
            return ((ICollection<T>)m_body).Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ((ICollection<T>)m_body).CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return ((ICollection<T>)m_body).Remove(item);
        }

        public int Count => m_count;

        public bool IsReadOnly => ((ICollection<T>)m_body).IsReadOnly;

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }
        #endregion

        /// <summary>
        /// 句柄储存的内部数组，数组长度可能和 <see cref="Count"/> 不相等
        /// </summary>
        public T[] Body => m_body ?? Array.Empty<T>();
        /// <summary>
        /// 指示句柄已分配有效的数组
        /// </summary>
        public bool Valid => m_body != null;


        public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
        {
            int m_index;
            readonly ArrayHandle<T> m_body;

            public Enumerator(ArrayHandle<T> body)
            {
                m_body = body;
                m_index = -1;
            }

            public readonly T Current => m_body[m_index];

            public bool MoveNext()
            {
                if (m_index < m_body.Count - 1)
                {
                    ++m_index;
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                m_index = -1;
            }

            readonly object IEnumerator.Current => m_body[m_index]!;

            public readonly void Dispose()
            {
                // pass
            }
        }
    }
}