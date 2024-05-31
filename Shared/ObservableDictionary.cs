using System;
using System.Collections.Generic;

namespace Omni.Shared.Collections
{
    public class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public event Action<TKey, TValue> OnItemAdded;
        public event Action<TKey, TValue> OnItemRemoved;
        public event Action<TKey, TValue> OnItemUpdated;

        public new TValue this[TKey key]
        {
            get { return base[key]; }
            set
            {
                base[key] = value;
                OnItemUpdated?.Invoke(key, value);
            }
        }

        public new void Add(TKey key, TValue value)
        {
            base.Add(key, value);
            OnItemAdded?.Invoke(key, value);
        }

        public new bool Remove(TKey key)
        {
            bool success = base.Remove(key, out TValue value);
            if (success)
                OnItemRemoved?.Invoke(key, value);
            return success;
        }

        public new bool TryAdd(TKey key, TValue value)
        {
            bool success = base.TryAdd(key, value);
            if (success)
                OnItemAdded?.Invoke(key, value);
            return success;
        }
    }
}
