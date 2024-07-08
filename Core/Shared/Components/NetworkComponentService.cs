using System;
using Omni.Core.Interfaces;
using UnityEngine;

namespace Omni.Core.Components
{
    [DefaultExecutionOrder(-11500)] // Component Service should be initialized before the all other components because has priority.
    public class NetworkComponentService : ServiceBehaviour, INetworkComponentService
    {
        [SerializeField]
        private MonoBehaviour m_Component;

        public GameObject GameObject => gameObject;
        public MonoBehaviour Component => m_Component;

        protected override void OnAwake()
        {
            if (m_Component == null)
            {
                throw new NullReferenceException(
                    "Component is null. Did you forget to set it in the inspector?"
                );
            }
        }

        protected override void OnValidate()
        {
            ServiceName = GameObject.name;
        }
    }
}
