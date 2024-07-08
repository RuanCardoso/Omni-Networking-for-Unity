using Omni.Core.Interfaces;
using UnityEngine;

namespace Omni.Core.Components
{
    [DefaultExecutionOrder(-11500)] // Component Service should be initialized before the all other components because has priority.
    public class ComponentService : MonoBehaviour
    {
        [SerializeField]
        private string m_ServiceName;

        [SerializeField]
        private MonoBehaviour m_Component;

        private void Awake()
        {
            Register();
        }

        private void Register()
        {
            if (string.IsNullOrEmpty(m_ServiceName))
            {
                throw new System.NullReferenceException("Component Service: service name is empty. A name is required for a service to be registered.");
            }

            if (m_Component == null)
            {
                throw new System.NullReferenceException($"Component Service: Requires a component({m_Component.GetType()}) on the same game object.");
            }

            if (m_Component is INetworkMessage || m_Component is NetworkManager)
            {
                throw new System.InvalidOperationException("Component Service: The component cannot be registered as a Network Service with this component.");
            }

            if (!NetworkService.TryRegister(m_Component, m_ServiceName))
            {
                NetworkService.Update(m_Component, m_ServiceName);
            }
        }
    }
}