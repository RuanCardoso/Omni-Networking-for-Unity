using Omni.Shared;
using System;
using Omni.Inspector;
using UnityEngine;

namespace Omni.Core.Components
{
    [DefaultExecutionOrder(-15000)]
    [DisallowMultipleComponent]
    public sealed class NetworkDontDestroy : MonoBehaviour
    {
        [SerializeField, ReadOnly] private string m_Guid;

        private void Awake()
        {
            if (transform.root == transform)
            {
                if (NetworkService.TryRegister(this, m_Guid))
                {
                    DontDestroyOnLoad(gameObject);
                    return;
                }

                Destroy(gameObject);
            }
            else
            {
                NetworkLogger.__Log__(
                    $"NetworkDontDestroy Error: Cannot apply DontDestroyOnLoad to '{gameObject.name}' as it is not a root object. This component must be attached to a root GameObject in the hierarchy.",
                    NetworkLogger.LogType.Error
                );
            }
        }

        private void Reset()
        {
            OnValidate();
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(m_Guid))
            {
                m_Guid = Guid.NewGuid().ToString();
                NetworkHelper.EditorSaveObject(gameObject);
            }
        }
    }
}