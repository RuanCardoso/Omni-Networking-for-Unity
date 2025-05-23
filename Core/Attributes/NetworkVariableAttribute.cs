// Roslyn Generated //  Roslyn Analyzer

using System;
using UnityEngine;

namespace Omni.Core
{
    /// <summary>
    /// Specifies which parts of a network-synchronized variable should be hidden in the Unity Inspector.
    /// Supports bitwise combinations for flexible control.
    /// </summary>
    /// <remarks>
    /// Use this enum in the <see cref="NetworkVariableAttribute"/> to control visibility of backing fields,
    /// properties, or both in the Unity Inspector. This is useful for debugging or when you want to hide
    /// implementation details from the Unity Editor interface.
    /// </remarks>
    [Flags]
    public enum HideMode
    {
        /// <summary>
        /// No parts are hidden; both the backing field and property are visible in the Inspector.
        /// </summary>
        None = 0,
        /// <summary>
        /// Hides the backing field from the Unity Inspector.
        /// </summary>
        BackingField = 1,
        /// <summary>
        /// Hides the property from the Unity Inspector.
        /// </summary>
        Property = 2,
        /// <summary>
        /// Hides both the backing field and the property from the Unity Inspector.
        /// </summary>
        Both = 4
    }

    /// <summary>
    /// An attribute used to mark a property or field for automatic network synchronization.
    /// This attribute supports features such as ownership control, equality checks, and 
    /// optional manual identifier assignment.
    /// </summary>
    /// <remarks>
    /// When applied to a property or field, this attribute allows it to synchronize its value
    /// across the network using a Memory Pack serializer. By default, ownership verification
    /// and equality checks are enabled.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
    public class NetworkVariableAttribute : PropertyAttribute
    {
        internal byte Id { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the property requires client authority 
        /// for synchronization.
        /// </summary>
        /// <value>
        /// Default is <c>false</c>, meaning client authority is not required.
        /// </value>
        public bool IsClientAuthority { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the server should automatically broadcast 
        /// updates received from a client to all other connected clients.
        /// </summary>
        /// <value>
        /// <para>
        /// <c>true</c> if the server will automatically forward updates received from a client to 
        /// all other clients (i.e., client → server → clients); 
        /// <c>false</c> if updates from clients are only processed by the server and 
        /// not automatically relayed to other clients (i.e., client → server only).
        /// </para>
        /// <para>
        /// The default value is <c>true</c>.
        /// </para>
        /// </value>
        /// <remarks>
        /// When enabled, this option simplifies state synchronization for most multiplayer games, 
        /// allowing clients to see updates from other players in real time without requiring manual 
        /// forwarding logic. If set to <c>false</c>, the developer is responsible for handling 
        /// how and when updates from a client should be propagated to other clients.
        /// </remarks>
        public bool ServerBroadcastsClientUpdates { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether ownership is required for property synchronization.
        /// </summary>
        /// <value>
        /// Default is <c>true</c>, meaning only the owner can modify the synchronized variable.
        /// </value>
        public bool RequiresOwnership { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether equality checks should be performed before synchronizing the value.
        /// </summary>
        /// <value>
        /// Default is <c>true</c>, meaning the value is synchronized only if it has changed.
        /// </value>
        public bool CheckEquality { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the field should be hidden in the Unity Inspector.
        /// </summary>
        /// <value>
        /// Default is <c>HideMode.BackingField</c>, meaning the field will not be visible in the Unity Inspector.
        /// </value>
        public HideMode HideMode { get; set; } = HideMode.BackingField;

        /// <summary>
        /// Gets or sets the delivery mode for network variable synchronization.
        /// </summary>
        /// <value>
        /// Default is <c>DeliveryMode.ReliableOrdered</c>, ensuring reliable and ordered delivery of updates.
        /// </value>
        public DeliveryMode DeliveryMode { get; set; } = DeliveryMode.ReliableOrdered;

        /// <summary>
        /// Gets or sets the target recipients for network variable updates.
        /// </summary>
        /// <value>
        /// Default is <c>Target.Auto</c>, which automatically determines the appropriate recipients based on the context.
        /// </value>
        public Target Target { get; set; } = Target.Auto;

        /// <summary>
        /// Gets or sets the sequence channel used for ordered message delivery.
        /// </summary>
        /// <value>
        /// Default is <c>0</c>. Different channels allow for independent ordering.
        /// </value>
        public byte SequenceChannel { get; set; } = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkVariableAttribute"/> class 
        /// with an automatically generated identifier.
        /// </summary>
        /// <remarks>
        /// The ID for the property or field will default to 0, and the source generator 
        /// will assign a unique value during compilation.
        /// </remarks>
        public NetworkVariableAttribute()
        {
            Id = 0; // 0 - Is auto generated by the source generator.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkVariableAttribute"/> class 
        /// with a specified identifier.
        /// </summary>
        /// <param name="id">
        /// The unique identifier to assign to the property or field for synchronization.
        /// </param>
        /// <remarks>
        /// Use this constructor when a manual ID assignment is required instead of relying on the source generator.
        /// </remarks>
        public NetworkVariableAttribute(byte id)
        {
            Id = id;
        }
    }
}