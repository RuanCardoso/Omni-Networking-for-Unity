using Omni.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;

#pragma warning disable

namespace Omni.Core
{
    // Hacky: DIRTY CODE!
    // This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
    // Despite its appearance, this approach is essential to achieve high performance.
    // Avoid refactoring as these techniques are crucial for optimizing execution speed.
    // Works with il2cpp.

    internal interface IPropertyInfo
    {
        string Name { get; }
        byte Id { get; }
    }

    internal interface IPropertyInfo<T>
    {
        Func<T> Invoke { get; set; }
    }

    internal class PropertyInfo<T> : IPropertyInfo, IPropertyInfo<T>
    {
        internal PropertyInfo(string name, byte id)
        {
            Name = name;
            Id = id;
        }

        public Func<T> Invoke { get; set; }
        public string Name { get; }
        public byte Id { get; }
    }

    internal class NetworkVariableField
    {
        internal int PropertyId { get; }
        internal bool RequiresOwnership { get; }
        internal bool IsClientAuthority { get; }
        internal bool CheckEquality { get; }

        internal NetworkVariableField(int propertyId, bool requiresOwnership, bool isClientAuthority,
            bool checkEquality)
        {
            PropertyId = propertyId;
            RequiresOwnership = requiresOwnership;
            IsClientAuthority = isClientAuthority;
            CheckEquality = checkEquality;
        }
    }

    public class NetworkVariablesBehaviour : MonoBehaviour
    {
        private readonly Dictionary<string, IPropertyInfo> runtimeProperties = new();
        internal readonly Dictionary<byte, NetworkVariableField> networkVariables = new();

        internal void FindAllNetworkVariables()
        {
            // Registers notifications for changes in the collection, enabling automatic updates when the collection is modified.
            ___NotifyCollectionChange___();

            Type type = GetType();
            FieldInfo[] fieldInfos = type.GetFields(System.Reflection.BindingFlags.Instance |
                                                    System.Reflection.BindingFlags.Public |
                                                    System.Reflection.BindingFlags.NonPublic);

            foreach (var field in fieldInfos)
            {
                NetworkVariableAttribute fieldAttr = field.GetCustomAttribute<NetworkVariableAttribute>();
                if (fieldAttr == null)
                    continue;

                string fieldName = field.Name;
                if (fieldName.StartsWith("m_"))
                {
                    fieldName = fieldName[2..];
                }
                else
                {
                    if (!char.IsUpper(fieldName[0]))
                    {
                        fieldName = char.ToUpper(fieldName[0]) + fieldName[1..];
                    }
                }

                PropertyInfo property = type.GetProperty(fieldName,
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);

                if (property == null)
                    continue;

                NetworkVariableAttribute propertyAttr =
                    property.GetCustomAttribute<NetworkVariableAttribute>();

                if (propertyAttr == null)
                    continue;

                if (!networkVariables.TryAdd(propertyAttr.Id,
                        new NetworkVariableField(propertyAttr.Id, fieldAttr.RequiresOwnership,
                            fieldAttr.IsClientAuthority, fieldAttr.CheckEquality)))
                {
                    throw new NotSupportedException(
                        $"[NetworkVariable] -> Duplicate network variable ID found: {propertyAttr.Id}. Ensure all network variable IDs within a class are unique. Field: {field.Name}");
                }
            }
        }

        internal DataBuffer CreateHeader<T>(T @object, byte id)
        {
            DataBuffer message = NetworkManager.Pool.Rent(); // disposed by the caller
            message.Write(id);

            // If the object implements ISerializable, serialize it.
            if (@object is IMessage data)
            {
                using var serializedData = data.Serialize();
                serializedData.CopyTo(message);
                return message;
            }

            message.WriteAsBinary(@object);
            return message;
        }

        internal IPropertyInfo GetPropertyInfoWithCallerName<T>(string callerName, BindingFlags flags)
        {
            if (!runtimeProperties.TryGetValue(callerName, out IPropertyInfo memberInfo))
            {
                // Reflection is slow, but cached for performance optimization!
                // Delegates are used to avoid reflection overhead, it is much faster, like a direct call.

                PropertyInfo propertyInfo = GetType().GetProperty(callerName, (System.Reflection.BindingFlags)flags) ??
                                            throw new NullReferenceException(
                                                $"NetworkVariable: Property not found: {callerName}. Use the other overload of this function.");

                string name = propertyInfo.Name;
                NetworkVariableAttribute attribute = propertyInfo.GetCustomAttribute<NetworkVariableAttribute>() ??
                                                     throw new NullReferenceException(
                                                         $"NetworkVariable: NetworkVariableAttribute not found on property: {name}. Ensure it has 'NetworkVariable' attribute.");

                MethodInfo getMethod = propertyInfo.GetMethod;
                if (getMethod == null)
                {
                    throw new NullReferenceException(
                        $"NetworkVariable: GetMethod not found on property: {name}. Ensure it has 'NetworkVariable' attribute."
                    );
                }

                byte id = attribute.Id;
                if (id <= 0)
                {
                    throw new ArgumentException($"NetworkVariable: Id must be greater than 0.");
                }

                memberInfo = new PropertyInfo<T>(name, id);
                // hack: performance optimization!
                ((IPropertyInfo<T>)memberInfo).Invoke = getMethod.CreateDelegate(typeof(Func<T>), this) as Func<T>;
                runtimeProperties.Add(callerName, memberInfo);
                return memberInfo;
            }

            return memberInfo;
        }

        // This method is intended to be overridden by the caller using source generators and reflection techniques. Magic wow!
        // https://github.com/RuanCardoso/OmniNetSourceGenerator
        // never override this method!
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't override this method! The source generator will override it.")]
        protected virtual void ___OnPropertyChanged___(string propertyName, byte propertyId, NetworkPeer peer,
            DataBuffer buffer)
        {
            // The overriden method must call base(SourceGenerator).
            if (peer == null)
            {
                OnClientPropertyChanged(propertyName, propertyId);
            }
            else
            {
                OnServerPropertyChanged(propertyName, propertyId, peer);
            }
        }

        // This method is intended to be overridden by the caller using source generators and reflection techniques. Magic wow!
        // https://github.com/RuanCardoso/OmniNetSourceGenerator
        // never override this method!
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't override this method! The source generator will override it.")]
        protected virtual void ___NotifyCollectionChange___()
        {
        }

        /// <summary>
        /// Handles property changes on the client side based on the provided property ID.
        /// </summary>
        /// <param name="id">The ID of the property that has changed.</param>
        protected virtual void OnClientPropertyChanged(string propertyName, int propertyId)
        {
        }

        /// <summary>
        /// Handles property changes on the server side based on the provided property ID and network peer.
        /// </summary>
        /// <param name="id">The ID of the property that has changed.</param>
        /// <param name="peer">The network peer associated with the property change.</param>
        protected virtual void OnServerPropertyChanged(string propertyName, int propertyId, NetworkPeer peer)
        {
        }

        /// <summary>
        /// Synchronizes the current state of all network variables and other relevant states, 
        /// ensuring that any updates are transmitted to the specified network peer. This method triggers 
        /// notifications for registered listeners, allowing the peer to receive and apply the 
        /// latest server-side data and changes. The synchronization is specifically scoped to the provided 
        /// network peer.
        /// </summary>
        /// <param name="peer">The network peer to synchronize the state with.</param>
        protected virtual void SyncNetworkState(NetworkPeer peer)
        {
        }

        /// <summary>
        /// Compares two values of type T for deep equality for network variables.
        /// </summary>
        /// <typeparam name="T">The type of the values to compare.</typeparam>
        /// <param name="oldValue">The old value to compare.</param>
        /// <param name="newValue">The new value to compare.</param>
        /// <param name="name">The name of the network variable.</param>
        /// <returns>True if the values are deeply equal; otherwise, false.</returns>
        protected virtual bool OnNetworkVariableDeepEquals<T>(T oldValue, T newValue, string name, byte id)
        {
            if (oldValue == null || newValue == null)
            {
                NetworkLogger.__Log__(
                    $"The network variable '{name}' contains a null value. " +
                    "Ensure the value is properly initialized before performing any operation.",
                    NetworkLogger.LogType.Error
                );

                return true;
            }

            if (networkVariables.TryGetValue(id, out NetworkVariableField field))
            {
                if (!field.CheckEquality)
                {
                    return false;
                }

                var type = typeof(T);
                if (type.IsValueType)
                {
#if OMNI_DEBUG
                    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    {
                        NetworkLogger.__Log__(
                            $"Warning: The struct '{type.Name}' contains reference-type fields. It is recommended to keep structs simple and free of references to ensure optimal performance.",
                            NetworkLogger.LogType.Warning
                        );
                    }
#endif
                    return oldValue.Equals(newValue);
                }
                else if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    // Slower than the other checks, but it is the only way to compare complex types(List<T>, Dictionary<T>()).
                    // It may be more recommended to avoid using complex types or just disable equality checking for these types.
                    string oldJson = NetworkManager.ToJson(oldValue);
                    string newJson = NetworkManager.ToJson(newValue);

                    JToken oldToken = JToken.Parse(oldJson);
                    JToken newToken = JToken.Parse(newJson);
                    return JToken.DeepEquals(oldToken, newToken);
                }
                else
                {
                    // You must implement equals and GetHashCode()
                    return oldValue.Equals(newValue);
                }
            }

            return false;
        }
    }
}