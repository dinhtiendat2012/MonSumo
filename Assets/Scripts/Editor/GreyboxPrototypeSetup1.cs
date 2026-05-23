using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

public static class GreyboxPrototypeSetup1
{
    private const string ScenePath = "Assets/Scenes/GrayboxPrototype.unity";
    private const string SkillFolder = "Assets/Asset/Skill";
    private const string MapFolder = "Assets/Asset/Map";
    private const string ItemPrefabFolder = "Assets/Prefab/Items";

    [MenuItem("Tools/MonSumo/Build Greybox Prototype Scene")]
    public static void BuildScene()
    {
        EnsureFolders();

        Material floorMaterial = CreateMaterial("MAT_Greybox_Floor", new Color(0.35f, 0.38f, 0.42f), MapFolder);
        Material edgeMaterial = CreateMaterial("MAT_Greybox_Edge", new Color(0.9f, 0.18f, 0.18f), MapFolder);
        Material playerMaterial = CreateMaterial("MAT_Player", new Color(0.2f, 0.55f, 1f), MapFolder);
        Material targetMaterial = CreateMaterial("MAT_Target", new Color(1f, 0.38f, 0.25f), MapFolder);
        Material pickupMaterial = CreateMaterial("MAT_Pickup", new Color(0.95f, 0.78f, 0.24f), MapFolder);

        MeleePushSkill meleeSkill = CreateSkillAsset<MeleePushSkill>("SK_MeleePush", "Melee Push");
        ProjectilePushSkill projectileSkill = CreateSkillAsset<ProjectilePushSkill>("SK_ProjectilePush", "Projectile Push");
        TimedBombSkill bombSkill = CreateSkillAsset<TimedBombSkill>("SK_TimedBombPush", "Timed Bomb Push");

        GameObject meleePickup = CreateItemPrefab("PF_Item_MeleePush", meleeSkill, pickupMaterial, PrimitiveShape.Cube);
        GameObject projectilePickup = CreateItemPrefab("PF_Item_ProjectilePush", projectileSkill, pickupMaterial, PrimitiveShape.Sphere);
        GameObject bombPickup = CreateItemPrefab("PF_Item_TimedBombPush", bombSkill, pickupMaterial, PrimitiveShape.Cube);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ClearGeneratedObjects();

        GameObject root = new GameObject("Greybox Prototype Setup1");
        ConfigureCamera();
        CreateArena(root.transform, floorMaterial, edgeMaterial);
        CreatePlayer("Player1", new Vector3(-3f, 1.1f, 0f), playerMaterial, true, root.transform);
        CreatePlayer("Push Test Target1", new Vector3(3f, 1.1f, 0f), targetMaterial, false, root.transform);

        Transform[] spawnPoints = CreateSpawnPoints(root.transform);
        ItemSpawner spawner = new GameObject("Item Spawner").AddComponent<ItemSpawner>();
        spawner.transform.SetParent(root.transform);
        SetSerializedValue(spawner, "itemPrefabs", new[] { meleePickup, projectilePickup, bombPickup });
        SetSerializedValue(spawner, "spawnPoints", spawnPoints);
        SetSerializedValue(spawner, "spawnCooldown", 3f);
        SetSerializedValue(spawner, "maxItems", 3);
        SetSerializedValue(spawner, "overlapCheckRadius", 0.9f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Greybox Prototype scene built with ProBuilder arena, players, item prefabs, skill assets, and spawner.");
    }

    private enum PrimitiveShape
    {
        Cube,
        Sphere
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Scripts");
        EnsureFolder("Assets/Scripts", "Editor");
        EnsureFolder("Assets", "Asset");
        EnsureFolder("Assets/Asset", "Skill");
        EnsureFolder("Assets/Asset", "Map");
        EnsureFolder("Assets", "Prefab");
        EnsureFolder("Assets/Prefab", "Items");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static Material CreateMaterial(string assetName, Color color, string folder)
    {
        string path = $"{folder}/{assetName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static T CreateSkillAsset<T>(string assetName, string displayName) where T : SkillDefinition
    {
        string path = $"{SkillFolder}/{assetName}.asset";
        T skill = AssetDatabase.LoadAssetAtPath<T>(path);
        if (skill == null)
        {
            skill = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(skill, path);
        }

        SetSerializedValue(skill, "displayName", displayName);
        return skill;
    }

    private static GameObject CreateItemPrefab(string prefabName, SkillDefinition skill, Material material, PrimitiveShape shape)
    {
        string path = $"{ItemPrefabFolder}/{prefabName}.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null && existing.TryGetComponent(out ItemPickup existingPickup))
        {
            SetSerializedValue(existingPickup, "skill", skill);
            return existing;
        }

        GameObject itemObject = CreateProBuilderShape(shape, prefabName, Vector3.one * 0.75f, material);
        Collider collider = shape == PrimitiveShape.Sphere
            ? itemObject.AddComponent<SphereCollider>()
            : itemObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        itemObject.AddComponent<Rigidbody>().isKinematic = true;

        ItemPickup pickup = itemObject.AddComponent<ItemPickup>();
        SetSerializedValue(pickup, "skill", skill);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(itemObject, path);
        Object.DestroyImmediate(itemObject);
        return prefab;
    }

    private static void ClearGeneratedObjects()
    {
        GameObject existing = GameObject.Find("Greybox Prototype Setup1");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject legacy = GameObject.Find("Greybox Prototype Setup");
        if (legacy != null)
        {
            Object.DestroyImmediate(legacy);
        }
    }

    private static void ConfigureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }

        camera.transform.position = new Vector3(0f, 12f, -13f);
        camera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
        camera.orthographic = true;
        camera.orthographicSize = 8.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.1f);
    }

    private static void CreateArena(Transform parent, Material floorMaterial, Material edgeMaterial)
    {
        GameObject floor = CreateProBuilderCube("Arena Floor1", new Vector3(16f, 0.35f, 16f), floorMaterial);
        floor.transform.position = new Vector3(0f, -0.2f, 0f);
        floor.transform.SetParent(parent);
        floor.AddComponent<BoxCollider>();

        CreateEdgeMarker("North Edge Marker1", new Vector3(0f, 0.05f, 8f), new Vector3(16f, 0.15f, 0.25f), parent, edgeMaterial);
        CreateEdgeMarker("South Edge Marker1", new Vector3(0f, 0.05f, -8f), new Vector3(16f, 0.15f, 0.25f), parent, edgeMaterial);
        CreateEdgeMarker("East Edge Marker1", new Vector3(8f, 0.05f, 0f), new Vector3(0.25f, 0.15f, 16f), parent, edgeMaterial);
        CreateEdgeMarker("West Edge Marker1", new Vector3(-8f, 0.05f, 0f), new Vector3(0.25f, 0.15f, 16f), parent, edgeMaterial);
    }

    private static void CreateEdgeMarker(string name, Vector3 position, Vector3 size, Transform parent, Material material)
    {
        GameObject edge = CreateProBuilderCube(name, size, material);
        edge.transform.position = position;
        edge.transform.SetParent(parent);
    }

    private static void CreatePlayer(string name, Vector3 position, Material material, bool controllable, Transform parent)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = name;
        player.transform.position = position;
        player.transform.SetParent(parent);
        player.GetComponent<Renderer>().sharedMaterial = material;

        Rigidbody body = player.AddComponent<Rigidbody>();
        body.mass = 1.5f;
        body.constraints = RigidbodyConstraints.FreezeRotation;

        if (controllable)
        {
            player.AddComponent<GreyboxPlayerController1>();
            PlayerInventory inventory = player.AddComponent<PlayerInventory>();
            GameObject origin = new GameObject("Skill Origin1");
            origin.transform.SetParent(player.transform);
            origin.transform.localPosition = new Vector3(0f, 0.2f, 0.7f);
            origin.transform.localRotation = Quaternion.identity;
            SetSerializedValue(inventory, "skillOrigin", origin.transform);
        }
    }

    private static Transform[] CreateSpawnPoints(Transform parent)
    {
        Vector3[] positions =
        {
            new Vector3(-4f, 0.5f, -4f),
            new Vector3(0f, 0.5f, -4f),
            new Vector3(4f, 0.5f, -4f),
            new Vector3(-4f, 0.5f, 4f),
            new Vector3(0f, 0.5f, 4f),
            new Vector3(4f, 0.5f, 4f)
        };

        Transform[] points = new Transform[positions.Length];
        GameObject holder = new GameObject("Item Spawn Points1");
        holder.transform.SetParent(parent);

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject point = new GameObject($"Spawn Point {i + 1}_1");
            point.transform.SetParent(holder.transform);
            point.transform.position = positions[i];
            points[i] = point.transform;
        }

        return points;
    }

    private static GameObject CreateProBuilderCube(string name, Vector3 size, Material material)
    {
        return CreateProBuilderShape(PrimitiveShape.Cube, name, size, material);
    }

    private static GameObject CreateProBuilderShape(PrimitiveShape shape, string name, Vector3 size, Material material)
    {
        ProBuilderMesh mesh = shape == PrimitiveShape.Sphere
            ? ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, size.x * 0.5f, 2)
            : ShapeGenerator.GenerateCube(PivotLocation.Center, size);

        GameObject gameObject = mesh.gameObject;
        gameObject.name = name;

        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        return gameObject;
    }

    private static void SetSerializedValue(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetSerializedValue(Object target, string propertyName, Object[] values)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        property.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetSerializedValue(Object target, string propertyName, string value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        property.stringValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetSerializedValue(Object target, string propertyName, float value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        property.floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetSerializedValue(Object target, string propertyName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        property.intValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
}
