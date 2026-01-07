using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FairyGUI.Foundations.Collections
{
    /// <summary>
    /// 从全局对象池取出的列表句柄，配合 using 语法使用，线程安全。
    /// 见：<see cref="New()"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal readonly struct ListHandle<T> : IDisposable, IList<T>, IReadOnlyList<T>
    {
        const int defaultCapacity = 1024;
        static readonly InternalCollectionPool<T, List<T>> s_pool = new InternalCollectionPool<T, List<T>>(
            () => new List<T>(defaultCapacity));

        readonly List<T> m_body;

        ListHandle(List<T> obj) => m_body = obj;

        public static ListHandle<T> New() => new ListHandle<T>(s_pool.Begin());
        public static ListHandle<T> New(IEnumerable<T> src)
        {
            var list = s_pool.Begin();
            list.AddRange(src);
            return new ListHandle<T>(list);
        }

        public void Dispose()
        {
            if (m_body != null) { s_pool.End(m_body); }
        }

        #region IList<T>
        public int IndexOf(T item) => ((IList<T>)m_body).IndexOf(item);

        public void Insert(int index, T item) => ((IList<T>)m_body).Insert(index, item);

        public void RemoveAt(int index) => ((IList<T>)m_body).RemoveAt(index);

        public T this[int index] { get => ((IList<T>)m_body)[index]; set => ((IList<T>)m_body)[index] = value; }

        public void Add(T item) => ((ICollection<T>)m_body).Add(item);

        public void Clear() => ((ICollection<T>)m_body).Clear();

        public bool Contains(T item) => ((ICollection<T>)m_body).Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => ((ICollection<T>)m_body).CopyTo(array, arrayIndex);

        public bool Remove(T item) => ((ICollection<T>)m_body).Remove(item);

        public int Count => ((ICollection<T>)m_body).Count;

        public bool IsReadOnly => ((ICollection<T>)m_body).IsReadOnly;

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)m_body).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)m_body).GetEnumerator();
        #endregion

        public void AddRange(IEnumerable<T> collection) => m_body.AddRange(collection);
        public ReadOnlyCollection<T> AsReadOnly() => m_body.AsReadOnly();
        public int BinarySearch(T item) => m_body.BinarySearch(item);
        public int BinarySearch(T item, IComparer<T> comparer) => m_body.BinarySearch(item, comparer);
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer) => m_body.BinarySearch(index, count, item, comparer);
        public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter) => m_body.ConvertAll(converter);
        public void CopyTo(int index, T[] array, int arrayIndex, int count) => m_body.CopyTo(index, array, arrayIndex, count);
        public void CopyTo(T[] array) => m_body.CopyTo(array);
        public bool Exists(Predicate<T> match) => m_body.Exists(match);
        public T Find(Predicate<T> match) => m_body.Find(match);
        public List<T> FindAll(Predicate<T> match) => m_body.FindAll(match);
        public int FindIndex(int startIndex, int count, Predicate<T> match) => m_body.FindIndex(startIndex, count, match);
        public int FindIndex(int startIndex, Predicate<T> match) => m_body.FindIndex(startIndex, match);
        public int FindIndex(Predicate<T> match) => m_body.FindIndex(match);
        public T FindLast(Predicate<T> match) => m_body.FindLast(match);
        public int FindLastIndex(int startIndex, int count, Predicate<T> match) => m_body.FindLastIndex(startIndex, count, match);
        public int FindLastIndex(int startIndex, Predicate<T> match) => m_body.FindLastIndex(startIndex, match);
        public int FindLastIndex(Predicate<T> match) => m_body.FindLastIndex(match);
        public void ForEach(Action<T> action) => m_body.ForEach(action);
        public List<T> GetRange(int index, int count) => m_body.GetRange(index, count);
        public int IndexOf(T item, int index, int count) => m_body.IndexOf(item, index, count);
        public int IndexOf(T item, int index) => m_body.IndexOf(item, index);
        public void InsertRange(int index, IEnumerable<T> collection) => m_body.InsertRange(index, collection);
        public int LastIndexOf(T item) => m_body.LastIndexOf(item);
        public int LastIndexOf(T item, int index) => m_body.LastIndexOf(item, index);
        public int LastIndexOf(T item, int index, int count) => m_body.LastIndexOf(item, index, count);
        public int RemoveAll(Predicate<T> match) => m_body.RemoveAll(match);
        public void RemoveRange(int index, int count) => m_body.RemoveRange(index, count);
        public void Reverse(int index, int count) => m_body.Reverse(index, count);
        public void Reverse() => m_body.Reverse();
        public void Sort(Comparison<T> comparison) => m_body.Sort(comparison);
        public void Sort(int index, int count, IComparer<T> comparer) => m_body.Sort(index, count, comparer);
        public void Sort() => m_body.Sort();
        public void Sort(IComparer<T> comparer) => m_body.Sort(comparer);
        public T[] ToArray() => m_body.ToArray();
        public void TrimExcess() => m_body.TrimExcess();
        public bool TrueForAll(Predicate<T> match) => m_body.TrueForAll(match);

        public List<T> Body => m_body;
        public int Capacity => m_body.Capacity;

        public static implicit operator List<T>(ListHandle<T> self) => self.m_body;
    }
}
