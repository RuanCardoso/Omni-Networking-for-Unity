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

        /// <summary>
        /// The `Awake` method is virtual, allowing it to be overridden in derived classes
        /// for additional startup logic. If overridden, it is essential to call the base class's
        /// `Awake` method to ensure proper initialization. Not doing so may result in incomplete
        /// initialization and unpredictable behavior.
        /// </summary>
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

        /// <summary>
        /// The `Start` method is virtual, allowing it to be overridden in derived classes
        /// for additional startup logic. If overridden, it is essential to call the base class's
        /// `Start` method to ensure proper initialization. Not doing so may result in incomplete
        /// initialization and unpredictable behavior.
        /// </summary>
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

        protected virtual void OnBeforeSceneLoad(Scene scene, SceneOperationMode op)
        {
            if (m_UnregisterOnLoad)
            {
                Unregister();
            }
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
            if (string.IsNullOrEmpty(m_ServiceName))
            {
                m_ServiceName = GetType().Name;
                NetworkHelper.EditorSaveObject(gameObject);
            }
        }
    }
}
