using System;
using Omni.Core.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        }

        protected override void OnValidate()
        {
            if (string.IsNullOrEmpty(ServiceName))
            {
                ServiceName = GameObject.name;
                NetworkHelper.EditorSaveObject(gameObject);
            }

            if (m_Component == null)
            {
                if (TryGetComponent<Button>(out var mButton))
                {
                    m_Component = mButton;
                }
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
        }
    }
}
