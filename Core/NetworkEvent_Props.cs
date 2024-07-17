using System.Runtime.CompilerServices;
using Omni.Core.Interfaces;
using static Omni.Core.NetworkManager;

namespace Omni.Core
{
    // Hacky: DIRTY CODE!
    // This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
    // Despite its appearance, this approach is essential to achieve high performance.
    // Avoid refactoring as these techniques are crucial for optimizing execution speed.
    // Works with il2cpp.

    public class NbClient
    {
        private readonly IInvokeMessage m_NetworkMessage;
        private readonly NetworkVariablesBehaviour m_NetworkVariablesBehaviour;

        internal NbClient(IInvokeMessage networkMessage)
        {
            m_NetworkMessage = networkMessage;
            m_NetworkVariablesBehaviour = m_NetworkMessage as NetworkVariablesBehaviour;
        }

        /// <summary>
        /// Sends a manual 'NetVar' message to the server with the specified property and property ID.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        /// <param name="property">The property value to synchronize.</param>
        /// <param name="propertyId">The ID of the property being synchronized.</param>
        public void ManualSync<T>(T property, byte propertyId, SyncOptions syncOptions)
        {
            ManualSync<T>(
                property,
                propertyId,
                syncOptions.DeliveryMode,
                syncOptions.SequenceChannel
            );
        }

        /// <summary>
        /// Sends a manual 'NetVar' message to the server with the specified property and property ID.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        /// <param name="property">The property value to synchronize.</param>
        /// <param name="propertyId">The ID of the property being synchronized.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void ManualSync<T>(
            T property,
            byte propertyId,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            byte sequenceChannel = 0
        )
        {
            using DataBuffer message = m_NetworkVariablesBehaviour.CreateHeader(
                property,
                propertyId
            );

            Invoke(255, message, deliveryMode, sequenceChannel);
        }

        /// <summary>
        /// Automatically sends a 'NetVar' message to the server based on the caller member name.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        public void AutoSync<T>(SyncOptions options, [CallerMemberName] string ___ = "")
        {
            AutoSync<T>(options.DeliveryMode, options.SequenceChannel, ___);
        }

        /// <summary>
        /// Automatically sends a 'NetVar' message to the server based on the caller member name.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void AutoSync<T>(
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            byte sequenceChannel = 0,
            [CallerMemberName] string ___ = ""
        )
        {
            IPropertyInfo property = m_NetworkVariablesBehaviour.GetPropertyInfoWithCallerName<T>(
                ___
            );
            IPropertyInfo<T> propertyGeneric = property as IPropertyInfo<T>;

            if (property != null)
            {
                using DataBuffer message = m_NetworkVariablesBehaviour.CreateHeader(
                    propertyGeneric.Invoke(),
                    property.Id
                );

                Invoke(255, message, deliveryMode, sequenceChannel);
            }
        }

        /// <summary>
        /// Invokes a global message on the server, similar to a Remote Procedure Call (RPC).
        /// </summary>
        /// <param name="msgId">The ID of the message to be invoked.</param>
        public void GlobalInvoke(byte msgId, SyncOptions options)
        {
            GlobalInvoke(msgId, options.Buffer, options.DeliveryMode, options.SequenceChannel);
        }

        /// <summary>
        /// Invokes a global message on the server, similar to a Remote Procedure Call (RPC).
        /// </summary>
        /// <param name="msgId">The ID of the message to be invoked.</param>
        /// <param name="buffer">The buffer containing the message data. Default is null.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void GlobalInvoke(
            byte msgId,
            DataBuffer buffer = null,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            byte sequenceChannel = 0
        )
        {
            Client.GlobalInvoke(msgId, buffer, deliveryMode, sequenceChannel);
        }

        /// <summary>
        /// Invokes a message on the server, similar to a Remote Procedure Call (RPC).
        /// </summary>
        /// <param name="msgId">The ID of the message to be invoked.</param>
        public void Invoke(byte msgId, SyncOptions options)
        {
            Invoke(msgId, options.Buffer, options.DeliveryMode, options.SequenceChannel);
        }

        /// <summary>
        /// Invokes a message on the server, similar to a Remote Procedure Call (RPC).
        /// </summary>
        /// <param name="msgId">The ID of the message to be invoked.</param>
        /// <param name="buffer">The buffer containing the message data. Default is null.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void Invoke(
            byte msgId,
            DataBuffer buffer = null,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            byte sequenceChannel = 0
        )
        {
            Client.Invoke(
                msgId,
                m_NetworkMessage.IdentityId,
                buffer,
                deliveryMode,
                sequenceChannel
            );
        }
    }

    public class NbServer
    {
        private readonly IInvokeMessage m_NetworkMessage;
        private readonly NetworkVariablesBehaviour m_NetworkVariablesBehaviour;

        internal NbServer(IInvokeMessage networkMessage)
        {
            m_NetworkMessage = networkMessage;
            m_NetworkVariablesBehaviour = m_NetworkMessage as NetworkVariablesBehaviour;
        }

        /// <summary>
        /// Sends a manual 'NetVar' message to all(default) clients with the specified property and property ID.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        /// <param name="property">The property value to synchronize.</param>
        /// <param name="propertyId">The ID of the property being synchronized.</param>
        /// <param name="target">The target for the message. Default is <see cref="Target.All"/>.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="groupId">The group ID for the message. Default is 0.</param>
        /// <param name="cacheId">The cache ID for the message. Default is 0.</param>
        /// <param name="cacheMode">The cache mode for the message. Default is <see cref="CacheMode.None"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void ManualSync<T>(
            T property,
            byte propertyId,
            SyncOptions options,
            NetworkPeer peer = null
        )
        {
            ManualSync<T>(
                property,
                propertyId,
                peer,
                options.Target,
                options.DeliveryMode,
                options.GroupId,
                options.CacheId,
                options.CacheMode,
                options.SequenceChannel
            );
        }

        /// <summary>
        /// Sends a manual 'NetVar' message to all(default) clients with the specified property and property ID.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        /// <param name="property">The property value to synchronize.</param>
        /// <param name="propertyId">The ID of the property being synchronized.</param>
        /// <param name="target">The target for the message. Default is <see cref="Target.All"/>.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="groupId">The group ID for the message. Default is 0.</param>
        /// <param name="cacheId">The cache ID for the message. Default is 0.</param>
        /// <param name="cacheMode">The cache mode for the message. Default is <see cref="CacheMode.None"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void ManualSync<T>(
            T property,
            byte propertyId,
            NetworkPeer peer = null,
            Target target = Target.All,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            int groupId = 0,
            int cacheId = 0,
            CacheMode cacheMode = CacheMode.None,
            byte sequenceChannel = 0
        )
        {
            peer ??= Server.ServerPeer;
            using DataBuffer message = m_NetworkVariablesBehaviour.CreateHeader(
                property,
                propertyId
            );
            Invoke(
                255,
                peer,
                message,
                target,
                deliveryMode,
                groupId,
                cacheId,
                cacheMode,
                sequenceChannel
            );
        }

        /// <summary>
        /// Automatically sends a 'NetVar' message to all(default) clients based on the caller member name.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        /// <param name="target">The target for the message. Default is <see cref="Target.All"/>.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="groupId">The group ID for the message. Default is 0.</param>
        /// <param name="cacheId">The cache ID for the message. Default is 0.</param>
        /// <param name="cacheMode">The cache mode for the message. Default is <see cref="CacheMode.None"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        /// <param name="___">The name of the calling member. This parameter is automatically supplied by the compiler
        public void AutoSync<T>(
            SyncOptions options,
            NetworkPeer peer = null,
            [CallerMemberName] string ___ = ""
        )
        {
            AutoSync<T>(
                peer,
                options.Target,
                options.DeliveryMode,
                options.GroupId,
                options.CacheId,
                options.CacheMode,
                options.SequenceChannel,
                ___
            );
        }

        /// <summary>
        /// Automatically sends a 'NetVar' message to all(default) clients based on the caller member name.
        /// </summary>
        /// <typeparam name="T">The type of the property to synchronize.</typeparam>
        /// <param name="target">The target for the message. Default is <see cref="Target.All"/>.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="groupId">The group ID for the message. Default is 0.</param>
        /// <param name="cacheId">The cache ID for the message. Default is 0.</param>
        /// <param name="cacheMode">The cache mode for the message. Default is <see cref="CacheMode.None"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        /// <param name="___">The name of the calling member. This parameter is automatically supplied by the compiler
        public void AutoSync<T>(
            NetworkPeer peer = null,
            Target target = Target.All,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            int groupId = 0,
            int cacheId = 0,
            CacheMode cacheMode = CacheMode.None,
            byte sequenceChannel = 0,
            [CallerMemberName] string ___ = ""
        )
        {
            IPropertyInfo property = m_NetworkVariablesBehaviour.GetPropertyInfoWithCallerName<T>(
                ___
            );
            IPropertyInfo<T> propertyGeneric = property as IPropertyInfo<T>;

            if (property != null)
            {
                peer ??= Server.ServerPeer;
                using DataBuffer message = m_NetworkVariablesBehaviour.CreateHeader(
                    propertyGeneric.Invoke(),
                    property.Id
                );

                Invoke(
                    255,
                    peer,
                    message,
                    target,
                    deliveryMode,
                    groupId,
                    cacheId,
                    cacheMode,
                    sequenceChannel
                );
            }
        }

        /// <summary>
        /// Invokes a global message on the client, similar to a Remote Procedure Call (RPC).
        /// </summary>
        /// <param name="msgId">The ID of the message to be invoked.</param>
        /// <param name="buffer">The buffer containing the message data. Default is null.</param>
        /// <param name="target">The target(s) for the message. Default is <see cref="Target.All"/>.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="groupId">The group ID for the message. Default is 0.</param>
        /// <param name="cacheId">The cache ID for the message. Default is 0.</param>
        /// <param name="cacheMode">The cache mode for the message. Default is <see cref="CacheMode.None"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void GlobalInvoke(byte msgId, NetworkPeer peer, SyncOptions options)
        {
            GlobalInvoke(
                msgId,
                peer,
                options.Buffer,
                options.Target,
                options.DeliveryMode,
                options.GroupId,
                options.CacheId,
                options.CacheMode,
                options.SequenceChannel
            );
        }

        /// <summary>
        /// Invokes a global message on the client, similar to a Remote Procedure Call (RPC).
        /// </summary>
        /// <param name="msgId">The ID of the message to be invoked.</param>
        /// <param name="buffer">The buffer containing the message data. Default is null.</param>
        /// <param name="target">The target(s) for the message. Default is <see cref="Target.All"/>.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="groupId">The group ID for the message. Default is 0.</param>
        /// <param name="cacheId">The cache ID for the message. Default is 0.</param>
        /// <param name="cacheMode">The cache mode for the message. Default is <see cref="CacheMode.None"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void GlobalInvoke(
            byte msgId,
            NetworkPeer peer,
            DataBuffer buffer = null,
            Target target = Target.Self,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            int groupId = 0,
            int cacheId = 0,
            CacheMode cacheMode = CacheMode.None,
            byte sequenceChannel = 0
        )
        {
            Server.GlobalInvoke(
                msgId,
                peer,
                buffer,
                target,
                deliveryMode,
                groupId,
                cacheId,
                cacheMode,
                sequenceChannel
            );
        }

        /// <summary>
        /// Invokes a message on the client, similar to a Remote Procedure Call (RPC).
        /// </summary>
        /// <param name="msgId">The ID of the message to be invoked.</param>
        /// <param name="buffer">The buffer containing the message data. Default is null.</param>
        /// <param name="target">The target(s) for the message. Default is <see cref="Target.All"/>.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="groupId">The group ID for the message. Default is 0.</param>
        /// <param name="cacheId">The cache ID for the message. Default is 0.</param>
        /// <param name="cacheMode">The cache mode for the message. Default is <see cref="CacheMode.None"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void Invoke(byte msgId, NetworkPeer peer, SyncOptions options)
        {
            Invoke(
                msgId,
                peer,
                options.Buffer,
                options.Target,
                options.DeliveryMode,
                options.GroupId,
                options.CacheId,
                options.CacheMode,
                options.SequenceChannel
            );
        }

        /// <summary>
        /// Invokes a message on the client, similar to a Remote Procedure Call (RPC).
        /// </summary>
        /// <param name="msgId">The ID of the message to be invoked.</param>
        /// <param name="buffer">The buffer containing the message data. Default is null.</param>
        /// <param name="target">The target(s) for the message. Default is <see cref="Target.All"/>.</param>
        /// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
        /// <param name="groupId">The group ID for the message. Default is 0.</param>
        /// <param name="cacheId">The cache ID for the message. Default is 0.</param>
        /// <param name="cacheMode">The cache mode for the message. Default is <see cref="CacheMode.None"/>.</param>
        /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
        public void Invoke(
            byte msgId,
            NetworkPeer peer,
            DataBuffer buffer = null,
            Target target = Target.All,
            DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
            int groupId = 0,
            int cacheId = 0,
            CacheMode cacheMode = CacheMode.None,
            byte sequenceChannel = 0
        )
        {
            Server.Invoke(
                msgId,
                peer,
                m_NetworkMessage.IdentityId,
                buffer,
                target,
                deliveryMode,
                groupId,
                cacheId,
                cacheMode,
                sequenceChannel
            );
        }
    }
}

// Hacky: DIRTY CODE!
// This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
// Despite its appearance, this approach is essential to achieve high performance.
// Avoid refactoring as these techniques are crucial for optimizing execution speed.
// Works with il2cpp.
