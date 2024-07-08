using Omni.Shared;
using UnityEngine;

namespace Omni.Core.Components
{
    [DefaultExecutionOrder(-15000)]
    public class NetworkDontDestroy : MonoBehaviour
    {
        private static NetworkDontDestroy _instance;

        private void Awake()
        {
            if (transform.root == transform)
            {
                if (_instance == null)
                {
                    DontDestroyOnLoad(gameObject);
                    _instance = this;
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
    }
}
