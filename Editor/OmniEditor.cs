#if UNITY_EDITOR

using Omni.Core;
using Omni.Core.Modules.Connection;
using Omni.Shared;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System;
using System.Linq;

namespace Omni.Editor
{
    public class OmniEditor
    {
        [MenuItem("Omni Networking/Add Network Manager", priority = -2)]
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
                logger = UnityEngine.Object.Instantiate(logger, manager.transform);
                logger.name = "Console";

                // Set dirty to save changes.
                EditorUtility.SetDirty(manager);
            }
        }

        [MenuItem("Omni Networking/Debug/View Debug Logs", priority = 1)]
        static void PrintPlayerLog()
        {
            NetworkLogger.Initialize("EditorLog");
            NetworkLogger.PrintPlayerLog();
        }

        [MenuItem("Omni Networking/Debug/View Encryption Keys", priority = 1)]
        static void GenerateEncryptionKeys()
        {
            string path = NetworkManager.__Internal__Key_Path__;
            Dictionary<string, string> keys = GetKeys(path);
            foreach (var (key, value) in keys)
                Debug.Log($"The key '{key}' has the value -> {value}");
        }

        private static Dictionary<string, string> GetKeys(string filePath)
        {
            if (!File.Exists(filePath))
                return new Dictionary<string, string>();

            string content = File.ReadAllText(filePath);
            var regex = new Regex(@"(?:private|public|internal|protected)?\s+(?:readonly\s+)?(?:static\s+)?byte\[\]\s+(\w+)\s*=\s*new\s+byte\[\]\s*{([^}]*)}", RegexOptions.Singleline);
            var matches = regex.Matches(content);

            var result = new Dictionary<string, string>();
            foreach (Match match in matches)
            {
                string arrayName = match.Groups[1].Value;
                string byteValues = match.Groups[2].Value;

                byte[] byteArray = byteValues.Split(new[] { ',', ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(byte.Parse)
                                            .ToArray();

                string hexFormat = BitConverter.ToString(byteArray).Replace("-", ", ");
                result[arrayName] = $"Hex: [{hexFormat}]\r\n";
            }

            return result;
        }
    }
}

#endif