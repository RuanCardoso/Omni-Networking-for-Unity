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
        internal string Name { get; set; }
        internal int PropertyId { get; set; }
        internal bool RequiresOwnership { get; set; }
        internal bool IsClientAuthority { get; set; }
        internal bool CheckEquality { get; set; }
        internal DeliveryMode DeliveryMode { get; set; }
        internal Target Target { get; set; }
        internal byte SequenceChannel { get; set; }

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
        /// <summary>
        /// When true, NetworkVariables will use MemoryPack serialization for equality checks 
        /// instead of JSON. This provides faster and allocation-friendly comparisons, but requires 
        /// the type to be supported by MemoryPack. If false, JSON-based DeepEquals will be used.
        /// </summary>
        protected virtual bool UseMemoryPackEquality { get; set; } = false;

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
                    $"[NetworkVariable Error] Duplicate registration detected for property '{propertyName}' (Id={propertyId}). " +
                    "Each NetworkVariable must have a unique Id within the same NetworkBehaviour. " +
                    "Tip: Check if two variables share the same [NetworkVariable] attribute Id or if a source generator conflict occurred.",
                    NetworkLogger.LogType.Error
                );
            }
        }

        /// <summary>
        /// Overrides the parameters of all registered <see cref="NetworkVariableField"/> 
        /// in <see cref="m_NetworkVariables"/> with the specified values.
        /// </summary>
        protected void OverrideNetworkVariableParameters(
            Target target = Target.Auto,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            bool requiresOwnership = true,
            bool isClientAuthority = false,
            bool checkEquality = true,
            byte sequenceChannel = 0)
        {
            foreach (var kvp in m_NetworkVariables)
            {
                var field = kvp.Value;

                field.Target = target;
                field.DeliveryMode = deliveryMode;
                field.RequiresOwnership = requiresOwnership;
                field.IsClientAuthority = isClientAuthority;
                field.CheckEquality = checkEquality;
                field.SequenceChannel = sequenceChannel;
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
                    $"[NetworkVariable Error] The variable '{name}' (Id={id}) is null. " +
                    "This usually means it was not initialized before use. " +
                    "Make sure to assign it in OnAwake() or OnStart() before accessing it.",
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
                if (!type.IsValueType)
                {
                    if (ReferenceEquals(oldValue, newValue))
                    {
                        NetworkLogger.__Log__(
                            $"[NetworkVariable Notice] '{field.Name}' (Id={id}) was not synchronized because the new value is the same reference as the old value. " +
                            "No network update was sent since nothing actually changed.",
                            NetworkLogger.LogType.Warning
                        );

                        return true;
                    }
                }

                if (type.IsValueType)
                {
#if OMNI_DEBUG
                    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    {
                        NetworkLogger.__Log__(
                            $"The struct '{type.Name}' contains reference-type fields. It is recommended to keep structs simple and free of references to ensure optimal performance.",
                            NetworkLogger.LogType.Warning
                        );
                    }
#endif
                    return oldValue.Equals(newValue);
                }
                else return UseMemoryPackEquality
                    ? NetworkHelper.DeepEqualsMemoryPack(oldValue, newValue)
                    : NetworkHelper.DeepEqualsJson(oldValue, newValue);
            }

            return false;
        }
    }
}