using System;
using System.Collections.Generic;

namespace FairyGUI.Foundations.Collections
{
    internal sealed class InternalCollectionPool<TValue, TCollection> : InternalObjectPool<TCollection>
      where TCollection : class, ICollection<TValue>
    {
        public InternalCollectionPool(Func<TCollection> factory,
            uint maxSize = 256) : base(factory, c => c.Clear(), maxSize)
        { }
    }
}