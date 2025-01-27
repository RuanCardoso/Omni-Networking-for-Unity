using System;
using Omni.Core.Interfaces;
using UnityEngine;

[DefaultExecutionOrder(-2000)]
public class NetworkComponentActivator : MonoBehaviour
{
    private MonoBehaviour[] components = Array.Empty<MonoBehaviour>();

    private void Awake()
    {
        if (transform != transform.root)
            throw new Exception("NetworkRegisterDisabledComponents must be on the root object.");

        components = transform.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var component in components)
        {
            if (!component.gameObject.activeSelf && component is IServiceBehaviour mono)
            {
                mono.Internal_Awake();
            }
        }
    }

    private void Start()
    {
        foreach (var component in components)
        {
            if (!component.gameObject.activeSelf && component is IServiceBehaviour mono)
            {
                mono.Internal_Start();
            }
        }
    }
}