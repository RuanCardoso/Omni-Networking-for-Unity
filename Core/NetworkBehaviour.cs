using System;
using Omni.Core.Interfaces;
using Omni.Shared;
using UnityEngine;

namespace Omni.Core
{
    public class NetworkBehaviour : MonoBehaviour, INetworkMessage
    {
        // Hacky: DIRTY CODE!
        // This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
        // Despite its appearance, this approach is essential to achieve high performance.
        // Avoid refactoring as these techniques are crucial for optimizing execution speed.
        // Works with il2cpp.

        public class NbClient
        {
            private readonly NetworkBehaviour m_NetworkBehaviour;

            internal NbClient(NetworkBehaviour networkBehaviour)
            {
                m_NetworkBehaviour = networkBehaviour;
            }

            public void Invoke(
                byte msgId,
                NetworkBuffer buffer = null,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                byte sequenceChannel = 0
            ) =>
                NetworkManager.Client.Invoke(
                    msgId,
                    m_NetworkBehaviour.IdentityId,
                    m_NetworkBehaviour.Id,
                    buffer,
                    deliveryMode,
                    sequenceChannel
                );
        }

        public class NbServer
        {
            private readonly NetworkBehaviour m_NetworkBehaviour;

            internal NbServer(NetworkBehaviour networkBehaviour)
            {
                m_NetworkBehaviour = networkBehaviour;
            }

            public void Invoke(
                byte msgId,
                NetworkBuffer buffer = null,
                Target target = Target.All,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                int groupId = 0,
                byte sequenceChannel = 0
            )
            {
                if (m_NetworkBehaviour.Identity.Owner == null)
                {
                    NetworkLogger.__Log__(
                        "Invoke: This property(Remote) is intended for server-side use only. It appears to be accessed from the client side."
                    );

                    return;
                }

                NetworkManager.Server.Invoke(
                    msgId,
                    m_NetworkBehaviour.Identity.Owner.Id,
                    m_NetworkBehaviour.IdentityId,
                    m_NetworkBehaviour.Id,
                    buffer,
                    target,
                    deliveryMode,
                    groupId,
                    sequenceChannel
                );
            }
        }

        // Hacky: DIRTY CODE!
        // This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
        // Despite its appearance, this approach is essential to achieve high performance.
        // Avoid refactoring as these techniques are crucial for optimizing execution speed.
        // Works with il2cpp.

        private readonly EventBehaviour<NetworkBuffer, int, Null, Null, Null> clientEventBehaviour =
            new();

        private readonly EventBehaviour<
            NetworkBuffer,
            NetworkPeer,
            int,
            Null,
            Null
        > serverEventBehaviour = new();

        [SerializeField]
        private byte m_Id = 0;

        public byte Id
        {
            get { return m_Id; }
            internal set { m_Id = value; }
        }

        public NetworkIdentity Identity { get; internal set; }
        public int IdentityId => Identity.IdentityId;

        private NbClient _local;

        /// <summary>
        /// Gets the <see cref="NbClient"/> instance used to invoke messages on the server from the client.
        /// </summary>
        public NbClient Local
        {
            get
            {
                if (_local == null)
                {
                    throw new Exception(
                        "This property(Local) is intended for client-side use only. It appears to be accessed from the server side."
                    );
                }

                return _local;
            }
            private set => _local = value;
        }

        private NbServer _remote;

        /// <summary>
        /// Gets the <see cref="NbServer"/> instance used to invoke messages on the client from the server.
        /// </summary>
        public NbServer Remote
        {
            get
            {
                if (_remote == null)
                {
                    throw new Exception(
                        "This property(Remote) is intended for server-side use only. It appears to be accessed from the client side."
                    );
                }

                return _remote;
            }
            private set => _remote = value;
        }

        internal void Register()
        {
            if (Identity.IsServer)
            {
                serverEventBehaviour.FindEvents<ServerAttribute>(this);
                Remote = new NbServer(this);
            }
            else
            {
                clientEventBehaviour.FindEvents<ClientAttribute>(this);
                Local = new NbClient(this);
            }

            AddEventBehaviour();
        }

        private void AddEventBehaviour()
        {
            var PeerEventBehaviours = Identity.IsServer
                ? NetworkManager.Server.PeerEventBehaviours
                : NetworkManager.Client.PeerEventBehaviours;

            var key = (IdentityId, m_Id);
            PeerEventBehaviours.Add(key, this);
        }

        private void TryClientLocate(byte msgId, NetworkBuffer buffer, int seqChannel)
        {
            if (clientEventBehaviour.TryGetLocate(msgId, out int argsCount))
            {
                switch (argsCount)
                {
                    case 0:
                        clientEventBehaviour.Invoke(msgId);
                        break;
                    case 1:
                        clientEventBehaviour.Invoke(msgId, buffer);
                        break;
                    case 2:
                        clientEventBehaviour.Invoke(msgId, buffer, seqChannel);
                        break;
                    case 3:
                        clientEventBehaviour.Invoke(msgId, buffer, seqChannel, default);
                        break;
                    case 4:
                        clientEventBehaviour.Invoke(msgId, buffer, seqChannel, default, default);
                        break;
                    case 5:
                        clientEventBehaviour.Invoke(
                            msgId,
                            buffer,
                            seqChannel,
                            default,
                            default,
                            default
                        );
                        break;
                }
            }
        }

        private void TryServerLocate(
            byte msgId,
            NetworkBuffer buffer,
            NetworkPeer peer,
            int seqChannel
        )
        {
            if (serverEventBehaviour.TryGetLocate(msgId, out int argsCount))
            {
                switch (argsCount)
                {
                    case 0:
                        serverEventBehaviour.Invoke(msgId);
                        break;
                    case 1:
                        serverEventBehaviour.Invoke(msgId, buffer);
                        break;
                    case 2:
                        serverEventBehaviour.Invoke(msgId, buffer, peer);
                        break;
                    case 3:
                        serverEventBehaviour.Invoke(msgId, buffer, peer, seqChannel);
                        break;
                    case 4:
                        serverEventBehaviour.Invoke(msgId, buffer, peer, seqChannel, default);
                        break;
                    case 5:
                        serverEventBehaviour.Invoke(
                            msgId,
                            buffer,
                            peer,
                            seqChannel,
                            default,
                            default
                        );
                        break;
                }
            }
        }

        public void Internal_OnMessage(
            byte msgId,
            NetworkBuffer buffer,
            NetworkPeer peer,
            bool _,
            int seqChannel
        )
        {
            if (Identity.IsServer)
            {
                TryServerLocate(msgId, buffer, peer, seqChannel);
            }
            else
            {
                TryClientLocate(msgId, buffer, seqChannel);
            }
        }

        protected virtual void OnValidate()
        {
            if (m_Id < 0)
            {
                m_Id = 0;
            }
            else if (m_Id > 255)
            {
                m_Id = 255;
            }
        }
    }
}
