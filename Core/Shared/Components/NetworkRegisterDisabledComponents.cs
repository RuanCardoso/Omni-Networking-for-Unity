using Omni.Core.Interfaces;
using UnityEngine;

[DefaultExecutionOrder(-2000)]
public class NetworkRegisterDisabledComponents : MonoBehaviour
{
    private MonoBehaviour[] components = new MonoBehaviour[0];

    private void Awake()
    {
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
