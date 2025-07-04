using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MemoryPack;
using Newtonsoft.Json;
using Omni.Inspector;
using Omni.Shared;
using UnityEngine;

#pragma warning disable

namespace Omni.Collections
{
    /// <summary>
    /// A serializable dictionary collection that provides events for item additions, removals, and updates.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    [MemoryPackable(GenerateType.Collection)]
    [Serializable, DeclareHorizontalGroup("Key/Value"), DeclareHorizontalGroup("HorizontalGroup")]
    [Nested]
    public partial class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
        where TKey : notnull
    {
        [ListDrawerSettings(AlwaysExpanded = true, HideAddButton = true, HideRemoveButton = true)]
        [Group("Key/Value"), DisableInPlayMode, SerializeField]
        [MemoryPackIgnore]
        [JsonIgnore]
        private List<TKey> _keys = new List<TKey>();

        [ListDrawerSettings(AlwaysExpanded = true, HideAddButton = true, HideRemoveButton = true)]
        [Group("Key/Value"), OnValueChanged(nameof(OnCollectionChanged)), SerializeField]
        [MemoryPackIgnore]
        [JsonIgnore]
        private List<TValue> _values = new List<TValue>();

        [JsonProperty("KvP"), MemoryPackInclude]
        private readonly Dictionary<TKey, TValue> _internalReference = new Dictionary<TKey, TValue>();

        public event Action<TKey, TValue> OnItemAdded;
        public event Action<TKey, TValue> OnItemRemoved;
        public event Action<TKey, TValue> OnItemUpdated;
        public Action<bool> OnUpdate;

        private bool _detectInspectorChanges = false;
        public ObservableDictionary() : this(false) { }

        public ObservableDictionary(bool detectInspectorChanges)
        {
            _detectInspectorChanges = detectInspectorChanges;
#if OMNI_DEBUG
            var tKey = typeof(TKey);
            var tValue = typeof(TValue);
            bool ignoreCheck = false;
            if (tKey.Namespace?.StartsWith("UnityEngine") == true || tValue.Namespace?.StartsWith("UnityEngine") == true)
            {
                if (tKey.IsValueType || tValue.IsValueType)
                    ignoreCheck = true;
            }

            if (!ignoreCheck)
            {
                if (!tValue.IsSerializable)
                {
                    throw new InvalidOperationException(
                        $"Type '{tValue.FullName}' must implement the Serializable attribute and have public fields to be used as a value in ObservableDictionary.");
                }

                if (!tKey.IsSerializable)
                {
                    throw new InvalidOperationException(
                        $"Type '{tValue.FullName}' must implement the Serializable attribute and have public fields to be used as a value in ObservableDictionary.");
                }
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

        [Group("HorizontalGroup")]
        [Button("Add Key")]
        [DisableInPlayMode]
        private void AddKeyValuePair()
        {
            try
            {
                int index = _keys.Count;
                TKey key = (TKey)Convert.ChangeType(index, typeof(TKey));

                _keys.Add(key);
                _values.Add(default);
            }
            catch
            {
                _keys.Add(default);
                _values.Add(default);
            }
        }

        [Group("HorizontalGroup")]
        [Button("Remove Key")]
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
            try
            {
                if (!Application.isPlaying)
                    return;
            }
            catch { }

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
                        UnityEngine.Debug.Log($"Updated: {key} = {value}");
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
            if (_detectInspectorChanges)
            {
                OnCollectionChanged();
            }

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