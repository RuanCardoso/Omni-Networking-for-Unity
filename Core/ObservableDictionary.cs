using System;
using System.Collections.Generic;
using System.Diagnostics;
using MemoryPack;
using Omni.Inspector;
using UnityEngine;
using System.Linq;
using Omni.Shared;

#pragma warning disable

namespace Omni.Collections
{
    /// <summary>
    /// A serializable dictionary collection that provides events for item additions, removals, and updates.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    [MemoryPackable(GenerateType.Collection)]
    [Serializable, DeclareHorizontalGroup("Key/Value")]
    public partial class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
        where TKey : notnull
    {
        [ListDrawerSettings(AlwaysExpanded = true, HideAddButton = true, HideRemoveButton = true)]
        [SerializeField, Group("Key/Value"), DisableInPlayMode]
        [MemoryPackIgnore]
        private List<TKey> _keys = new List<TKey>();

        [ListDrawerSettings(AlwaysExpanded = true, HideAddButton = true, HideRemoveButton = true)]
        [SerializeField, Group("Key/Value"), OnValueChanged(nameof(OnCollectionChanged))]
        [MemoryPackIgnore]
        private List<TValue> _values = new List<TValue>();

        private readonly Dictionary<TKey, TValue> _internalReference = new Dictionary<TKey, TValue>();

        public event Action<TKey, TValue> OnItemAdded;
        public event Action<TKey, TValue> OnItemRemoved;
        public event Action<TKey, TValue> OnItemUpdated;
        public Action<bool> OnUpdate;

        public ObservableDictionary()
        {
#if OMNI_DEBUG
            var tKey = typeof(TKey);
            var tValue = typeof(TValue);
            if (tKey.Namespace?.StartsWith("UnityEngine") == true || tValue.Namespace?.StartsWith("UnityEngine") == true)
            {
                if (tKey.IsValueType || tValue.IsValueType)
                    return;
            }

            if (!tValue.IsSerializable)
            {
                throw new InvalidOperationException(
                    $"The value type '{tValue.FullName}' must be serializable.");
            }

            if (!tKey.IsSerializable)
            {
                throw new InvalidOperationException(
                    $"The key type '{tKey.FullName}' must be serializable.");
            }
#endif
            _internalReference = this;
#if UNITY_EDITOR
            OnItemAdded = (key, value) =>
            {
                _keys.Add(key);
                _values.Add(value);
            };

            OnItemRemoved = (key, value) =>
            {
                _keys.Remove(key);
                _values.Remove(value);
            };

            OnItemUpdated = (key, value) =>
            {
                if (!_keys.Contains(key))
                {
                    if (_internalReference.ContainsKey(key))
                    {
                        _keys.Add(key);
                        _values.Add(value);
                    }
                }
            };

            OnUpdate = (_) =>
            {
                _keys = _internalReference.Keys.ToList();
                _values = _internalReference.Values.ToList();
            };
#endif
        }

        public new TValue this[TKey key]
        {
            get { return base[key]; }
            set
            {
                if (typeof(TValue).IsValueType)
                {
                    if (base.TryGetValue(key, out TValue currentValue))
                    {
                        if (currentValue.Equals(value))
                            return;
                    }
                }

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

        public new bool Remove(TKey key, out TValue value)
        {
            bool success = base.Remove(key, out value);
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

        public new void Clear()
        {
            foreach (KeyValuePair<TKey, TValue> pair in _internalReference)
                OnItemRemoved?.Invoke(pair.Key, pair.Value);

            base.Clear();
            OnUpdate?.Invoke(true);
        }

        [Button("Add Key")]
        [DisableInPlayMode]
        private void AddKeyValuePair()
        {
            _keys.Add(default);
            _values.Add(default);
        }

        [Button("Remove Last Key")]
        [DisableInPlayMode]
        private void RemoveKeyValuePair()
        {
            if (_keys.Count > 0)
                _keys.RemoveAt(_keys.Count - 1);

            if (_values.Count > 0)
                _values.RemoveAt(_values.Count - 1);
        }

        [Conditional("UNITY_EDITOR")]
        private void OnCollectionChanged()
        {
            if (!Application.isPlaying)
                return;

            if (!_values.SequenceEqual(_internalReference.Values))
            {
                for (int i = 0; i < _keys.Count; i++)
                {
                    var key = _keys[i];
                    if (_internalReference.ContainsKey(key))
                    {
                        var value = _values[i];
                        if (_internalReference[key].Equals(value))
                            continue;

                        _internalReference[key] = value;
                        OnItemUpdated?.Invoke(key, value);
                    }
                }

                _keys = _internalReference.Keys.ToList();
                _values = _internalReference.Values.ToList();
            }
        }

        public void OnBeforeSerialize()
        {
            OnCollectionChanged();
        }

        public void OnAfterDeserialize()
        {
            _internalReference.Clear();
            if (_keys.Count != _values.Count)
                return;

            for (int i = 0; i < _keys.Count; i++)
            {
                if (!_internalReference.TryAdd(_keys[i], _values[i]))
                    NetworkLogger.Print($"Deserialization error: Duplicate key '{_keys[i]}' found in ObservableDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>.", NetworkLogger.LogType.Warning);
            }
        }
    }
}