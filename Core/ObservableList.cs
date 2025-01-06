using System;
using System.Collections.Generic;
using MemoryPack;
using Omni.Inspector;
using UnityEngine;

namespace Omni.Collections
{
    /// <summary>
    /// A serializable list that provides notifications for item modifications, additions, and removals.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    [MemoryPackable(GenerateType.Collection)]
    [Serializable]
    public partial class ObservableList<T> : List<T>
    {
        [ListDrawerSettings(AlwaysExpanded = true)] [SerializeField] [LabelText("Items")]
        private List<T> _internalReference = new List<T>();

        public event Action<int, T> OnItemAdded;
        public event Action<int, T> OnItemRemoved;
        public event Action<int, T> OnItemUpdated;
        public Action<bool> OnUpdate;

        public ObservableList()
        {
#if OMNI_DEBUG
            if (!typeof(T).IsSerializable)
            {
                throw new InvalidOperationException(
                    $"The value type '{typeof(T).FullName}' must be serializable.");
            }
#endif
            _internalReference = this;
        }

        public new T this[int index]
        {
            get => base[index];
            set
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                base[index] = value;
                OnItemUpdated?.Invoke(index, value);
            }
        }

        public new void Add(T item)
        {
            base.Add(item);
            OnItemAdded?.Invoke(Count - 1, item);
        }

        public new void Insert(int index, T item)
        {
            base.Insert(index, item);
            OnItemAdded?.Invoke(index, item);
        }

        public new bool Remove(T item)
        {
            int index = IndexOf(item);
            bool success = base.Remove(item);
            if (success)
                OnItemRemoved?.Invoke(index, item);
            return success;
        }

        public new void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            T item = this[index];
            base.RemoveAt(index);
            OnItemRemoved?.Invoke(index, item);
        }

        public new void Clear()
        {
            for (int i = 0; i < Count; i++)
                OnItemRemoved?.Invoke(i, this[i]);

            base.Clear();
            OnUpdate?.Invoke(true);
        }
    }
}