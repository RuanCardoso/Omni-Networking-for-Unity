#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Omni.Core;
using UnityEngine.SceneManagement;

internal enum CategoryOrder
{
    Server = 0,
    Client = 1,
    Others = 2,
    Disabled = Server + Client + Others // 3
}

internal interface IHierarchyCategory
{
    string Name { get; set; }
    Color Color { get; set; }
}

internal class HierarchyFolderComponent : MonoBehaviour, IHierarchyCategory
{
    public string Name { get; set; } = "Miscellaneous";
    public Color Color { get; set; } = Color.black;
}

internal class HierarchyCategory : IHierarchyCategory
{
    public string Name { get; set; } = "Miscellaneous";
    public Color Color { get; set; } = Color.black;
}

internal class HierarchyFolder
{
    private const string k_FolderPrefix = "--- ";
    private const string K_FolderSuffix = " ---";

    private static bool m_IsOrganizing = false;
    private static readonly Dictionary<string, bool> m_FolderExpandedStates = new();
    private static readonly Dictionary<string, List<GameObject>> m_FolderChildren = new();

    private static readonly HierarchyCategory k_ServerCategory = new()
    {
        Name = "Server",
        Color = new Color(0.0f, 0.5f, 0.0f)
    };

    private static readonly HierarchyCategory k_ClientCategory = new()
    {
        Name = "Client",
        Color = new Color(0.0f, 0.0f, 0.5f)
    };

    private static readonly HierarchyCategory k_CamerasCategory = new()
    {
        Name = "Cameras",
        Color = new Color(0.5f, 0.35f, 0.0f)
    };

    private static readonly HierarchyCategory k_LightsCategory = new()
    {
        Name = "Lights",
        Color = new Color(0.5f, 0.5f, 0.0f)
    };

    private static readonly HierarchyCategory k_MiscellaneousCategory = new()
    {
        Name = "Miscellaneous",
        Color = Color.black
    };

    private static readonly HierarchyCategory k_DisabledCategory = new()
    {
        Name = "Disabled",
        Color = new Color(0.5f, 0.5f, 0.5f)
    };

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
        string folderKey = folderComponent.Name;

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
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

        Dictionary<string, GameObject> folders = new();
        Dictionary<string, List<GameObject>> categorizedObjects = new();

        foreach (GameObject sceneObject in rootObjects)
        {
            if (sceneObject.name.StartsWith(k_FolderPrefix) && sceneObject.name.EndsWith(K_FolderSuffix))
                continue;

            IHierarchyCategory category = DetermineCategory(sceneObject);
            if (!folders.ContainsKey(category.Name))
            {
                GameObject existingFolder = rootObjects.FirstOrDefault(o =>
                    o.name == k_FolderPrefix + category.Name + K_FolderSuffix &&
                    o.GetComponent<HierarchyFolderComponent>() != null);

                if (existingFolder != null)
                {
                    folders[category.Name] = existingFolder;
                }
                else
                {
                    folders[category.Name] = new(k_FolderPrefix + category.Name + K_FolderSuffix)
                    {
                        hideFlags = HideFlags.HideInInspector | HideFlags.DontSave | HideFlags.NotEditable
                    };

                    var folderComponent = folders[category.Name].AddComponent<HierarchyFolderComponent>();
                    folderComponent.Name = category.Name;
                    folderComponent.Color = category.Color;
                }

                categorizedObjects[category.Name] = new List<GameObject>();
            }

            categorizedObjects[category.Name].Add(sceneObject);
        }

        var sortedCategories = folders.Keys.OrderBy(k =>
        {
            if (k.Contains("Server")) return CategoryOrder.Server;
            else if (k.Contains("Client")) return CategoryOrder.Client;
            else if (k.Contains("Disabled")) return CategoryOrder.Disabled;
            else return CategoryOrder.Others;
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

    private static IHierarchyCategory DetermineCategory(GameObject sceneObject)
    {
        if (!sceneObject.activeSelf)
            return k_DisabledCategory;

        if (sceneObject.TryGetComponent<NetworkIdentity>(out var networkIdentity))
            return networkIdentity.IsServer ? k_ServerCategory : k_ClientCategory;
        else if (sceneObject.name.Contains("Server"))
            return k_ServerCategory;
        else if (sceneObject.name.Contains("Client"))
            return k_ClientCategory;
        else if (sceneObject.TryGetComponent<Camera>(out _))
            return k_CamerasCategory;
        else if (sceneObject.TryGetComponent<Light>(out _))
            return k_LightsCategory;

        return k_MiscellaneousCategory;
    }
}

[CustomEditor(typeof(HierarchyFolderComponent))]
internal class HierarchyFolderEditor : Editor
{
    public override void OnInspectorGUI() { }

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyWindowItemOnGUI;
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
    }

    private static void OnHierarchyWindowItemOnGUI(int instanceId, Rect selectionRect)
    {
        GameObject obj = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
        if (obj == null)
            return;

        if (obj.TryGetComponent<HierarchyFolderComponent>(out var folderComponent))
        {
            var folderIcon = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
            EditorGUI.DrawRect(selectionRect, folderComponent.Color);

            float indent = EditorGUI.indentLevel * 16f;
            Rect iconRect = new Rect(selectionRect.x + indent, selectionRect.y, 16, 16);
            GUI.DrawTexture(iconRect, folderIcon);

            string displayName = obj.name;
            if (displayName.StartsWith("--- ") && displayName.EndsWith(" ---"))
                displayName = displayName[4..^4];

            Vector2 textSize = EditorStyles.boldLabel.CalcSize(new(displayName));
            float centerX = selectionRect.x + (selectionRect.width - textSize.x) / 2f;

            Rect labelRect = new(centerX, selectionRect.y, textSize.x, selectionRect.height);
            GUIStyle labelStyle = new(EditorStyles.boldLabel);
            labelStyle.normal.textColor = Color.white;
            EditorGUI.LabelField(labelRect, displayName, labelStyle);
        }
    }
}
#endif
