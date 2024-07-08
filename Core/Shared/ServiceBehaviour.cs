using System.ComponentModel;
using Omni.Core.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Omni.Core
{
    [DefaultExecutionOrder(-10500)]
    public class ServiceBehaviour : MonoBehaviour, IServiceBehaviour
    {
        [Header("Service Settings")]
        [SerializeField]
        private string m_ServiceName;

        private bool m_UnregisterOnLoad = true;
        public string ServiceName
        {
            get => m_ServiceName;
            set => m_ServiceName = value;
        }

        public virtual void Awake()
        {
            InitAwake();
        }

        private void InitAwake()
        {
            if (NetworkService.Exists(m_ServiceName))
            {
                m_UnregisterOnLoad = false;
                return;
            }

            if (m_UnregisterOnLoad)
            {
                NetworkManager.OnBeforeSceneLoad += OnBeforeSceneLoad;
                InitializeServiceLocator();
                OnAwake();
            }
        }

        public virtual void Start()
        {
            InitStart();
        }

        private void InitStart()
        {
            if (m_UnregisterOnLoad)
            {
                OnStart();
                Service.UpdateReference(m_ServiceName);
            }

            m_UnregisterOnLoad = !NetworkHelper.IsDontDestroyOnLoad(gameObject);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Internal_Awake()
        {
            InitAwake();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Internal_Start()
        {
            InitStart();
        }

        protected void InitializeServiceLocator()
        {
            if (!NetworkService.TryRegister(this, m_ServiceName))
            {
                // Update the old reference to the new one.
                NetworkService.Update(this, m_ServiceName);
            }
        }

        protected void Unregister()
        {
            NetworkManager.OnBeforeSceneLoad -= OnBeforeSceneLoad;
            NetworkService.Unregister(m_ServiceName);
            OnStop();
        }

        protected virtual void OnBeforeSceneLoad(Scene scene)
        {
            if (m_UnregisterOnLoad)
            {
                Unregister();
            }
        }

        protected virtual void OnAwake() { }

        protected virtual void OnStart() { }

        protected virtual void OnStop() { }

        protected virtual void Reset()
        {
            OnValidate();
        }

        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(m_ServiceName))
            {
                m_ServiceName = GetType().Name;
            }
        }
    }
}
