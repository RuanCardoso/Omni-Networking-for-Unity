#if UNITY_EDITOR

using Omni.Core;
using Omni.Core.Modules.Connection;
using UnityEditor;
using UnityEngine;

public class OmniEditor
{
    [MenuItem("Omni Networking/Setup", false, 10)]
    static void Setup()
    {
        if (GameObject.Find("Network Manager") == null)
        {
            GameObject manager = new("Network Manager");
            manager.AddComponent<NetworkManager>();
            manager.AddComponent<LiteTransporter>();
            EditorUtility.SetDirty(manager);
        }
    }
}
#endif
