#if UNITY_EDITOR

using Omni.Core;
using Omni.Core.Modules.Connection;
using Omni.Shared;
using UnityEditor;
using UnityEngine;

namespace Omni.Editor
{
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

                var server = new GameObject(">> [Server Code Here] <<");
                server.transform.parent = manager.transform;
                
                var client = new GameObject(">> [Client Code Here] <<");
                client.transform.parent = manager.transform;

                // Add logger prefab.
                GameObject logger = Resources.Load<GameObject>("IngameDebugConsole");
                logger = Object.Instantiate(logger, manager.transform);
                logger.name = "Console";

                // Set dirty to save changes.
                EditorUtility.SetDirty(manager);
            }
        }

        [MenuItem("Omni Networking/Print Player Log", false, 30)]
        static void PrintPlayerLog()
        {
            NetworkLogger.PrintPlayerLog();
        }
    }
#endif
}
