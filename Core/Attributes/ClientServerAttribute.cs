using System;

namespace Omni.Core
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class EventAttribute : Attribute
    {
        internal byte Id { get; }

        public EventAttribute(byte id)
        {
            Id = id;
            if (Id is > 230 and < NetworkConstants.NETWORK_VARIABLE_RPC_ID)
            {
                throw new Exception(
                    $"Rpc Id: ({Id}) must be less than 230. IDs above 230 are reserved for internal use, such as RPC or custom messages. Please avoid using ID's above this threshold."
                );
            }
        }
    }

    /// <summary>
    /// Marks a method as a client-side Remote Procedure Call (RPC) event.
    /// </summary>
    /// <remarks>
    /// Use this attribute to indicate that a method is intended to handle RPCs sent to the client.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ClientAttribute : EventAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientAttribute"/> class with the specified rpc ID.
        /// </summary>
        /// <param name="id">The unique identifier for the client-side RPC event.</param>
        public ClientAttribute(byte id)
            : base(id)
        {
        }
    }

    /// <summary>
    /// Marks a method as a server-side Remote Procedure Call (RPC) event.
    /// </summary>
    /// <remarks>
    /// Use this attribute to indicate that a method is intended to handle RPCs sent to the server.
    /// By default, ownership verification is required for the RPC to be accepted.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ServerAttribute : EventAttribute
    {
        /// <summary>
        /// Gets or sets a value indicating whether ownership is required to invoke the server RPC.
        /// </summary>
        /// <value>
        /// Default is <c>true</c>, meaning only the client with ownership can call this RPC.
        /// </value>
        public bool RequiresOwnership { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerAttribute"/> class with the specified rpc ID.
        /// </summary>
        /// <param name="id">The unique identifier for the server-side RPC event.</param>
        public ServerAttribute(byte id)
            : base(id)
        {
        }
    }
}