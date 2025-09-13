using Omni.Shared;
using System;
using Omni.Inspector;
using UnityEngine;

namespace Omni.Core.Components
{
    [DefaultExecutionOrder(-15000)]
    [DisallowMultipleComponent]
    [DeclareBoxGroup("GUID")]
    public sealed class NetworkDontDestroy : OmniBehaviour
    {
        [Group("GUID"), HideLabel]
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
                    $"NetworkDontDestroy error: '{gameObject.name}' is not a root object. " +
                    "This component must be on a root GameObject.",
                    NetworkLogger.LogType.Warning
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