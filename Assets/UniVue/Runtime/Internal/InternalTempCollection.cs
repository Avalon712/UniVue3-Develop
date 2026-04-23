using System;
using System.Collections;
using System.Collections.Generic;

namespace UniVue.Internal
{
    internal struct InternalTempCollection<TCollection, TItem> : IDisposable,
                                                                 IEquatable<InternalTempCollection<TCollection, TItem>>,
                                                                 IEnumerable<TItem>
        where TCollection : class, ICollection<TItem>, new()
    {
        public TCollection Collection { get; private set; }

        private bool _disposed;

        public InternalTempCollection(ICollection<TItem> collection)
        {
            _disposed = false;
            Collection = InternalObjectPool<TCollection>.Shared.Rent();
            Collection.Clear();
            if (collection != null)
                foreach (TItem item in collection)
                    Collection.Add(item);
        }

        public InternalTempCollection(IEnumerable<TItem> collection)
        {
            _disposed = false;
            Collection = InternalObjectPool<TCollection>.Shared.Rent();
            Collection.Clear();
            if (collection != null)
                foreach (TItem item in collection)
                    Collection.Add(item);
        }

        public static implicit operator TCollection(InternalTempCollection<TCollection, TItem> collection)
        {
            return collection.Collection;
        }

        public IEnumerator<TItem> GetEnumerator()
        {
            return Collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Collection.Clear();
            TCollection temp = Collection;
            InternalObjectPool<TCollection>.Shared.Return(ref temp);
            Collection = null;
        }

        public bool Equals(InternalTempCollection<TCollection, TItem> other)
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            return false;
        }

        public override int GetHashCode()
        {
            return Collection.GetHashCode();
        }
    }
}