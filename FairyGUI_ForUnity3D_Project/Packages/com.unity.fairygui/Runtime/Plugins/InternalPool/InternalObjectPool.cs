using System;

namespace FairyGUI.Foundations.Collections
{
    internal class InternalObjectPool<T> where T : class
    {
        //internal readonly ConcurrentBag<T> m_objects;
        internal readonly T[] m_objects;
        uint m_count;
        static readonly object s_lock = new object();

        protected readonly Func<T> m_factory;
        protected readonly uint maxSize = 256;
        protected readonly Action<T> m_releaseMethod;

        public InternalObjectPool(Func<T> factory,
            Action<T> releaseMethod,
            uint maxSize)
        {
            //m_objects = new ConcurrentBag<T>();
            m_objects = new T[maxSize];
            m_count = 0;
            m_factory = factory;
            m_releaseMethod = releaseMethod;
            this.maxSize = maxSize;
        }

        public T Begin()
        {
            //return m_objects.TryTake(out var item) ? item : m_factory();

            lock (s_lock)
            {
                if (m_count == 0)
                {
                    return m_factory();
                }
                else
                {
                    m_count--;
                    var o = m_objects[m_count];
                    m_objects[m_count] = null!;
                    return o;
                }
            }
        }

        public virtual void End(T obj)
        {
            //int count = m_objects.Count;
            //if ((count == 0 || !m_objects.Contains(obj)) && count < maxSize)
            //{
            //    m_releaseMethod(obj);
            //    m_objects.Add(obj);
            //}

            lock (s_lock)
            {
                if (obj != null
                    && m_count != maxSize
                    && (m_count == 0 || Array.IndexOf(m_objects, obj, 0, (int)m_count) == -1))
                {
                    m_releaseMethod(obj);
                    m_objects[m_count] = obj;
                    m_count++;
                }
            }
        }
    }
}
