using System;
using Omni.Core.Interfaces;
using UnityEngine;
using UnityEngine.UI;
#if OMNI_TEXTMESHPRO_ENABLED
using TMPro;
#endif

namespace Omni.Core.Components
{
    [DefaultExecutionOrder(-11500)] // Component Service should be initialized before the all other components because has priority.
    public class NetworkComponentService : ServiceBehaviour, INetworkComponentService
    {
        [SerializeField]
        private Component m_Component;

        public Component Component => m_Component;
        public GameObject GameObject => gameObject;

        protected override void OnAwake()
        {
            if (m_Component == null)
            {
                throw new NullReferenceException(
                    "Component is null. Did you forget to set it in the inspector?"
                );
            }

            if (transform.root.TryGetComponent(out NetworkIdentity identity))
            {
                Unregister(); // Unregister globally if it's already registered and register locally(Identity);
                if (!identity.TryRegister(this, ServiceName))
                {
                    // Update the old reference to the new one.
                    identity.UpdateService(this, ServiceName);
                }
            }
        }

        protected override void OnValidate()
        {
            if (m_Component == null)
            {
                if (TryGetComponent<Button>(out var mButton))
                {
                    m_Component = mButton;
                }
#if OMNI_TEXTMESHPRO_ENABLED
                else if (TryGetComponent<TMP_Text>(out var mText))
                {
                    m_Component = mText;
                }
                else if (TryGetComponent<TMP_InputField>(out var mInputField))
                {
                    m_Component = mInputField;
                }
                else if (TryGetComponent<TMP_Dropdown>(out var mDropdown))
                {
                    m_Component = mDropdown;
                }
#endif
                else if (TryGetComponent<Text>(out var mUnityText))
                {
                    m_Component = mUnityText;
                }
                else if (TryGetComponent<InputField>(out var mUnityInputField))
                {
                    m_Component = mUnityInputField;
                }
                else if (TryGetComponent<Dropdown>(out var mUnityDropdown))
                {
                    m_Component = mUnityDropdown;
                }
                else if (TryGetComponent<Toggle>(out var mUnityToggle))
                {
                    m_Component = mUnityToggle;
                }
                else if (TryGetComponent<Slider>(out var mUnitySlider))
                {
                    m_Component = mUnitySlider;
                }
                else if (TryGetComponent<Canvas>(out var mCanvas))
                {
                    m_Component = mCanvas;
                }
                else
                {
                    if (TryGetComponent<Image>(out var mImage))
                    {
                        m_Component = mImage;
                    }
                }

                NetworkHelper.EditorSaveObject(gameObject);
            }

            if (m_Component != null && ServiceName == GameObject.name)
            {
                ServiceName = "";
            }

            if (string.IsNullOrEmpty(ServiceName))
            {
                if (m_Component != null)
                {
                    ServiceName = m_Component.GetType().Name;
                    NetworkHelper.EditorSaveObject(gameObject);
                }
                else
                {
                    ServiceName = GameObject.name;
                    NetworkHelper.EditorSaveObject(gameObject);
                }
            }
        }
    }
}
