using System;
using Omni.Core.Attributes;
using Omni.Shared;
using UnityEngine;

namespace Omni.Core.Components
{
    [DefaultExecutionOrder(-15000)]
    [DisallowMultipleComponent]
    public sealed class NetworkDontDestroy : MonoBehaviour
    {
        [SerializeField, ReadOnly]
        private string m_Guid;

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
                    "DontDestroy: Only the root object can be set to DontDestroyOnLoad",
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
