#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Crea la escena RoomTemplates con todas las salas visibles y enlaza RoomTemplateLibrary.
/// </summary>
public static class RoomTemplateSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/RoomTemplates.unity";
    private const string LibraryPath = "Assets/Data/Rooms/RoomTemplateLibrary.asset";
    private const string PrefabFolder = "Assets/Prefabs/Rooms";

    private static readonly TemplateDefinition[] Definitions =
    {
        new TemplateDefinition("EntryRoom", RoomType.Entry, "Entry", 0),
        new TemplateDefinition("NormalRoom_Small", RoomType.Normal, "Normal — Small", 1),
        new TemplateDefinition("NormalRoom_Medium", RoomType.Normal, "Normal — Medium", 2),
        new TemplateDefinition("NormalRoom_Obstacles", RoomType.Normal, "Normal — Obstacles", 3),
        new TemplateDefinition("BossRoom", RoomType.Boss, "Boss", 4),
        new TemplateDefinition("ShopRoom", RoomType.Shop, "Shop", 5),
    };

    [MenuItem("Last Ascention/Setup Room Template Scene")]
    public static void SetupRoomTemplateScene()
    {
        try
        {
            SetupRoomTemplateSceneInternal();
        }
        catch (System.Exception exception)
        {
            Debug.LogError("[RoomTemplates] Setup falló: " + exception.Message + "\n" + exception.StackTrace);
        }
    }

    private static void SetupRoomTemplateSceneInternal()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Data/Rooms");

        EnsureLibraryAssetExists();
        Scene scene = CreateOrOpenScene();
        ClearSceneExceptCamera();

        GameObject workbenchRoot = new GameObject("RoomTemplateWorkbench");
        RoomTemplateWorkbench workbench = workbenchRoot.AddComponent<RoomTemplateWorkbench>();

        float spacing = 28f;
        List<GameObject> normalPrefabs = new List<GameObject>();

        for (int i = 0; i < Definitions.Length; i++)
        {
            TemplateDefinition definition = Definitions[i];
            string prefabPath = PrefabFolder + "/" + definition.prefabName + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[RoomTemplates] No se encontró prefab: " + prefabPath);
                continue;
            }

            Vector3 position = new Vector3(definition.columnIndex * spacing, 0f, 0f);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                continue;
            }

            SceneManager.MoveGameObjectToScene(instance, scene);
            instance.name = definition.displayLabel.Replace(" ", "_");
            instance.transform.position = position;
            instance.transform.SetParent(workbenchRoot.transform, true);

            RoomTemplateSlot slot = instance.GetComponent<RoomTemplateSlot>();
            if (slot == null)
            {
                slot = instance.AddComponent<RoomTemplateSlot>();
            }

            SerializedObject slotObject = new SerializedObject(slot);
            slotObject.FindProperty("slotRoomType").enumValueIndex = (int)definition.roomType;
            slotObject.FindProperty("displayLabel").stringValue = definition.displayLabel;
            slotObject.FindProperty("gizmoColor").colorValue = RoomTemplateSlot.GetDefaultColor(definition.roomType);
            slotObject.ApplyModifiedPropertiesWithoutUndo();

            CreateLabel(instance.transform, definition.displayLabel, definition.roomType);

            if (definition.roomType == RoomType.Entry)
            {
                AssignLibraryPrefab(propertyName: "entryRoom", prefab);
            }
            else if (definition.roomType == RoomType.Boss)
            {
                AssignLibraryPrefab(propertyName: "bossRoom", prefab);
            }
            else if (definition.roomType == RoomType.Shop)
            {
                AssignLibraryPrefab(propertyName: "shopRoom", prefab);
            }
            else if (definition.roomType == RoomType.Normal)
            {
                normalPrefabs.Add(prefab);
            }
        }

        AssignLibraryNormalRooms(normalPrefabs);
        RoomTemplateLibrary library = AssetDatabase.LoadAssetAtPath<RoomTemplateLibrary>(LibraryPath);

        SerializedObject workbenchSo = new SerializedObject(workbench);
        workbenchSo.FindProperty("templateLibrary").objectReferenceValue = library;
        workbenchSo.FindProperty("templateSpacing").floatValue = spacing;
        workbenchSo.ApplyModifiedPropertiesWithoutUndo();
        workbench.RefreshSlotReferences();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        FrameSceneView(workbenchRoot);

        AssetDatabase.SaveAssets();
        Debug.Log("[Last Ascention] Escena RoomTemplates lista: " + ScenePath + " (" + Definitions.Length + " plantillas).");

        try
        {
            WireDungeonBuilders();
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("[RoomTemplates] No se pudieron enlazar DungeonBuilders: " + exception.Message);
        }
    }

    [MenuItem("Last Ascention/Sync Room Library From Template Scene")]
    public static void SyncLibraryFromScene()
    {
        RoomTemplateWorkbench workbench = Object.FindFirstObjectByType<RoomTemplateWorkbench>();
        if (workbench == null || workbench.TemplateLibrary == null)
        {
            Debug.LogError("Abre Assets/Scenes/RoomTemplates.unity y vuelve a ejecutar.");
            return;
        }

        RoomTemplateLibrary library = workbench.TemplateLibrary;
        List<GameObject> normals = new List<GameObject>();

        for (int i = 0; i < workbench.Slots.Length; i++)
        {
            RoomTemplateSlot slot = workbench.Slots[i];
            if (slot == null)
            {
                continue;
            }

            GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(slot.gameObject);
            if (sourcePrefab == null)
            {
                continue;
            }

            switch (slot.SlotRoomType)
            {
                case RoomType.Entry:
                    library.entryRoom = sourcePrefab;
                    break;
                case RoomType.Boss:
                    library.bossRoom = sourcePrefab;
                    break;
                case RoomType.Shop:
                    library.shopRoom = sourcePrefab;
                    break;
                case RoomType.Normal:
                case RoomType.Secret:
                    if (!normals.Contains(sourcePrefab))
                    {
                        normals.Add(sourcePrefab);
                    }
                    break;
            }
        }

        library.normalRooms = normals.ToArray();
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        WireDungeonBuilders();
        Debug.Log("[Last Ascention] RoomTemplateLibrary sincronizada desde escena.");
    }

    private static void EnsureLibraryAssetExists()
    {
        if (AssetDatabase.LoadAssetAtPath<RoomTemplateLibrary>(LibraryPath) != null)
        {
            return;
        }

        RoomTemplateLibrary library = ScriptableObject.CreateInstance<RoomTemplateLibrary>();
        AssetDatabase.CreateAsset(library, LibraryPath);
        AssetDatabase.SaveAssets();
    }

    private static void AssignLibraryPrefab(string propertyName, GameObject prefab)
    {
        RoomTemplateLibrary library = AssetDatabase.LoadAssetAtPath<RoomTemplateLibrary>(LibraryPath);
        if (library == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(library);
        so.FindProperty(propertyName).objectReferenceValue = prefab;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignLibraryNormalRooms(List<GameObject> normalPrefabs)
    {
        RoomTemplateLibrary library = AssetDatabase.LoadAssetAtPath<RoomTemplateLibrary>(LibraryPath);
        if (library == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(library);
        SerializedProperty array = so.FindProperty("normalRooms");
        array.arraySize = normalPrefabs.Count;
        for (int i = 0; i < normalPrefabs.Count; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = normalPrefabs[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Scene CreateOrOpenScene()
    {
        Scene existing = EditorSceneManager.GetSceneByPath(ScenePath);
        if (existing.IsValid())
        {
            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, ScenePath);
        return scene;
    }

    private static void ClearSceneExceptCamera()
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].GetComponent<Camera>() != null || roots[i].GetComponent<Light>() != null)
            {
                if (roots[i].name == "Main Camera")
                {
                    roots[i].transform.position = new Vector3(70f, 0f, -10f);
                }
                continue;
            }

            Object.DestroyImmediate(roots[i]);
        }
    }

    private static void CreateLabel(Transform parent, string text, RoomType roomType)
    {
        GameObject labelObject = new GameObject("Label_" + text.Replace(" ", "_"));
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = new Vector3(0f, 9.5f, 0f);

        TextMesh textMesh = labelObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.characterSize = 0.2f;
        textMesh.fontSize = 48;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = RoomTemplateSlot.GetDefaultColor(roomType);
    }

    private static void WireDungeonBuilders()
    {
        RoomTemplateLibrary library = AssetDatabase.LoadAssetAtPath<RoomTemplateLibrary>(LibraryPath);
        if (library == null)
        {
            Debug.LogWarning("[RoomTemplates] No se encontró RoomTemplateLibrary en " + LibraryPath);
            return;
        }

        string[] gameplayScenes =
        {
            "Assets/Scenes/GameplayScene.unity",
            "Assets/Scenes/TestScene.unity",
            "Assets/Scenes/CityArken.unity",
        };

        string previousScene = SceneManager.GetActiveScene().path;
        for (int i = 0; i < gameplayScenes.Length; i++)
        {
            if (!System.IO.File.Exists(gameplayScenes[i]))
            {
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(gameplayScenes[i], OpenSceneMode.Single);
            DungeonBuilder[] builders = Object.FindObjectsByType<DungeonBuilder>(FindObjectsSortMode.None);
            for (int b = 0; b < builders.Length; b++)
            {
                SerializedObject so = new SerializedObject(builders[b]);
                so.FindProperty("templateLibrary").objectReferenceValue = library;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(builders[b]);
            }

            EditorSceneManager.SaveScene(scene);
        }

        if (!string.IsNullOrEmpty(previousScene) && System.IO.File.Exists(previousScene))
        {
            EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }
        else
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
    }

    private static void FrameSceneView(GameObject focus)
    {
        if (focus == null || SceneView.lastActiveSceneView == null)
        {
            return;
        }

        Bounds bounds = new Bounds(focus.transform.position, Vector3.one);
        RoomTemplateSlot[] slots = focus.GetComponentsInChildren<RoomTemplateSlot>();
        for (int i = 0; i < slots.Length; i++)
        {
            bounds.Encapsulate(slots[i].transform.position);
        }

        SceneView.lastActiveSceneView.Frame(bounds, false);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string folderName = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, folderName);
    }

    private struct TemplateDefinition
    {
        public string prefabName;
        public RoomType roomType;
        public string displayLabel;
        public int columnIndex;

        public TemplateDefinition(string prefabName, RoomType roomType, string displayLabel, int columnIndex)
        {
            this.prefabName = prefabName;
            this.roomType = roomType;
            this.displayLabel = displayLabel;
            this.columnIndex = columnIndex;
        }
    }
}
#endif
