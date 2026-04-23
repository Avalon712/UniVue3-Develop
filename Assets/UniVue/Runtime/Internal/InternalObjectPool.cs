using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace UniVue.Internal
{
    internal sealed class InternalObjectPool<T> where T : class, new()
    {
        private static readonly ConcurrentDictionary<Type, object> _pools = new();
        private readonly Func<T> _createFunc;
        private readonly Action<T> _disposeFunc;
        private readonly Stack<T> _items = new(32);
        private readonly HashSet<T> _itemsSet = new(InternalReferenceComparer<T>.Shared);

        private uint _maxCapacity = uint.MaxValue;

        private InternalObjectPool()
        {
        }

        public InternalObjectPool(Func<T> createFunc, Action<T> disposeFunc)
        {
            _createFunc = createFunc;
            _disposeFunc = disposeFunc;
        }

        public int Count => _items.Count;

        /// <summary>
        /// 最大容量，超过该容量的对象将不再被缓存，而是直接丢弃（默认为uint.MaxValue，即不限制容量）
        /// </summary>
        public uint MaxCapacity
        {
            get => _maxCapacity;
            set
            {
                _maxCapacity = value;
                while (Count > value) _itemsSet.Remove(_items.Pop());
            }
        }

        public static InternalObjectPool<T> Shared
        {
            get
            {
                Type type = typeof(T);
                if (!_pools.TryGetValue(type, out object poolObj))
                {
                    poolObj = new InternalObjectPool<T>();
                    _pools[type] = poolObj;
                }

                return (InternalObjectPool<T>)poolObj;
            }
        }

        public T Rent()
        {
            if (_items.Count <= 0)
                return _createFunc != null ? _createFunc.Invoke() : new T();

            T item = _items.Pop();
            _itemsSet.Remove(item);
            return item;
        }

        public T Rent(Func<T> createFunc)
        {
            if (_items.Count <= 0)
                return createFunc != null ? createFunc.Invoke() : new T();

            T item = _items.Pop();
            _itemsSet.Remove(item);
            return item;
        }

        public void Return(ref T item, Action<T> disposeFunc)
        {
            if (item == null) return;
            disposeFunc?.Invoke(item);
            if (Count >= MaxCapacity || !_itemsSet.Add(item)) return;
            _items.Push(item);
            item = null;
        }

        public void Return(ref T item)
        {
            if (item == null) return;
            _disposeFunc?.Invoke(item);
            if (Count >= MaxCapacity || !_itemsSet.Add(item)) return;
            _items.Push(item);
            item = null;
        }
    }
}