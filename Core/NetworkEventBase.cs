using System;
using System.ComponentModel;
using Omni.Inspector;
using UnityEngine;

#pragma warning disable

namespace Omni.Core
{
    [DeclareFoldoutGroup("Network Variables", Expanded = true, Title = "Network Variables - (Auto Synced)")]
    [DeclareFoldoutGroup("Service Settings")]
    [StackTrace]
    public class NetworkEventBase : NetworkVariablesBehaviour
    {
        [GroupNext("Service Settings")]
        [SerializeField]
        internal string m_ServiceName;

        [SerializeField] internal int m_Id;
        internal bool allowUnregisterService = true;

        /// <summary>
        /// Called when the service is initialized.
        /// </summary>
        protected virtual void OnAwake()
        {
        }

        /// <summary>
        /// Called when the service is initialized.
        /// </summary>
        protected virtual void OnStart()
        {
        }

        /// <summary>
        /// Called when the service is stopped/destroyed/unregistered.
        /// </summary>
        protected virtual void OnStop()
        {
        }

        /// <summary>
        /// Rents a data buffer from the network manager's pool. The caller must ensure the buffer is disposed or used within a using statement.
        /// </summary>
        /// <returns>A rented data buffer.</returns>
        protected DataBuffer Rent(bool enableTracking = true)
        {
            return NetworkManager.Pool.Rent(enableTracking);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't override this method! The source generator will override it.")]
        protected internal virtual void ___InjectServices___()
        {
        }

        protected virtual void Reset()
        {
            OnValidate();
        }

        protected virtual void OnValidate()
        {
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
                    $"The component {GetType().Name} cannot be attached to an object that also has a 'NetworkIdentity' component. Please use 'NetworkBehaviour' in such cases."
                );
            }
#endif
        }
    }
}