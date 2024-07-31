using System;
using UnityEngine;

#pragma warning disable

namespace Omni.Core
{
    public class NetworkEventBase : NetworkVariablesBehaviour
    {
        [Header("Service Settings")]
        [SerializeField]
        internal string m_ServiceName;

        [SerializeField]
        internal int m_Id;

        [SerializeField]
        internal BindingFlags m_BindingFlags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private NbClient local;
        private NbServer remote;
        internal bool m_UnregisterOnLoad = true;

        // public api: allow send from other object
        /// <summary>
        /// Gets the <see cref="NbClient"/> instance used to invoke messages on the server from the client.
        /// </summary>
        public NbClient Local
        {
            get
            {
                if (local == null)
                {
                    throw new NullReferenceException(
                        "This property(Local) is intended for client-side use only. It appears to be accessed from the server side. Or Call Awake() and Start() base first or initialize manually."
                    );
                }

                return local;
            }
            internal set => local = value;
        }

        // public api: allow send from other object
        /// <summary>
        /// Gets the <see cref="NbServer"/> instance used to invoke messages on the client from the server.
        /// </summary>
        public NbServer Remote
        {
            get
            {
                if (remote == null)
                {
                    throw new NullReferenceException(
                        "This property(Remote) is intended for server-side use only. It appears to be accessed from the client side. Or Call Awake() and Start() base first or initialize manually."
                    );
                }

                return remote;
            }
            internal set => remote = value;
        }

        /// <summary>
        /// Called when the service is initialized.
        /// </summary>
        protected virtual void OnAwake() { }

        /// <summary>
        /// Called when the service is initialized.
        /// </summary>
        protected virtual void OnStart() { }

        /// <summary>
        /// Called when the service is stopped/destroyed/unregistered.
        /// </summary>
        protected virtual void OnStop() { }

        protected virtual void Reset()
        {
            OnValidate();
        }

        protected virtual void OnValidate()
        {
            if (remote != null || local != null)
                ___NotifyChange___(); // Override by the source generator.

            if (m_Id == 0)
            {
                m_Id = NetworkHelper.GenerateSceneUniqueId();
                NetworkHelper.EditorSaveObject(gameObject);
            }

            if (string.IsNullOrEmpty(m_ServiceName))
            {
                m_ServiceName = GetType().Name;
                NetworkHelper.EditorSaveObject(gameObject);
            }

#if OMNI_DEBUG
            if (GetComponentInChildren<NetworkIdentity>() != null)
            {
                throw new NotSupportedException(
                    "NetworkEventBase should not be attached to an object with a NetworkIdentity. Use 'NetworkBehaviour' instead."
                );
            }
#endif
        }
    }
}
