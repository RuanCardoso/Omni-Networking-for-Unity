using Omni.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;

#pragma warning disable

namespace Omni.Core
{
    // Hacky: DIRTY CODE!
    // This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
    // Despite its appearance, this approach is essential to achieve high performance.
    // Avoid refactoring as these techniques are crucial for optimizing execution speed.
    // Works with il2cpp.

    internal class NetworkVariableField
    {
        internal string Name { get; }
        internal int PropertyId { get; }
        internal bool RequiresOwnership { get; }
        internal bool IsClientAuthority { get; }
        internal bool CheckEquality { get; }
        internal DeliveryMode DeliveryMode { get; }
        internal Target Target { get; }
        internal byte SequenceChannel { get; }

        internal NetworkVariableField(int propertyId, bool requiresOwnership, bool isClientAuthority, bool checkEquality, string name, DeliveryMode deliveryMode, Target target, byte sequenceChannel)
        {
            PropertyId = propertyId;
            RequiresOwnership = requiresOwnership;
            IsClientAuthority = isClientAuthority;
            CheckEquality = checkEquality;
            Name = name;
            DeliveryMode = deliveryMode;
            Target = target;
            SequenceChannel = sequenceChannel;
        }
    }

    public class NetworkVariablesBehaviour : OmniBehaviour
    {
        internal readonly Dictionary<byte, NetworkVariableField> m_NetworkVariables = new();

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't override this method! The source generator will override it.")]
        protected void ___RegisterNetworkVariable___(string propertyName, byte propertyId, bool requiresOwnership,
            bool isClientAuthority, bool checkEquality, DeliveryMode deliveryMode, Target target, byte sequenceChannel)
        {
            if (!m_NetworkVariables.TryAdd(propertyId, new NetworkVariableField(propertyId, requiresOwnership,
                isClientAuthority, checkEquality, propertyName, deliveryMode, target, sequenceChannel)))
            {
                NetworkLogger.__Log__(
                     $"Error: Network variable '{propertyName}' (ID: {propertyId}) is already registered. " +
                     "Ensure that the ID is unique and not reused for multiple variables.",
                     NetworkLogger.LogType.Error
                );
            }
        }

        internal DataBuffer CreateNetworkVariableMessage<T>(T @object, byte id)
        {
            if (@object is DataBuffer buffer)
            {
                return CreateNetworkVariableMessage(buffer, id);
            }

            DataBuffer message = NetworkManager.Pool.Rent(enableTracking: false); // disposed by the caller(not user)
            message.Write(id);

            // If the object implements ISerializable, serialize it.
            if (@object is IMessage data)
            {
                using var serializedData = data.Serialize();
                message.Internal_CopyFrom(serializedData);
                return message;
            }

            message.WriteAsBinary(@object);
            return message;
        }

        private DataBuffer CreateNetworkVariableMessage(DataBuffer buffer, byte id) // Used for Action, Func, delegates.
        {
            DataBuffer message = NetworkManager.Pool.Rent(enableTracking: false); // disposed by the caller(not user)
            message.Write(id);
            message.Internal_CopyFrom(buffer);
            return message;
        }

        // This method is intended to be overridden by the caller using source generators and reflection techniques. Magic wow!
        // https://github.com/RuanCardoso/OmniNetSourceGenerator
        // never override this method!
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't override this method! The source generator will override it.")]
        protected virtual void ___OnPropertyChanged___(string propertyName, byte propertyId, NetworkPeer peer,
            DataBuffer buffer)
        {
            // Overridden by the source generator!
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
        protected virtual void ___RegisterNetworkVariables___()
        {
            // Overridden by the source generator!
        }

        // This method is intended to be overridden by the caller using source generators and reflection techniques. Magic wow!
        // https://github.com/RuanCardoso/OmniNetSourceGenerator
        // never override this method!
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't override this method! The source generator will override it.")]
        protected virtual void ___NotifyCollectionChange___()
        {
            // Overridden by the source generator!
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
            // Overridden by the source generator!
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
                    $"Error: Network variable '{name}' (ID: {id}) contains a null value. " +
                    "Network variables must be initialized before comparison. " +
                    "Initialize this variable in OnStart() or OnAwake() to prevent this error.",
                    NetworkLogger.LogType.Error
                );

                return true;
            }

            if (m_NetworkVariables.TryGetValue(id, out NetworkVariableField field))
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