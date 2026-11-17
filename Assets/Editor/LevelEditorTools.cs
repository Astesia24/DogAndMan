using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 关卡编辑器工具集。
///
/// 全部工具都放在 Tools/关卡编辑器 下，放在当前 Scene 视图的视野中心。
/// 除了“新建关卡场景”之外，所有工具都只是往场景里放一个物体，
/// 不会修改已有的墙、也不会重建场景。
/// </summary>
public static class LevelEditorTools
{
    private const string SpritePath = "Assets/Art/Sprites/Square.png";
    private const string RootName = "__Level";

    // ───────────────────────── 新建关卡 ─────────────────────────

    [MenuItem("Tools/关卡编辑器/新建关卡场景", false, 0)]
    public static void CreateLevel()
    {
        float width = 40f;
        float height = 24f;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var root = new GameObject(RootName).transform;

        // 地板
        var floor = NewSprite("Floor", new Vector2(width, height), new Color(0.13f, 0.13f, 0.15f), 0, root);
        floor.transform.localPosition = new Vector3(width * 0.5f, height * 0.5f, 0f);

        // 四面外墙
        var walls = new GameObject("Walls").transform;
        walls.SetParent(root, false);

        MakeWall(walls, new Vector2(width * 0.5f, -0.5f), new Vector2(width + 2f, 1f));
        MakeWall(walls, new Vector2(width * 0.5f, height + 0.5f), new Vector2(width + 2f, 1f));
        MakeWall(walls, new Vector2(-0.5f, height * 0.5f), new Vector2(1f, height + 2f));
        MakeWall(walls, new Vector2(width + 0.5f, height * 0.5f), new Vector2(1f, height + 2f));

        // 寻路网格
        var gridGo = new GameObject("LevelGrid");
        gridGo.transform.SetParent(root, false);

        var grid = gridGo.AddComponent<LevelGrid>();
        grid.origin = Vector2.zero;
        grid.size = new Vector2(width, height);
        grid.cellSize = 0.5f;
        grid.blockers = SolidMask();
        grid.bakeOnAwake = true;

        // 狗和魔女
        var dog = SpawnPrefab("P_Dog", "Dog", new Vector2(5f, height * 0.5f), root);
        var witch = SpawnPrefab("P_Witch", "Witch", new Vector2(7f, height * 0.5f), root);

        // 相机
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(root, false);

        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 7f;
        cam.backgroundColor = Color.black;

        var follow = camGo.AddComponent<FollowCamera>();
        follow.target = dog.transform;
        follow.mapOrigin = Vector2.zero;
        follow.mapSize = new Vector2(width, height);
        follow.viewWidth = 24f;
        follow.viewHeight = 14f;

        // 出口
        MakeExit(root, new Vector2(width - 2f, height * 0.5f), new Vector2(2f, 3f));

        Directory.CreateDirectory("Assets/Scenes");

        if (!EditorSceneManager.SaveScene(scene, "Assets/Scenes/NewLevel.unity"))
        {
            Debug.LogError("[关卡编辑器] 场景保存失败");
            return;
        }

        AddToBuildSettings("Assets/Scenes/NewLevel.unity");

        Debug.Log($"[关卡编辑器] 已生成 NewLevel（{width}×{height}），狗在 (5,{height * 0.5f})，魔女在旁边。");
    }

    // ───────────────────────── 放置 ─────────────────────────

    [MenuItem("Tools/关卡编辑器/放墙", false, 10)]
    public static void PlaceWall()
    {
        var walls = GameObject.Find("Walls");
        var parent = walls != null ? walls.transform : RootOrNull();

        var go = NewSprite("Wall", new Vector2(2f, 1f), new Color(0.34f, 0.30f, 0.25f), 1, parent);
        go.transform.position = SceneCenter();
        go.layer = WallLayer();

        AddBox(go, new Vector2(2f, 1f));

        Select(go);
        Debug.Log("[关卡编辑器] 放了一堵墙（2×1）。改 BoxCollider2D 的 Size 可以调大小。");
    }

    [MenuItem("Tools/关卡编辑器/放小狗洞", false, 11)]
    public static void PlaceDogHole()
    {
        // 故意做小：0.9 × 0.9 米，只有狗钻得过
        var size = new Vector2(0.9f, 0.9f);

        var go = NewSprite("DogHole", size, new Color(0.05f, 0.05f, 0.06f, 1f), 2, RootOrNull());
        go.transform.position = SceneCenter();

        int holeLayer = LayerMask.NameToLayer("DogHole");
        go.layer = holeLayer >= 0 ? holeLayer : 0;

        AddBox(go, size);
        go.AddComponent<DogHole>();

        Select(go);

        if (holeLayer < 0)
        {
            Debug.LogWarning("[关卡编辑器] 项目里没有 DogHole 层，狗洞会挡不住魔女。请先在 Layers 里加一层叫 DogHole。");
        }
        else
        {
            Debug.Log("[关卡编辑器] 放了小狗洞（0.9×0.9）。它自己不破墙，直接摆在场景里就行——" +
                      "狗能穿过，魔女和守卫会被挡住，声音和视线能过。");
        }
    }

    [MenuItem("Tools/关卡编辑器/放钟", false, 12)]
    public static void PlaceBell()
    {
        var go = SpawnPrefab("Bell", "Bell", SceneCenter(), RootOrNull());
        Select(go);
        Debug.Log("[关卡编辑器] 放了钟（敲响会把守卫引过来，魔女不来）。");
    }

    [MenuItem("Tools/关卡编辑器/放闸门", false, 13)]
    public static void PlaceGate()
    {
        var go = SpawnPrefab("Gate", "Gate", SceneCenter(), RootOrNull());

        var gate = go.GetComponent<Gate>();
        if (gate != null)
        {
            var col = go.GetComponent<Collider2D>();
            if (col != null) gate.blockers = new[] { col };
            gate.visual = go.GetComponent<SpriteRenderer>();
            gate.startOpen = true;
        }

        Select(go);
        Debug.Log("[关卡编辑器] 放了闸门（默认开着）。" +
                  "关着的时候挡路、挡声音、挡视线；开着的时候全部穿透。");
    }

    [MenuItem("Tools/关卡编辑器/放开门拉杆", false, 20)]
    public static void PlaceOpenLever()
    {
        PlaceLever(false);
    }

    [MenuItem("Tools/关卡编辑器/放关门拉杆", false, 21)]
    public static void PlaceCloseLever()
    {
        PlaceLever(true);
    }

    private static void PlaceLever(bool closeGates)
    {
        var go = SpawnPrefab("Lever", closeGates ? "Lever_Close" : "Lever_Open", SceneCenter(), RootOrNull());

        var lever = go.GetComponent<Lever>();
        if (lever != null)
        {
            lever.closeGates = closeGates;
            lever.gates = new Gate[0];
        }

        Select(go);
        Debug.Log($"[关卡编辑器] 放了{(closeGates ? "关门" : "开门")}拉杆（只有魔女拉得动）。" +
                  "把要控制的闸门拖进它的 Gates 数组。");
    }

    [MenuItem("Tools/关卡编辑器/放守卫", false, 30)]
    public static void PlaceGuard()
    {
        var go = SpawnPrefab("P_Guard", "Guard", SceneCenter(), RootOrNull());
        Select(go);
        Debug.Log("[关卡编辑器] 放了守卫。巡逻点在它的 Patrol Points 数组里拖场景物体进去。");
    }

    [MenuItem("Tools/关卡编辑器/放狗", false, 31)]
    public static void PlaceDog()
    {
        var go = SpawnPrefab("P_Dog", "Dog", SceneCenter(), RootOrNull());
        Select(go);
    }

    [MenuItem("Tools/关卡编辑器/放魔女", false, 32)]
    public static void PlaceWitch()
    {
        var go = SpawnPrefab("P_Witch", "Witch", SceneCenter(), RootOrNull());
        Select(go);
    }

    [MenuItem("Tools/关卡编辑器/放床", false, 33)]
    public static void PlaceBed()
    {
        var go = SpawnPrefab("Bed", "Bed", SceneCenter(), RootOrNull());
        Select(go);
    }

    [MenuItem("Tools/关卡编辑器/放陶罐", false, 34)]
    public static void PlaceJar()
    {
        var go = SpawnPrefab("Jar", "Jar", SceneCenter(), RootOrNull());
        Select(go);
    }

    [MenuItem("Tools/关卡编辑器/放出口", false, 40)]
    public static void PlaceExit()
    {
        var root = RootOrNull();
        var go = MakeExit(root, SceneCenter(), new Vector2(2f, 3f));
        Select(go);
        Debug.Log("[关卡编辑器] 放了出口。魔女走进去就通关；要切下一关就在 Next Scene 里填场景名。");
    }

    [MenuItem("Tools/关卡编辑器/重烘寻路网格", false, 50)]
    public static void RebakeGrid()
    {
        var grid = Object.FindObjectOfType<LevelGrid>();
        if (grid == null)
        {
            Debug.LogWarning("[关卡编辑器] 场景里没有 LevelGrid。");
            return;
        }

        grid.Bake();

        int blocked = 0;
        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                if (!grid.Walkable(x, y)) blocked++;
            }
        }

        int total = grid.Width * grid.Height;
        Debug.Log($"[关卡编辑器] 网格重烘完成：{grid.Width}×{grid.Height}，阻挡 {blocked}/{total}");
    }

    // ───────────────────────── 内部工具 ─────────────────────────

    private static Transform RootOrNull()
    {
        var root = GameObject.Find(RootName);
        return root != null ? root.transform : null;
    }

    private static Vector3 SceneCenter()
    {
        var view = SceneView.lastActiveSceneView;
        if (view == null) return Vector3.zero;

        var p = view.pivot;
        p.z = 0f;
        return p;
    }

    private static int WallLayer()
    {
        int layer = LayerMask.NameToLayer("Wall");
        return layer >= 0 ? layer : 0;
    }

    private static LayerMask SolidMask()
    {
        int mask = 0;

        int wall = LayerMask.NameToLayer("Wall");
        if (wall >= 0) mask |= 1 << wall;

        int hole = LayerMask.NameToLayer("DogHole");
        if (hole >= 0) mask |= 1 << hole;

        return mask;
    }

    private static GameObject NewSprite(string name, Vector2 size, Color color, int sortingOrder, Transform parent)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = EnsureSquareSprite();
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        return go;
    }

    private static void MakeWall(Transform parent, Vector2 center, Vector2 size)
    {
        var go = NewSprite("Wall", size, new Color(0.34f, 0.30f, 0.25f), 1, parent);
        go.transform.position = center;
        go.layer = WallLayer();

        AddBox(go, size);
    }

    private static GameObject MakeExit(Transform parent, Vector2 pos, Vector2 size)
    {
        var go = NewSprite("Exit", size, new Color(0.35f, 0.85f, 0.45f, 0.55f), 2, parent);
        go.transform.position = pos;

        var box = AddBox(go, size);
        box.isTrigger = true;

        go.AddComponent<ExitZone>();

        return go;
    }

    private static BoxCollider2D AddBox(GameObject go, Vector2 size)
    {
        var box = go.AddComponent<BoxCollider2D>();
        box.size = Vector2.one;   // 物体本身已经缩放到目标尺寸

        return box;
    }

    private static GameObject SpawnPrefab(string prefabName, string name, Vector2 pos, Transform parent)
    {
        string path = $"Assets/Scripts/Game/Prefabs/{prefabName}.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        GameObject go;

        if (prefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, true);
        }
        else
        {
            Debug.LogWarning($"[关卡编辑器] 找不到预制体 {path}，只放了一个空物体。");
            go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
        }

        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = Vector3.one;

        Undo.RegisterCreatedObjectUndo(go, "Place " + name);

        return go;
    }

    private static void Select(GameObject go)
    {
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }

    private static void AddToBuildSettings(string path)
    {
        var scenes = EditorBuildSettings.scenes;

        foreach (var s in scenes)
        {
            if (s.path == path) return;
        }

        var list = new EditorBuildSettingsScene[scenes.Length + 1];
        scenes.CopyTo(list, 0);
        list[list.Length - 1] = new EditorBuildSettingsScene(path, true);

        EditorBuildSettings.scenes = list;
    }

    /// <summary>1 米见方的纯白贴图，所有占位图形都用它染色。</summary>
    private static Sprite EnsureSquareSprite()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (sprite != null) return sprite;

        Directory.CreateDirectory(Path.GetDirectoryName(SpritePath));

        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var px = new Color32[16];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);

        tex.SetPixels32(px);
        tex.Apply();

        File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(SpritePath);

        var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 4f;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }
}
