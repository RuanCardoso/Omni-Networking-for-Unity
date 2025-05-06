#if UNITY_EDITOR_DISABLED
using System.Collections.Generic;
using System.Linq;
using Omni.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

internal enum CategoryOrder
{
    Server = 0,
    Client = 1,
    Others = 2,
    Offline = 3,
    Disabled = Server + Client + Others + Offline // 6
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
    internal const string k_FolderPrefix = "--- ";
    internal const string k_FolderSuffix = " ---";

    internal const float k_IconWidth = 16f;
    internal const float k_PixelWidth = 32f;

    private static bool m_IsOrganizing = false;
    private static readonly Dictionary<string, bool> m_FolderExpandedStates = new();

    private static readonly HierarchyCategory k_ServerCategory = new()
    {
        Name = "Server",
        Color = new Color(0.0f, 0.4f, 0.0f) // Verde militar escuro
    };

    private static readonly HierarchyCategory k_ClientCategory = new()
    {
        Name = "Client",
        Color = new Color(0.0f, 0.2f, 0.5f) // Azul escuro profundo
    };

    private static readonly HierarchyCategory k_CamerasCategory = new()
    {
        Name = "Cameras",
        Color = new Color(0.4f, 0.25f, 0.0f) // Marrom escuro/cobre
    };

    private static readonly HierarchyCategory k_LightsCategory = new()
    {
        Name = "Lights",
        Color = new Color(0.4f, 0.4f, 0.0f) // Amarelo queimado
    };

    private static readonly HierarchyCategory k_MiscellaneousCategory = new()
    {
        Name = "Miscellaneous",
        Color = new Color(0f, 0f, 0f) // Preto
    };

    private static readonly HierarchyCategory k_DisabledCategory = new()
    {
        Name = "Disabled",
        Color = new Color(0.15f, 0.15f, 0.15f) // Quase preto
    };

    private static readonly HierarchyCategory k_OfflineCategory = new()
    {
        Name = "Offline",
        Color = new Color(0.3f, 0.0f, 0.0f) // Vinho escuro
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

        string folderKey = folderComponent.Name;
        if (!m_FolderExpandedStates.ContainsKey(folderKey))
            m_FolderExpandedStates[folderKey] = false;

        Rect foldoutRect = new(k_PixelWidth, selectionRect.y, k_IconWidth, k_IconWidth);
        GUIContent icon = EditorGUIUtility.IconContent(m_FolderExpandedStates[folderKey] ? "Toolbar Minus" : "Toolbar Plus");
        if (GUI.Button(foldoutRect, icon, GUIStyle.none))
            m_FolderExpandedStates[folderKey] = !m_FolderExpandedStates[folderKey];
    }

    private static void OrganizeHierarchy()
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        Dictionary<string, GameObject> folders = new();
        Dictionary<string, List<GameObject>> categorizedObjects = new();

        foreach (GameObject sceneObject in rootObjects)
        {
            if (sceneObject.name.StartsWith(k_FolderPrefix) && sceneObject.name.EndsWith(k_FolderSuffix))
            {
                var sceneVisibilityManager = SceneVisibilityManager.instance;
                sceneVisibilityManager.DisablePicking(sceneObject, false);
                sceneVisibilityManager.Hide(sceneObject, false);

                continue;
            }

            IHierarchyCategory category = GetCategory(sceneObject);
            if (!folders.ContainsKey(category.Name))
            {
                GameObject existingFolder = rootObjects.FirstOrDefault(o =>
                    o.name == k_FolderPrefix + category.Name + k_FolderSuffix &&
                    o.GetComponent<HierarchyFolderComponent>() != null);

                if (existingFolder != null)
                    folders[category.Name] = existingFolder;
                else
                {
                    GameObject folderObj = new(k_FolderPrefix + category.Name + k_FolderSuffix)
                    {
                        hideFlags = HideFlags.HideInInspector | HideFlags.DontSave | HideFlags.NotEditable |
                                  HideFlags.DontUnloadUnusedAsset
                    };

                    var sceneVisibilityManager = SceneVisibilityManager.instance;
                    sceneVisibilityManager.DisablePicking(folderObj, false);
                    sceneVisibilityManager.Hide(folderObj, false);

                    var folderComponent = folderObj.AddComponent<HierarchyFolderComponent>();
                    folderComponent.Name = category.Name;
                    folderComponent.Color = category.Color;

                    folders[category.Name] = folderObj;
                }

                categorizedObjects[category.Name] = new List<GameObject>();
            }

            categorizedObjects[category.Name].Add(sceneObject);
        }

        foreach (GameObject sceneObject in rootObjects)
        {
            if (sceneObject.name.StartsWith(k_FolderPrefix) && sceneObject.name.EndsWith(k_FolderSuffix))
            {
                string categoryName = sceneObject.name[4..^4];
                if (!categorizedObjects.ContainsKey(categoryName))
                    sceneObject.hideFlags |= HideFlags.HideInHierarchy;
                else sceneObject.hideFlags &= ~HideFlags.HideInHierarchy;
            }
        }

        var sortedCategories = folders.Keys.OrderBy(k =>
        {
            if (k.Contains("Server")) return CategoryOrder.Server;
            else if (k.Contains("Client")) return CategoryOrder.Client;
            else if (k.Contains("Disabled")) return CategoryOrder.Disabled;
            else if (k.Contains("Offline")) return CategoryOrder.Offline;
            else return CategoryOrder.Others;
        }).ThenBy(k => k).ToList();

        int currentIndex = 0;
        foreach (string category in sortedCategories)
        {
            folders[category].transform.SetSiblingIndex(currentIndex++);
            foreach (GameObject sceneObject in categorizedObjects[category])
            {
                sceneObject.transform.SetSiblingIndex(currentIndex++);
                if (!m_FolderExpandedStates.ContainsKey(category) || !m_FolderExpandedStates[category])
                    sceneObject.hideFlags |= HideFlags.HideInHierarchy;
                else sceneObject.hideFlags &= ~HideFlags.HideInHierarchy;
            }
        }
    }

    private static IHierarchyCategory GetCategory(GameObject sceneObject)
    {
        if (!sceneObject.activeSelf)
            return k_DisabledCategory;

        if (sceneObject.TryGetComponent<NetworkIdentity>(out var networkIdentity))
            return networkIdentity.IsRegistered ? networkIdentity.IsServer ? k_ServerCategory : k_ClientCategory : k_OfflineCategory;
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
        if (obj != null)
        {
            if (obj.TryGetComponent<HierarchyFolderComponent>(out var folderComponent))
            {
                Rect fullWidthRect = new(HierarchyFolder.k_PixelWidth, selectionRect.y, EditorGUIUtility.currentViewWidth - HierarchyFolder.k_PixelWidth, selectionRect.height);
                EditorGUI.DrawRect(fullWidthRect, folderComponent.Color);

                var folderIcon = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
                Rect folderIconRect = new(HierarchyFolder.k_PixelWidth + HierarchyFolder.k_IconWidth, selectionRect.y, HierarchyFolder.k_IconWidth, HierarchyFolder.k_IconWidth);
                GUI.DrawTexture(folderIconRect, folderIcon);

                string displayName = obj.name;
                if (displayName.StartsWith(HierarchyFolder.k_FolderPrefix) && displayName.EndsWith(HierarchyFolder.k_FolderSuffix))
                    displayName = displayName[4..^4];

                GUIStyle labelStyle = new(EditorStyles.boldLabel);
                labelStyle.normal.textColor = Color.white;

                Vector2 textSize = EditorStyles.boldLabel.CalcSize(new(displayName));
                float centerX = selectionRect.x + (selectionRect.width - textSize.x) / 2f;

                Rect labelRect = new(centerX, selectionRect.y, textSize.x, selectionRect.height);
                EditorGUI.LabelField(labelRect, displayName, labelStyle);
            }
        }
    }
}
#endif
