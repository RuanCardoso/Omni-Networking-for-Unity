using Omni.Core.Attributes;
using UnityEngine;

namespace Omni.Core
{
    public class NetworkIdentity : MonoBehaviour
    {
        [SerializeField]
        [ReadOnly]
        private int m_Id;

        [SerializeField]
        [ReadOnly]
        private bool m_IsServer;

        [SerializeField]
        [ReadOnly]
        private bool m_IsLocalPlayer;

        public int IdentityId
        {
            get { return m_Id; }
            internal set { m_Id = value; }
        }

        /// <summary>
        /// Owner of this object. Only available on server, returns null on client.
        /// </summary>
        public NetworkPeer Owner { get; internal set; }

        /// <summary>
        /// Indicates whether this object is obtained from the server or checked on the client.
        /// True if the object is obtained from the server, false if it is checked on the client.
        /// </summary>
        public bool IsServer
        {
            get { return m_IsServer; }
            internal set { m_IsServer = value; }
        }

        /// <summary>
        /// Indicates whether this object is owned by the local player.
        /// </summary>
        public bool IsLocalPlayer
        {
            get { return m_IsLocalPlayer; }
            internal set { m_IsLocalPlayer = value; }
        }
    }
}
