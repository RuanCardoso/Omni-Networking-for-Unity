#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Omni.Core;

public class HierarchyFolder
{
    private const string k_FolderPrefix = "--- ";
    private const string K_FolderSuffix = " ---";

    private static bool m_IsOrganizing = false;
    private static readonly Dictionary<string, bool> m_FolderExpandedStates = new();
    private static readonly Dictionary<string, List<GameObject>> m_FolderChildren = new();

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyWindowItemOnGUI;

        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
    }

    private static void OnHierarchyChanged()
    {
        if (m_IsOrganizing)
            return;

        EditorApplication.delayCall -= DelayedOrganizeHierarchy;
        EditorApplication.delayCall += DelayedOrganizeHierarchy;
    }

    private static void DelayedOrganizeHierarchy()
    {
        if (!m_IsOrganizing)
        {
            m_IsOrganizing = true;
            OrganizeHierarchy();
            m_IsOrganizing = false;
        }
    }

    private static void OnHierarchyWindowItemOnGUI(int instanceId, Rect selectionRect)
    {
        GameObject instance = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
        if (instance == null || !instance.TryGetComponent<HierarchyFolderComponent>(out var folderComponent))
            return;

        Rect foldoutRect = new(selectionRect.x + EditorGUI.indentLevel * 16f, selectionRect.y, 16, 16);
        string folderKey = folderComponent.category;

        if (!m_FolderExpandedStates.ContainsKey(folderKey))
            m_FolderExpandedStates[folderKey] = false;

        bool isExpanded = EditorGUI.Foldout(foldoutRect, m_FolderExpandedStates[folderKey], "");
        if (isExpanded != m_FolderExpandedStates[folderKey])
        {
            m_FolderExpandedStates[folderKey] = isExpanded;
            UpdateChildrenVisibility(folderKey, isExpanded);
        }
    }

    private static void UpdateChildrenVisibility(string category, bool isExpanded)
    {
        if (!m_FolderChildren.ContainsKey(category))
            return;

        foreach (var child in m_FolderChildren[category])
        {
            if (child != null)
            {
                if (isExpanded)
                {
                    child.hideFlags &= ~HideFlags.HideInHierarchy;
                }
                else
                {
                    child.hideFlags |= HideFlags.HideInHierarchy;
                }
            }
        }
    }

    private static void OrganizeHierarchy()
    {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        Dictionary<string, GameObject> folders = new();
        Dictionary<string, List<GameObject>> categorizedObjects = new();

        foreach (GameObject sceneObject in rootObjects)
        {
            if (sceneObject.name.StartsWith(k_FolderPrefix) && sceneObject.name.EndsWith(K_FolderSuffix))
                continue;

            string category = DetermineCategory(sceneObject, out Color color);
            if (!folders.ContainsKey(category))
            {
                GameObject existingFolder = rootObjects.FirstOrDefault(o =>
                    o.name == k_FolderPrefix + category + K_FolderSuffix &&
                    o.GetComponent<HierarchyFolderComponent>() != null);

                if (existingFolder != null)
                {
                    folders[category] = existingFolder;
                }
                else
                {
                    folders[category] = new(k_FolderPrefix + category + K_FolderSuffix)
                    {
                        hideFlags = HideFlags.HideInInspector | HideFlags.DontSave | HideFlags.NotEditable
                    };

                    var folderComponent = folders[category].AddComponent<HierarchyFolderComponent>();
                    folderComponent.category = category;
                    folderComponent.color = color;
                }

                categorizedObjects[category] = new List<GameObject>();
            }

            categorizedObjects[category].Add(sceneObject);
        }

        var sortedCategories = folders.Keys.OrderBy(k =>
        {
            if (k.Contains("Server")) return 0;
            if (k.Contains("Client")) return 1;
            return 2;
        }).ThenBy(k => k).ToList();

        int currentIndex = 0;
        foreach (string category in sortedCategories)
        {
            folders[category].transform.SetSiblingIndex(currentIndex++);
            m_FolderChildren[category] = new List<GameObject>(categorizedObjects[category]);

            foreach (GameObject sceneObject in categorizedObjects[category])
            {
                sceneObject.transform.SetSiblingIndex(currentIndex++);
                if (!m_FolderExpandedStates.ContainsKey(category) || !m_FolderExpandedStates[category])
                {
                    sceneObject.hideFlags |= HideFlags.HideInHierarchy;
                }
                else
                {
                    sceneObject.hideFlags &= ~HideFlags.HideInHierarchy;
                }
            }
        }
    }

    private static string DetermineCategory(GameObject obj, out Color color)
    {
        if (obj.TryGetComponent<NetworkIdentity>(out var networkIdentity))
        {
            bool isServer = networkIdentity.IsServer;
            color = isServer ? new Color(0.0f, 0.5f, 0.0f) : new Color(0.0f, 0.0f, 0.5f);
            return isServer ? "Server" : "Client";
        }
        else if (obj.name.Contains("Server"))
        {
            color = new Color(0.0f, 0.5f, 0.0f);
            return "Server";
        }
        else if (obj.name.Contains("Client"))
        {
            color = new Color(0.0f, 0.0f, 0.5f);
            return "Client";
        }
        else if (obj.TryGetComponent<Camera>(out _))
        {
            color = new Color(0.5f, 0.35f, 0.0f);
            return "Cameras";
        }
        else if (obj.TryGetComponent<Light>(out _))
        {
            color = new Color(0.5f, 0.5f, 0.0f);
            return "Lights";
        }

        color = Color.black;
        return "Miscellaneous";
    }
}

// Componente para marcar os objetos como pastas
public class HierarchyFolderComponent : MonoBehaviour
{
    internal string category = "Miscellaneous";
    internal Color color = Color.black;
}

// Custom Editor para personalizar a aparência na hierarquia
[CustomEditor(typeof(HierarchyFolderComponent))]
public class HierarchyFolderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Não mostra nada no inspector
    }

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
    }

    private static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null)
            return;

        if (obj.TryGetComponent<HierarchyFolderComponent>(out var folderComponent))
        {
            var folderIcon = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
            // Desenha um retângulo preto para cobrir os elementos padrão
            EditorGUI.DrawRect(selectionRect, folderComponent.color);

            // Calcula a posição do ícone considerando a indentação
            float indent = EditorGUI.indentLevel * 16f;
            Rect iconRect = new Rect(selectionRect.x + indent, selectionRect.y, 16, 16);
            GUI.DrawTexture(iconRect, folderIcon);

            // Remove o prefixo e sufixo do nome para exibição
            string displayName = obj.name;
            if (displayName.StartsWith("--- ") && displayName.EndsWith(" ---"))
            {
                displayName = displayName.Substring(4, displayName.Length - 8);
            }

            // Calcula o tamanho do texto
            GUIContent content = new GUIContent(displayName);
            Vector2 textSize = EditorStyles.boldLabel.CalcSize(content);

            // Calcula a posição centralizada
            float centerX = selectionRect.x + (selectionRect.width - textSize.x) / 2f;
            Rect labelRect = new Rect(centerX, selectionRect.y, textSize.x, selectionRect.height);

            EditorGUI.LabelField(labelRect, displayName, EditorStyles.boldLabel);
        }
    }
}
#endif
