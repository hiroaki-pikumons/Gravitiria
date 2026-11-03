using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LevelGenerator;

/// <summary>
/// Remix ステージ R1〜R5。ヒント文は出さず、色（スイッチと対応レーザーは同色）と床の光るガイドで誘導する。
/// 各ステージの想定解法はメソッド冒頭のコメントと Tools/autopilot_full_run.txt の台本に残す。
/// Tools > Gravity > Generate Remix Levels、または -executeMethod RemixLevelGenerator.GenerateAll。
/// </summary>
public static class RemixLevelGenerator
{
    private static readonly Color Red = new Color(1f, 0.12f, 0.05f);
    private static readonly Color Blue = new Color(0.25f, 0.55f, 1f);
    private static readonly Color Green = new Color(0.2f, 1f, 0.35f);
    private static readonly Color Yellow = new Color(1f, 0.85f, 0.15f);
    private static readonly Color Cyan = new Color(0.1f, 0.9f, 1f);
    private static readonly Color Magenta = new Color(1f, 0.2f, 0.9f);

    [MenuItem("Tools/Gravity/Generate Remix Levels")]
    public static void GenerateAll()
    {
        // 既存ステージ（Hiroaki 版）には手を入れない。Level5 の出口を R1 に繋ぐ変更と、ブルーム用プロファイルの修復だけ。
        PostFxSetup.EnsureProfile();
        PatchLevel5Exit();
        BuildR1();
        BuildR2();
        BuildR3();
        BuildR4();
        BuildR5();
        RegisterRemixScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("[RemixLevelGenerator] done");
    }

    // ------------------------------------------------------------------ 部品
    private static GameObject ColoredLaser(Vector3 pos, Vector3 euler, Color color, string name = "Laser")
    {
        var go = Inst("SecurityLaser", pos, euler, name);
        go.GetComponent<LaserBeamVisual>().groupColor = color;
        return go;
    }

    private static List<GameObject> ColoredFenceX(float z, float y, float fromX, float toX, float gapFrom, float gapTo, Color color, string name = "Laser")
    {
        var list = new List<GameObject>();
        for (float x = fromX; x <= toX + 0.01f; x += 1.5f)
        {
            if (x > gapFrom - 0.75f && x < gapTo + 0.75f) continue;
            list.Add(ColoredLaser(new Vector3(x, y, z), Vector3.zero, color, name));
        }
        return list;
    }

    private static LaserSwitch Button(Vector3 center, Vector3 size, Color color, IEnumerable<GameObject> targets, string name = "Switch")
    {
        var go = Box(name, center, size, Mat("Emission_Mat"));
        go.GetComponent<Collider>().isTrigger = true;
        var sw = go.AddComponent<LaserSwitch>();
        sw.groupColor = color;
        sw.message = "";
        sw.messageEn = "";
        sw.lasers = new List<GameObject>(targets).ToArray();
        return sw;
    }

    /// <summary>床や壁に貼る光るガイド帯。ヒント文の代わりに進む方向を示す。</summary>
    private static void Guide(Vector3 center, Vector3 size, Color color, string name = "Guide")
    {
        var go = Box(name, center, size, Mat("Emission_Mat"));
        Object.DestroyImmediate(go.GetComponent<Collider>());
        var r = go.GetComponent<Renderer>();
        var block = new MaterialPropertyBlock();
        block.SetColor("_Color", color * 0.4f);
        block.SetColor("_EmissionColor", color * 1.6f);
        r.SetPropertyBlock(block);
        // プロパティブロックはシーンに保存されないので、専用マテリアルを作って割り当てる
        r.sharedMaterial = GuideMaterial(color);
    }

    private static readonly Dictionary<Color, Material> guideMats = new Dictionary<Color, Material>();
    private static Material GuideMaterial(Color color)
    {
        if (guideMats.TryGetValue(color, out var m) && m != null) return m;
        string key = ColorUtility.ToHtmlStringRGB(color);
        string path = $"Assets/Color/Guide_{key}.mat";
        m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Standard"));
            m.SetColor("_Color", color * 0.4f);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", color * 1.6f);
            AssetDatabase.CreateAsset(m, path);
        }
        guideMats[color] = m;
        return m;
    }

    private static GameObject Enemy(string prefabName, Vector3 pos, Vector3 euler)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Enemies/{prefabName}.prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
        return go;
    }

    private static void Pickup(Progress.Ability ability, Vector3 pos)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Pickups/Pickup_{ability}.prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = pos;
    }

    private static void Room(float width, float length, float height, float zStart, bool closeBack, bool closeFront, float wallBottom = -1f)
    {
        float zc = zStart + length / 2f;
        Box("Floor", new Vector3(0f, 0f, zc), new Vector3(width, 1f, length));
        float wallH = height - wallBottom;
        float wallY = wallBottom + wallH / 2f;
        Box("WallL", new Vector3(-(width / 2f + 0.5f), wallY, zc), new Vector3(1f, wallH, length));
        Box("WallR", new Vector3(width / 2f + 0.5f, wallY, zc), new Vector3(1f, wallH, length));
        if (closeBack) Box("WallBack", new Vector3(0f, wallY, zStart - 0.5f), new Vector3(width + 2f, wallH, 1f));
        if (closeFront) Box("WallFront", new Vector3(0f, wallY, zStart + length + 0.5f), new Vector3(width + 2f, wallH, 1f));
    }

    // ------------------------------------------------------------------ R1 反転と鍵
    // 想定解法: 床のレーザー地帯を右→左→右→左の隙間で抜け、奥の青いボタンを踏む（天井の青レーザーが消える）。
    //           天井を狙って反転し、天井を戻って z=20 の出口へ。ダッシュのピックアップはスタート直後の通路上。
    private static void BuildR1()
    {
        var scene = OpenTemplate();
        Room(20f, 70f, 15f, -5f, closeBack: true, closeFront: true);
        Box("Ceiling", new Vector3(0f, 14f, 30f), new Vector3(20f, 1f, 70f));

        Pickup(Progress.Ability.Dash, new Vector3(0f, 1.6f, 2.5f));

        // 床のレーザー地帯（赤・常時）。隙間は右・左・右・左
        ColoredFenceX(15f, 5f, -9f, 9f, 5f, 9f, Red, "FieldLaser");
        ColoredFenceX(25f, 5f, -9f, 9f, -9f, -5f, Red, "FieldLaser");
        ColoredFenceX(35f, 5f, -9f, 9f, 5f, 9f, Red, "FieldLaser");
        ColoredFenceX(45f, 5f, -9f, 9f, -9f, -5f, Red, "FieldLaser");
        foreach (var (x, z) in new[] { (7f, 15f), (-7f, 25f), (7f, 35f), (-7f, 45f) })
            Guide(new Vector3(x, 0.51f, z), new Vector3(2.5f, 0.05f, 6f), Cyan);

        // 天井の青レーザー（出口の周囲）と、それを止める青いボタン（地帯の奥）
        var ceilingLasers = new List<GameObject>();
        for (float z = 10f; z <= 30.01f; z += 1.5f)
            foreach (float x in new[] { -4.5f, 4.5f })
                ceilingLasers.Add(ColoredLaser(new Vector3(x, 13f, z), new Vector3(0f, 0f, 90f), Blue, "CeilingLaser"));
        Button(new Vector3(0f, 1.0f, 60f), new Vector3(3f, 1f, 3f), Blue, ceilingLasers, "BlueSwitch");
        Guide(new Vector3(0f, 0.51f, 55f), new Vector3(2f, 0.05f, 6f), Blue);
        Guide(new Vector3(0f, 13.49f, 40f), new Vector3(2f, 0.05f, 36f), Blue);   // 天井のガイド：ボタンから出口へ

        Goal(new Vector3(0f, 11.5f, 20f), "R2");

        PointLight(new Vector3(0f, 6f, 8f), 28f, 1.2f, Cold);
        PointLight(new Vector3(0f, 6f, 30f), 28f, 1.0f, Warm);
        PointLight(new Vector3(0f, 6f, 55f), 28f, 1.4f, Blue);
        PointLight(new Vector3(0f, 11f, 20f), 24f, 1.4f, Cold);
        Save(scene, "R1");
    }

    // ------------------------------------------------------------------ R2 裏口
    // 想定解法: 入口はスタートの背後（後ろの縁）。縁から落ちて床の裏を狙って反転し、裏側の赤い柵を右→左の隙間で抜けて出口へ。
    //           クイック重力のピックアップは裏側 1 つ目の隙間の先。
    private static void BuildR2()
    {
        var scene = OpenTemplate();
        Room(16f, 50f, 7f, -5f, closeBack: false, closeFront: true, wallBottom: -6f);
        Guide(new Vector3(0f, 0.51f, -2.5f), new Vector3(3f, 0.05f, 4f), Cyan);        // 後ろの縁へ
        Guide(new Vector3(0f, 0.51f, 2f), new Vector3(1f, 0.05f, 3f), Cyan);

        ColoredFenceX(10f, -5.5f, -7.5f, 7.5f, 4f, 8f, Red, "UnderLaser");
        ColoredFenceX(24f, -5.5f, -7.5f, 7.5f, -8f, -4f, Red, "UnderLaser");
        Guide(new Vector3(6f, -0.51f, 10f), new Vector3(2.5f, 0.05f, 6f), Cyan);
        Guide(new Vector3(-6f, -0.51f, 24f), new Vector3(2.5f, 0.05f, 6f), Cyan);
        Pickup(Progress.Ability.QuickGravity, new Vector3(6f, -2f, 16f));

        Goal(new Vector3(0f, -2.2f, 38f), "R3");

        PointLight(new Vector3(0f, 4f, 0f), 26f, 1.2f, Cold);
        PointLight(new Vector3(0f, -4f, 8f), 26f, 1.2f, Warm);
        PointLight(new Vector3(0f, -4f, 30f), 26f, 1.4f, Cold);
        Save(scene, "R2");
    }

    // ------------------------------------------------------------------ R3 誘導
    // 想定解法: 赤い柵を抜けて奥の黄色いボタンを踏むと、黄色い壁が消えてドローンの巡回が伸び、
    //           低いトンネルの中にある緑のボタン（人は入れない高さ）をドローンが押す → 緑のレーザー門が開く → 出口。
    private static void BuildR3()
    {
        var scene = OpenTemplate();
        Room(20f, 70f, 8f, -5f, closeBack: true, closeFront: true);

        ColoredFenceX(12f, 5f, -9f, 9f, 5f, 9f, Red, "FieldLaser");
        ColoredFenceX(20f, 5f, -9f, 9f, -9f, -5f, Red, "FieldLaser");
        Guide(new Vector3(7f, 0.51f, 12f), new Vector3(2.5f, 0.05f, 6f), Cyan);
        Guide(new Vector3(-7f, 0.51f, 20f), new Vector3(2.5f, 0.05f, 6f), Cyan);

        // ドローンの巡回レーン（z=40）。黄色い壁で手前側に閉じ込められている
        var drone = Enemy("PatrolDrone", new Vector3(-6f, 0.86f, 40f), Vector3.zero);
        drone.GetComponent<PatrolDrone>().patrolDistance = 30f;
        var yellowWall = Box("YellowWall", new Vector3(2f, 1.5f, 40f), new Vector3(0.6f, 3f, 4f), GuideMaterial(Yellow));
        // 緑のボタンは低いトンネルの奥（高さ 1.3：ドローンは通れるがプレイヤーは入れない）
        Box("Tunnel", new Vector3(5.5f, 1.55f, 40f), new Vector3(5f, 0.5f, 4f));
        var greenGate = ColoredFenceX(62f, 5f, -9f, 9f, 99f, 99f, Green, "GateLaser");
        Button(new Vector3(5.5f, 0.75f, 40f), new Vector3(2.4f, 0.5f, 2.4f), Green, greenGate, "GreenSwitch");
        Button(new Vector3(0f, 1.0f, 58f), new Vector3(3f, 1f, 3f), Yellow, new[] { yellowWall }, "YellowSwitch");
        Guide(new Vector3(9.2f, 0.51f, 35f), new Vector3(1.2f, 0.05f, 30f), Yellow);   // 右端の安全な通路
        Guide(new Vector3(0f, 0.51f, 54f), new Vector3(2f, 0.05f, 4f), Yellow);
        Guide(new Vector3(0f, 0.51f, 60f), new Vector3(2f, 0.05f, 3f), Green);

        Goal(new Vector3(0f, 2.5f, 64f), "R4");

        PointLight(new Vector3(0f, 5f, 10f), 26f, 1.2f, Warm);
        PointLight(new Vector3(0f, 5f, 40f), 28f, 1.3f, Yellow);
        PointLight(new Vector3(0f, 5f, 60f), 26f, 1.4f, Green);
        Save(scene, "R3");
    }

    // ------------------------------------------------------------------ R4 点滅
    // 想定解法: 3 列の赤い柵が位相をずらして点滅する。消えた瞬間に走り抜けるか、ダッシュの無敵で抜ける。
    private static void BuildR4()
    {
        var scene = OpenTemplate();
        Room(16f, 70f, 8f, -5f, closeBack: true, closeFront: true);

        float[] zs = { 15f, 30f, 45f };
        float[] phases = { 0f, 0.35f, 0.7f };
        for (int i = 0; i < zs.Length; i++)
        {
            foreach (var laser in ColoredFenceX(zs[i], 5f, -7.5f, 7.5f, 99f, 99f, Red, "BlinkLaser"))
            {
                var b = laser.AddComponent<LaserBlinker>();
                b.period = 2.4f;
                b.onFraction = 0.55f;
                b.phase = phases[i];
            }
            Guide(new Vector3(0f, 0.51f, zs[i] - 3f), new Vector3(1.2f, 0.05f, 2.5f), Cyan);
            Guide(new Vector3(0f, 0.51f, zs[i] - 5.5f), new Vector3(1.2f, 0.05f, 1.2f), Cyan);
        }

        Goal(new Vector3(0f, 2.5f, 62f), "R5");

        PointLight(new Vector3(0f, 5f, 8f), 26f, 1.2f, Cold);
        PointLight(new Vector3(0f, 5f, 30f), 26f, 1.0f, Warm);
        PointLight(new Vector3(0f, 5f, 58f), 26f, 1.4f, Cold);
        Save(scene, "R4");
    }

    // ------------------------------------------------------------------ R5 総合試験
    // 想定解法: 赤い柵を左の隙間で抜け、砲台の射線（中央）を避けて進む。右の壁の高い所にある青いボタンへ
    //           クイック重力（または照準）で壁に乗って押す → 裏側の青い門が開く。床に戻り、スタート背後の縁から落ちて
    //           床の裏へ反転し、裏側の出口へ。磁石の敵は砲台の射線に誘い込むと消える。
    private static void BuildR5()
    {
        var scene = OpenTemplate();
        Room(16f, 65f, 7f, -5f, closeBack: false, closeFront: true, wallBottom: -7f);
        Guide(new Vector3(0f, 0.51f, -2.5f), new Vector3(3f, 0.05f, 4f), Cyan);

        ColoredFenceX(8f, 5f, -7.5f, 7.5f, -8f, -4f, Red, "FieldLaser");
        Guide(new Vector3(-6f, 0.51f, 8f), new Vector3(2.5f, 0.05f, 6f), Cyan);

        var turret = Enemy("LaserTurret", new Vector3(0f, 0.5f, 40f), new Vector3(0f, 180f, 0f));
        turret.GetComponent<LaserTurret>().interval = 2f;
        Enemy("Magnet", new Vector3(0f, 1.5f, 30f), Vector3.zero).GetComponent<Magnet>().range = 9f;   // 砲台の射線上：弾で消える見本
        Enemy("PatrolDrone", new Vector3(5f, 0.86f, 14f), Vector3.zero).GetComponent<PatrolDrone>().patrolDistance = 30f;

        var underGate = ColoredFenceX(30f, -5.5f, -7.5f, 7.5f, 99f, 99f, Blue, "UnderGate");
        // 右の壁（x=8）の高さ 5.5 にある青いボタン
        var sw = Button(new Vector3(8f, 5.5f, 20f), new Vector3(0.6f, 2f, 3f), Blue, underGate, "BlueSwitch");
        Guide(new Vector3(7.99f, 3f, 20f), new Vector3(0.05f, 3f, 1f), Blue);      // 壁の縦ガイド
        Guide(new Vector3(6f, 0.51f, 20f), new Vector3(3f, 0.05f, 1f), Blue);
        Guide(new Vector3(0f, -0.51f, 36f), new Vector3(1.2f, 0.05f, 8f), Blue);    // 裏側：門から出口へ

        Goal(new Vector3(0f, -2.2f, 45f), "Ending", "全ステージクリア。見事だ。", "All stages clear. Well done.");

        PointLight(new Vector3(0f, 4f, 5f), 26f, 1.2f, Cold);
        PointLight(new Vector3(0f, 4f, 30f), 26f, 1.0f, Warm);
        PointLight(new Vector3(6f, 5f, 20f), 22f, 1.3f, Blue);
        PointLight(new Vector3(0f, -4f, 40f), 26f, 1.4f, Cold);
        Save(scene, "R5");
    }

    private static void PatchLevel5Exit()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level5.unity", OpenSceneMode.Single);
        foreach (var door in Object.FindObjectsByType<GoalDoor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            door.nextSceneName = "R1";
            door.clearMessage = "";
            door.clearMessageEn = "";
            EditorUtility.SetDirty(door);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void RegisterRemixScenes()
    {
        var wanted = new[] { "Title", "Tutorial", "Level1", "Level2", "Level3", "Level4", "Level5", "R1", "R2", "R3", "R4", "R5", "Ending" };
        var list = new List<EditorBuildSettingsScene>();
        foreach (var name in wanted)
        {
            string path = $"Assets/Scenes/{name}.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) { Debug.LogWarning($"[RemixLevelGenerator] missing {path}"); continue; }
            list.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = list.ToArray();
    }
}
