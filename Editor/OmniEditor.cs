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
        [MenuItem("Omni Networking/Setup", false, -100)]
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

        [MenuItem("Omni Networking/View Client Debug Logs", false, 30)]
        static void PrintPlayerLog()
        {
            NetworkLogger.Initialize("EditorLog");
            NetworkLogger.PrintPlayerLog();
        }

        [MenuItem("Omni Networking/View Encryption Keys", false, 30)]
        static void GenerateEncryptionKeys()
        {
            string path = Path.Combine(
                 Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                 "__omni_development_keys__"
             );

            Dictionary<string, string> keys = GetKeys(path);
            foreach (var (key, value) in keys)
                Debug.Log($"The key '{key}' has the value -> {value}");
        }

        static Dictionary<string, string> GetKeys(string filePath)
        {
            if (!File.Exists(filePath))
                return new Dictionary<string, string>();

            // Lê todo o conteúdo do arquivo
            string content = File.ReadAllText(filePath);

            // Regex para encontrar declarações de arrays de bytes
            var regex = new Regex(@"(?:private|public|internal|protected)?\s+(?:readonly\s+)?(?:static\s+)?byte\[\]\s+(\w+)\s*=\s*new\s+byte\[\]\s*{([^}]*)}", RegexOptions.Singleline);
            var matches = regex.Matches(content);

            // Dicionário para armazenar os resultados (nome do array -> representação em string)
            var result = new Dictionary<string, string>();

            foreach (Match match in matches)
            {
                string arrayName = match.Groups[1].Value;
                string byteValues = match.Groups[2].Value;

                // Converte os valores em string para um array de bytes
                byte[] byteArray = byteValues.Split(new[] { ',', ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(byte.Parse)
                                            .ToArray();

                // Cria representações em diferentes formatos
                string hexFormat = BitConverter.ToString(byteArray).Replace("-", ", ");
                string decFormat = string.Join(", ", byteArray);
                string asciiFormat = new string(byteArray.Select(b => b >= 32 && b <= 126 ? (char)b : '.').ToArray());

                // Combina as representações em uma única string
                result[arrayName] = $"Hex: [{hexFormat}]\nDec: [{decFormat}]\nASCII: {asciiFormat}";
            }

            // Identifica a chave interna se presente
            if (content.Contains("NetworkManager.__Internal__Key__"))
            {
                var assignmentMatch = Regex.Match(content, @"NetworkManager\.__Internal__Key__\s*=\s*[^.]+\.(\w+);");
                if (assignmentMatch.Success)
                {
                    string keyName = assignmentMatch.Groups[1].Value;
                    if (result.ContainsKey(keyName))
                    {
                        result[keyName] += "\n[CHAVE INTERNA UTILIZADA NO NETWORKMANAGER]";
                    }
                }
            }

            return result;
        }
    }
}

#endif