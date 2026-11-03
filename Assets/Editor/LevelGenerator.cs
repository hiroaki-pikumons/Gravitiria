using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Level1 を雛形（Player / Canvas / EventSystem / Directional Light）にして Level2〜5 を生成する。
/// メニュー Tools > Gravity > Generate Levels、または batchmode の -executeMethod LevelGenerator.GenerateAll。
/// </summary>
public static class LevelGenerator
{
    internal const string TemplateScene = "Assets/Scenes/Level1.unity";
    internal static readonly string[] KeepFromTemplate = { "Player", "Canvas", "EventSystem", "Directional Light" };

    internal static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Color/{name}.mat");
    internal static Material DefaultMat => AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
    internal static GameObject Prefab(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Obstacle/{name}.prefab");

    [MenuItem("Tools/Gravity/Generate Levels")]
    public static void GenerateAll()
    {
        LaserStyler.StylePrefab();
        LocalizeTutorialScene("Tutorial");
        UpdateLevel1();
        BuildLevel2();
        BuildLevel3();
        BuildLevel4();
        BuildLevel5();
        BuildMenuScene("Title", typeof(TitleScreen));
        BuildMenuScene("Ending", typeof(EndingScreen));
        RegisterScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("[LevelGenerator] done");
    }

    // ------------------------------------------------------------------ Tutorial / TutorialNext（既存の字幕に英訳を付ける）
    internal static void LocalizeTutorialScene(string sceneName)
    {
        var scene = EditorSceneManager.OpenScene($"Assets/Scenes/{sceneName}.unity", OpenSceneMode.Single);
        foreach (var trigger in Object.FindObjectsByType<TutorialVoiceTrigger>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(trigger);
            string ja = so.FindProperty("message").stringValue;
            string en;
            if (ja.Contains("WASD")) en = "Move with WASD.\nAnd hold right-click — something interesting happens when you let go.";
            else if (ja.Contains("足場")) en = "The footing ahead has collapsed. Watch your step.";
            else continue;
            so.FindProperty("messageEn").stringValue = en;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        PostFxSetup.ApplyToSceneCameras();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ------------------------------------------------------------------ Level1（既存）
    internal static void UpdateLevel1()
    {
        var scene = EditorSceneManager.OpenScene(TemplateScene, OpenSceneMode.Single);

        ConvertSensorToSubtitle("MessageSensor (2)",
            "この先、赤いレーザーが廊下を塞いでいる。右クリックで時間を遅らせ、照準を向けた面へ重力を移せ。出口は床の裏側だ。",
            "Red lasers block the corridor ahead. Hold right-click to slow time, aim at a surface, and release to move gravity there. The exit is on the underside of the floor.");
        ConvertSensorToSubtitle("MessageSensor (1)",
            "よし、床の裏側に回り込んだな。レーザーの隙間は左右で入れ替わる。白く光る箱が出口だ。",
            "Good, you made it to the underside. The gaps in the laser fences alternate sides. The glowing white box is the exit.");

        // 元からある（プレハブ化されていない）レーザーにも新しい見た目を適用
        var legacyLaser = GameObject.Find("SecurityLaser");
        if (legacyLaser != null && PrefabUtility.GetPrefabInstanceStatus(legacyLaser) == PrefabInstanceStatus.NotAPrefab)
        {
            LaserStyler.Apply(legacyLaser);
        }

        // ブルーム（Level2〜5 は Level1 を雛形にするのでカメラ設定ごと引き継がれる）
        PostFxSetup.ApplyToSceneCameras();

        // 裏側への入口を正面から背面へ移す：正面の壁を閉じ、背面の壁を開けて外の床を有効にする
        SetColliderEnabled("wall (3)", true);
        SetColliderEnabled("wall (1)", false);
        SetColliderEnabled("Floor (6)", true);

        // 裏側ルートのレーザー柵（床の裏から垂れ下がる）。隙間を右→左と互い違いにする
        DestroyAllNamed("UndersideLaser");
        foreach (var laser in Object.FindObjectsByType<SecurityLaser>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            // 床の裏（y<-1）にある生成済みレーザーだけ消す。元からある地上のレーザーは残す
            if (laser.transform.parent == null && laser.transform.position.y < -1f) Object.DestroyImmediate(laser.gameObject);
        }
        LaserFenceX(z: 5f, y: -5.5f, fromX: -9f, toX: 9f, gapFrom: 5f, gapTo: 9.5f, name: "UndersideLaser");
        LaserFenceX(z: 16f, y: -5.5f, fromX: -9f, toX: 9f, gapFrom: -9.5f, gapTo: -5f, name: "UndersideLaser");

        DestroyAllNamed("VoiceSensor");
        Sensor("この部屋の出口は床の裏側だ。入口はスタート地点の後ろ、開いている壁の先にある。",
            "The exit of this room is on the underside of the floor. The way in is behind you, past the open wall.",
            new Vector3(0f, 3f, 0f), new Vector3(18f, 8f, 6f));

        foreach (var door in Object.FindObjectsByType<GoalDoor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            door.nextSceneName = "Level2";
            door.clearMessage = "";
            EditorUtility.SetDirty(door);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    internal static void ConvertSensorToSubtitle(string objectName, string message, string messageEn)
    {
        var go = GameObject.Find(objectName);
        if (go == null) { Debug.LogWarning($"[LevelGenerator] {objectName} が見つかりません"); return; }
        var old = go.GetComponent<MessageTrigger>();
        if (old != null) Object.DestroyImmediate(old);
        var trigger = go.GetComponent<TutorialVoiceTrigger>() ?? go.AddComponent<TutorialVoiceTrigger>();
        SetMessage(trigger, message, messageEn);
        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.enabled = false;
    }

    internal static void SetColliderEnabled(string objectName, bool enabled)
    {
        var go = GameObject.Find(objectName);
        if (go == null) { Debug.LogWarning($"[LevelGenerator] {objectName} が見つかりません"); return; }
        var col = go.GetComponent<Collider>();
        if (col != null) col.enabled = enabled;
    }

    internal static void SetMessage(TutorialVoiceTrigger trigger, string message, string messageEn)
    {
        var so = new SerializedObject(trigger);
        so.FindProperty("message").stringValue = message;
        so.FindProperty("messageEn").stringValue = messageEn;
        so.FindProperty("playOnlyOnce").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------------ 共通部品
    internal static Scene OpenTemplate()
    {
        var scene = EditorSceneManager.OpenScene(TemplateScene, OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
        {
            if (!KeepFromTemplate.Contains(root.name)) Object.DestroyImmediate(root);
        }
        var player = GameObject.Find("Player");
        player.transform.SetPositionAndRotation(new Vector3(0f, 3f, 0f), Quaternion.identity);
        return scene;
    }

    internal static void Save(Scene scene, string name)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{name}.unity");
        Debug.Log($"[LevelGenerator] saved {name}");
    }

    internal static GameObject Box(string name, Vector3 center, Vector3 size, Material mat = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = center;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat != null ? mat : DefaultMat;
        return go;
    }

    internal static GameObject Inst(string prefab, Vector3 pos, Vector3 euler, string name = null)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(Prefab(prefab));
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
        if (name != null) go.name = name;
        return go;
    }

    internal static void Laser(Vector3 pos, Vector3 euler, string name = null) => Inst("SecurityLaser", pos, euler, name);

    internal static void LaserFenceX(float z, float y, float fromX, float toX, float gapFrom, float gapTo, string name = null)
    {
        // 縦向きレーザーを 1.5 間隔で並べ、[gapFrom, gapTo] の区間だけ空ける
        for (float x = fromX; x <= toX + 0.01f; x += 1.5f)
        {
            if (x > gapFrom - 0.75f && x < gapTo + 0.75f) continue;
            Laser(new Vector3(x, y, z), Vector3.zero, name);
        }
    }

    internal static void DestroyAllNamed(string name)
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (go != null && go.name == name && go.transform.parent == null) Object.DestroyImmediate(go);
        }
    }

    internal static void Sensor(string message, string messageEn, Vector3 center, Vector3 size)
    {
        var go = Inst("MessageSensor", center, Vector3.zero, "VoiceSensor");
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().enabled = false;
        SetMessage(go.GetComponent<TutorialVoiceTrigger>(), message, messageEn);
    }

    internal static void Goal(Vector3 pos, string next, string clearMessage = "", string clearMessageEn = "")
    {
        var go = Inst("WhiteGoal", pos, Vector3.zero);
        foreach (var door in go.GetComponentsInChildren<GoalDoor>())
        {
            door.nextSceneName = next;
            door.clearMessage = clearMessage;
            door.clearMessageEn = clearMessageEn;
            door.clearMessageHold = 2.5f;
        }
    }

    internal static void MovingBlock(Vector3 pos, float normalSpeed, float fastSpeed)
    {
        var go = Inst("movingblock", pos, Vector3.zero);
        var enemy = go.GetComponent<TimeDefyingEnemy>();
        enemy.normalSpeed = normalSpeed;
        enemy.fastSpeed = fastSpeed;
    }

    internal static void Stasis(Vector3 center, Vector3 size, float slowFactor = 0.45f)
    {
        var go = Inst("StasisZone", center, Vector3.zero);
        go.transform.localScale = size;
        go.GetComponent<StasisZone>().slowFactor = slowFactor;
    }

    internal static void PointLight(Vector3 pos, float range, float intensity, Color color)
    {
        var go = new GameObject("Point Light");
        go.transform.position = pos;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = range;
        light.intensity = intensity;
        light.color = color;
    }

    internal static readonly Color Warm = new Color(1f, 0.85f, 0.6f);
    internal static readonly Color Cold = new Color(0.6f, 0.8f, 1f);

    // ------------------------------------------------------------------ Level2：壁を歩け
    internal static void BuildLevel2()
    {
        var scene = OpenTemplate();

        Box("Floor", new Vector3(0f, 0f, 6f), new Vector3(14f, 1f, 24f));
        Box("Guide", new Vector3(0f, 0.51f, 16f), new Vector3(2f, 0.05f, 4f), Mat("Emission_Mat"));
        Box("Wall", new Vector3(10f, 12f, 42.5f), new Vector3(1f, 34f, 65f));
        Box("Ceiling", new Vector3(0f, 30f, 52.5f), new Vector3(22f, 1f, 45f));
        Box("CeilingGuide", new Vector3(0f, 29.49f, 70f), new Vector3(4f, 0.05f, 4f), Mat("Emission_Mat"));

        // 壁の通り道を塞ぐレーザー（天井へ逃げるしかない）
        foreach (float y in new[] { -1f, 9f, 19f, 29f }) Laser(new Vector3(9f, y, 40f), Vector3.zero);

        Goal(new Vector3(0f, 27.5f, 70f), "Level3");

        Sensor("右の壁を見ろ。右クリックで狙いを定め、離せば重力はその面へ移る。壁が床になる。",
            "Look at the wall on your right. Aim with right-click and release — gravity moves to that surface. The wall becomes your floor.",
            new Vector3(0f, 4f, 6f), new Vector3(14f, 8f, 12f));
        Sensor("前方、赤いレーザーが壁を塞いでいる。頭上の天井へ重力を移して回り込め。出口は天井の裏だ。",
            "Red lasers block the wall ahead. Move gravity to the ceiling above and go around. The exit is on the underside of the ceiling.",
            new Vector3(8f, 12f, 27f), new Vector3(4f, 34f, 14f));

        PointLight(new Vector3(0f, 6f, 16f), 25f, 1.2f, Cold);
        PointLight(new Vector3(6f, 12f, 30f), 30f, 1.2f, Warm);
        PointLight(new Vector3(0f, 26f, 68f), 30f, 1.5f, Cold);

        Save(scene, "Level2");
    }

    // ------------------------------------------------------------------ Level3：時間の番人
    internal static void BuildLevel3()
    {
        var scene = OpenTemplate();

        Box("Floor", new Vector3(0f, 0f, 35f), new Vector3(16f, 1f, 80f));
        Box("WallL", new Vector3(-8.5f, 2.5f, 35f), new Vector3(1f, 6f, 80f));
        Box("WallR", new Vector3(8.5f, 2.5f, 35f), new Vector3(1f, 6f, 80f));

        // 第1区間：縦レーザーの柵、右端だけ開いている
        MovingBlock(new Vector3(-3f, 1.5f, 8f), 3f, 25f);
        LaserFenceX(z: 14f, y: 5f, fromX: -7.5f, toX: 7.5f, gapFrom: 5f, gapTo: 8f);

        // 第2区間：膝の高さの横レーザー。手前の跳躍パッドで跳び越える
        Inst("JumpPad", new Vector3(0f, 0.15f, 27f), Vector3.zero);
        Box("PadGuide", new Vector3(0f, 0.52f, 27f), new Vector3(3.2f, 0.05f, 3.2f), Mat("Emission_Mat"));
        Laser(new Vector3(-4.5f, 2.3f, 30.5f), new Vector3(0f, 0f, 90f));
        Laser(new Vector3(4.5f, 2.3f, 30.5f), new Vector3(0f, 0f, 90f));
        MovingBlock(new Vector3(4f, 1.5f, 22f), 3f, 25f);
        MovingBlock(new Vector3(-4f, 1.5f, 37f), 3f, 25f);

        // 第3区間：停滞フィールドの中を箱が漂い、左端だけ開いた柵
        Stasis(new Vector3(0f, 3f, 50f), new Vector3(16f, 6f, 12f));
        MovingBlock(new Vector3(2f, 1.5f, 47f), 4f, 30f);
        MovingBlock(new Vector3(-2f, 1.5f, 53f), 4f, 30f);
        LaserFenceX(z: 62f, y: 5f, fromX: -7.5f, toX: 7.5f, gapFrom: -8f, gapTo: -5f);

        Goal(new Vector3(0f, 2.5f, 72f), "Level4");

        Sensor("廊下に動く箱がいる。奴らは時間に逆らう──スローをかけると逆に速くなる。ここではスローを封印しろ。",
            "Moving blocks patrol this corridor. They defy time — slow-motion makes them faster. Don't use slow-mo here.",
            new Vector3(0f, 3f, 2f), new Vector3(16f, 6f, 6f));
        Sensor("正面の横レーザーは、手前の光る板を踏めば跳び越えられる。",
            "Step on the glowing pad to jump over the horizontal laser ahead.",
            new Vector3(0f, 3f, 22.5f), new Vector3(16f, 6f, 4f));
        Sensor("青い霧は停滞フィールド。中では箱も鈍る。落ち着いて抜けろ。出口は左端だ。",
            "The blue haze is a stasis field. Everything inside slows down, blocks included. Take it easy. The gap is on the far left.",
            new Vector3(0f, 3f, 42f), new Vector3(16f, 6f, 4f));

        PointLight(new Vector3(0f, 5f, 14f), 25f, 1.2f, Warm);
        PointLight(new Vector3(0f, 5f, 30f), 25f, 1.2f, Warm);
        PointLight(new Vector3(0f, 5f, 50f), 25f, 1.2f, Cold);
        PointLight(new Vector3(0f, 5f, 70f), 25f, 1.5f, Cold);

        Save(scene, "Level3");
    }

    // ------------------------------------------------------------------ Level4：塔
    internal static void BuildLevel4()
    {
        var scene = OpenTemplate();

        Box("Base", new Vector3(0f, 0f, 0f), new Vector3(16f, 1f, 16f));
        Box("WallN", new Vector3(0f, 35f, 8.5f), new Vector3(16f, 70f, 1f));
        Box("WallS", new Vector3(0f, 35f, -8.5f), new Vector3(16f, 70f, 1f));
        Box("WallE", new Vector3(8.5f, 35f, 0f), new Vector3(1f, 70f, 16f));
        Box("WallW", new Vector3(-8.5f, 35f, 0f), new Vector3(1f, 70f, 16f));
        Box("Cap", new Vector3(0f, 70.5f, 0f), new Vector3(18f, 1f, 18f));
        Box("CapGuide", new Vector3(0f, 69.99f, 0f), new Vector3(4f, 0.05f, 4f), Mat("Emission_Mat"));

        // 高さごとに塞がれる壁が変わる：20 → 北南、35 → 東西、50 → 北南、62 → 東西
        foreach (float y in new[] { 20f, 50f })
        {
            foreach (float x in new[] { -4.5f, 4.5f })
            {
                Laser(new Vector3(x, y, 7f), new Vector3(0f, 0f, 90f));
                Laser(new Vector3(x, y, -7f), new Vector3(0f, 0f, 90f));
            }
        }
        foreach (float y in new[] { 35f, 62f })
        {
            foreach (float z in new[] { -4.5f, 4.5f })
            {
                Laser(new Vector3(7f, y, z), new Vector3(90f, 0f, 0f));
                Laser(new Vector3(-7f, y, z), new Vector3(90f, 0f, 0f));
            }
        }

        // 天井一面のレーザー：下から天井へ直接反転しても抜けられない。壁のスイッチで解除する
        var ceilingLasers = new List<GameObject>();
        for (float z = -9f; z <= 9.01f; z += 1.5f)
        {
            foreach (float x in new[] { -4.5f, 4.5f })
            {
                ceilingLasers.Add(Inst("SecurityLaser", new Vector3(x, 69f, z), new Vector3(0f, 0f, 90f), "CeilingLaser"));
            }
        }
        var laserSwitch = Box("LaserSwitch", new Vector3(0f, 61f, 7.6f), new Vector3(4f, 2f, 0.6f), Mat("Emission_Mat"));
        laserSwitch.GetComponent<Collider>().isTrigger = true;
        var sw = laserSwitch.AddComponent<LaserSwitch>();
        sw.lasers = ceilingLasers.ToArray();
        sw.message = "天井のレーザーを解除した。頂上へ上がれ。";
        sw.messageEn = "Ceiling lasers disabled. Head for the top.";

        Goal(new Vector3(0f, 67.5f, 0f), "Level5");

        Sensor("ここは縦穴だ。壁へ重力を移せば、壁は登れる。レーザーの帯に行き当たったら、別の壁へ乗り換えろ。頂上はレーザーで覆われている──壁のどこかにある光るスイッチで解除しろ。",
            "This is a shaft. Move gravity onto a wall and you can climb it. When a laser band blocks you, switch to another wall. The top is covered in lasers — find the glowing switch on a wall to disable them.",
            new Vector3(0f, 4f, 0f), new Vector3(14f, 8f, 14f));
        Sensor("半分まで来た。帯は壁ごとに高さが違う。焦らず、空いている壁を選べ。スイッチは北の壁の上の方だ。",
            "Halfway there. The bands sit at different heights on each wall. Take your time and pick the open one. The switch is high on the north wall.",
            new Vector3(0f, 40f, 0f), new Vector3(14f, 6f, 14f));

        PointLight(new Vector3(0f, 12f, 0f), 30f, 1.3f, Warm);
        PointLight(new Vector3(0f, 32f, 0f), 30f, 1.3f, Cold);
        PointLight(new Vector3(0f, 52f, 0f), 30f, 1.3f, Warm);
        PointLight(new Vector3(0f, 66f, 0f), 30f, 1.6f, Cold);

        Save(scene, "Level4");
    }

    // ------------------------------------------------------------------ Level5：最終試験
    internal static void BuildLevel5()
    {
        var scene = OpenTemplate();

        // A：動く箱と停滞フィールドの廊下
        Box("Floor", new Vector3(0f, 0f, 20f), new Vector3(16f, 1f, 50f));
        Box("WallL", new Vector3(-8.5f, 2.5f, 20f), new Vector3(1f, 6f, 50f));
        Box("WallR", new Vector3(8.5f, 2.5f, 20f), new Vector3(1f, 6f, 50f));
        MovingBlock(new Vector3(-4f, 1.5f, 10f), 2.5f, 25f);
        MovingBlock(new Vector3(4f, 1.5f, 20f), 2.5f, 25f);
        Stasis(new Vector3(0f, 3f, 32f), new Vector3(16f, 6f, 10f));
        MovingBlock(new Vector3(0f, 1.5f, 32f), 4f, 30f);

        // B：谷を越えて壁へ、壁はレーザーで塞がれている
        Box("Wall", new Vector3(10f, 12f, 72f), new Vector3(1f, 34f, 66f));
        Box("WallGuide", new Vector3(9.49f, 12f, 55f), new Vector3(0.05f, 4f, 4f), Mat("Emission_Mat"));
        foreach (float y in new[] { -1f, 9f, 19f, 29f }) Laser(new Vector3(9f, y, 70f), Vector3.zero);

        // C：天井。足元の横レーザーは天井の跳躍パッドで跳び越える
        Box("Ceiling", new Vector3(0f, 30f, 82.5f), new Vector3(18f, 1f, 65f));
        Inst("JumpPad", new Vector3(0f, 29.85f, 92f), new Vector3(180f, 0f, 0f));
        Box("PadGuide", new Vector3(0f, 29.48f, 92f), new Vector3(3.2f, 0.05f, 3.2f), Mat("Emission_Mat"));
        Laser(new Vector3(-4.5f, 28.8f, 96f), new Vector3(0f, 0f, 90f));
        Laser(new Vector3(4.5f, 28.8f, 96f), new Vector3(0f, 0f, 90f));
        Box("CeilingGuide", new Vector3(0f, 29.49f, 110f), new Vector3(4f, 0.05f, 4f), Mat("Emission_Mat"));

        Goal(new Vector3(0f, 27.5f, 110f), "R1");

        Sensor("最終試験だ。これまでの全てを使え。箱、霧、壁、天井──そして跳べ。",
            "Final exam. Use everything you've learned: blocks, haze, walls, ceiling — and jump.",
            new Vector3(0f, 3f, 2f), new Vector3(16f, 6f, 6f));
        Sensor("廊下はここで終わりだ。右の壁へ重力を移せ。",
            "The corridor ends here. Move gravity to the wall on the right.",
            new Vector3(0f, 3f, 42f), new Vector3(16f, 6f, 5f));
        Sensor("壁の先はレーザーで塞がれている。天井へ上がれ。",
            "Lasers block the wall ahead. Go up to the ceiling.",
            new Vector3(8f, 12f, 55f), new Vector3(4f, 34f, 10f));
        Sensor("足元の光る板を踏めば跳べる。横のレーザーを飛び越えろ。出口はすぐそこだ。",
            "Step on the glowing pad under your feet to jump. Clear the horizontal laser. The exit is right there.",
            new Vector3(0f, 27f, 84f), new Vector3(18f, 6f, 8f));

        PointLight(new Vector3(0f, 5f, 10f), 25f, 1.2f, Warm);
        PointLight(new Vector3(0f, 5f, 32f), 25f, 1.2f, Cold);
        PointLight(new Vector3(6f, 12f, 58f), 30f, 1.2f, Warm);
        PointLight(new Vector3(0f, 26f, 92f), 30f, 1.4f, Warm);
        PointLight(new Vector3(0f, 26f, 110f), 30f, 1.6f, Cold);

        Save(scene, "Level5");
    }

    // ------------------------------------------------------------------ Title / Ending
    internal static void BuildMenuScene(string name, System.Type screenType)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.03f, 0.06f);
        cam.fieldOfView = 50f;
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = Vector3.zero;
        PostFxSetup.Apply(cam);

        // 背景：画面右奥でゆっくり回るゴールの箱と、光る輪
        var goal = Inst("WhiteGoal", new Vector3(6.5f, 0.5f, 18f), Vector3.zero, "Showpiece");
        PrefabUtility.UnpackPrefabInstance(goal, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        foreach (var door in goal.GetComponentsInChildren<GoalDoor>()) Object.DestroyImmediate(door);
        foreach (var col in goal.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
        for (int i = 0; i < 3; i++)
        {
            var ring = Box($"Ring{i}", new Vector3(6.5f, 0.5f, 18f), new Vector3(9f + i * 3f, 0.08f, 0.08f), Mat("Emission_Mat"));
            ring.AddComponent<SimpleRotator>().rotationSpeed = new Vector3(5f + i * 4f, 11f - i * 3f, 2f);
            Object.DestroyImmediate(ring.GetComponent<Collider>());
        }
        var floor = Box("Floor", new Vector3(0f, -14f, 30f), new Vector3(80f, 1f, 80f));
        Object.DestroyImmediate(floor.GetComponent<Collider>());

        PointLight(new Vector3(2f, 2f, 10f), 30f, 0.9f, Cold);
        var sun = new GameObject("Directional Light").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 0.25f;
        sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

        var screen = new GameObject(name + "Screen");
        screen.AddComponent(screenType);

        EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{name}.unity");
        Debug.Log($"[LevelGenerator] saved {name}");
    }

    // ------------------------------------------------------------------ Build Settings
    internal static void RegisterScenes()
    {
        var wanted = new[] { "Title", "Tutorial", "Level1", "Level2", "Level3", "Level4", "Level5", "R1", "R2", "R3", "R4", "R5", "Ending" };
        var list = new List<EditorBuildSettingsScene>();
        foreach (var name in wanted)
        {
            string path = $"Assets/Scenes/{name}.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) { Debug.LogWarning($"[LevelGenerator] missing scene {path}"); continue; }
            list.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = list.ToArray();
    }
}
