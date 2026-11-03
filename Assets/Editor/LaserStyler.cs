using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// SecurityLaser の見た目：
///   極細の白熱コア ＋ 柔らかく縁が溶ける赤いグロー ＋ 淡いハロー（いずれも Gravity/LaserBeam シェーダー、HDR でブルームに乗る）
///   両端にスリムな発射口（暗い金属のシリンダー ＋ 赤く光るリング）
/// 当たり判定（CapsuleCollider / DangerZone）は触らない。
/// </summary>
public static class LaserStyler
{
    private const string PrefabPath = "Assets/Obstacle/SecurityLaser.prefab";
    private const string BeamShader = "Gravity/LaserBeam";
    private const string CoreMatPath = "Assets/Color/Laser_Core_Mat.mat";
    private const string GlowMatPath = "Assets/Color/Laser_Glow_Mat.mat";
    private const string HaloMatPath = "Assets/Color/Laser_Halo_Mat.mat";
    private const string EmitterMatPath = "Assets/Color/Laser_Emitter_Mat.mat";
    private const string RingMatPath = "Assets/Color/Laser_Ring_Mat.mat";

    // 子のスケールはプレハブ親（x,z = 0.5、y = 5）に対する相対値。コアの世界半径 = 0.25 * 0.22 ≒ 0.055
    private static readonly Vector3 CoreScale = new Vector3(0.22f, 1.0f, 0.22f);
    private static readonly Vector3 GlowScale = new Vector3(0.7f, 1.0f, 0.7f);
    private static readonly Vector3 HaloScale = new Vector3(1.6f, 1.0f, 1.6f);
    private static readonly Vector3 HousingScale = new Vector3(1.4f, 0.045f, 1.4f);   // 世界: 半径 0.35、高さ 0.45
    private static readonly Vector3 RingScale = new Vector3(1.5f, 0.006f, 1.5f);

    private static readonly string[] OldChildren = { "Spark", "EmitterA", "EmitterB", "LensA", "LensB" };

    [MenuItem("Tools/Gravity/Style Lasers")]
    public static void StylePrefab()
    {
        EnsureMaterials();
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        Apply(root);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
        Debug.Log("[LaserStyler] prefab styled");
    }

    public static void Apply(GameObject root)
    {
        EnsureMaterials();

        var rootRenderer = root.GetComponent<MeshRenderer>();
        if (rootRenderer != null) rootRenderer.enabled = false;

        foreach (var name in OldChildren)
        {
            var old = root.transform.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        var core = Layer(root, "Core", PrimitiveType.Capsule, Vector3.zero, CoreScale, CoreMatPath);
        var glow = Layer(root, "Glow", PrimitiveType.Capsule, Vector3.zero, GlowScale, GlowMatPath);
        var halo = Layer(root, "Halo", PrimitiveType.Capsule, Vector3.zero, HaloScale, HaloMatPath);

        foreach (var (suffix, sign) in new[] { ("A", -1f), ("B", 1f) })
        {
            Layer(root, "Housing" + suffix, PrimitiveType.Cylinder, new Vector3(0f, sign * 1.0f, 0f), HousingScale, EmitterMatPath);
            Layer(root, "Ring" + suffix, PrimitiveType.Cylinder, new Vector3(0f, sign * 0.975f, 0f), RingScale, RingMatPath);
        }

        var visual = root.GetComponent<LaserBeamVisual>();
        if (visual == null) visual = root.AddComponent<LaserBeamVisual>();
        visual.core = core;
        visual.glow = glow;
        visual.halo = halo;
    }

    private static Renderer Layer(GameObject root, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, string matPath)
    {
        var existing = root.transform.Find(name);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = localScale;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        return r;
    }

    private static void EnsureMaterials()
    {
        Beam(CoreMatPath, new Color(1f, 0.86f, 0.82f, 1f), intensity: 4.5f, power: 0.8f, flow: 0.15f, flowScale: 4f, pulseWidth: 0.05f, queue: 3002);
        Beam(GlowMatPath, new Color(1f, 0.12f, 0.05f, 1f), intensity: 1.4f, power: 2.2f, flow: 0.45f, flowScale: 6f, pulseWidth: 0.08f, queue: 3001);
        Beam(HaloMatPath, new Color(1f, 0.1f, 0.05f, 1f), intensity: 0.35f, power: 3.5f, flow: 0.2f, flowScale: 3f, pulseWidth: 0.1f, queue: 3000);
        Standard(EmitterMatPath, new Color(0.1f, 0.1f, 0.12f), metallic: 0.85f, smoothness: 0.7f, emission: Color.black);
        Standard(RingMatPath, new Color(0.3f, 0.02f, 0.01f), metallic: 0.2f, smoothness: 0.6f, emission: new Color(6f, 0.4f, 0.2f));
    }

    private static void Beam(string path, Color color, float intensity, float power, float flow, float flowScale, float pulseWidth, int queue)
    {
        var shader = Shader.Find(BeamShader);
        if (shader == null) { Debug.LogError($"[LaserStyler] shader not found: {BeamShader}"); return; }
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        mat.SetColor("_Color", color);
        mat.SetFloat("_Intensity", intensity);
        mat.SetFloat("_Power", power);
        mat.SetFloat("_FlowAmount", flow);
        mat.SetFloat("_FlowScale", flowScale);
        mat.SetFloat("_FlowSpeed", 1.5f);
        mat.SetFloat("_PulseWidth", pulseWidth);
        mat.SetFloat("_PulseGain", 2f);
        mat.SetFloat("_PulsePos", -1f);
        mat.renderQueue = queue;
        EditorUtility.SetDirty(mat);
    }

    private static void Standard(string path, Color color, float metallic, float smoothness, Color emission)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = Shader.Find("Standard");
        mat.SetColor("_Color", color);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", smoothness);
        if (emission.maxColorComponent > 0f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetColor("_EmissionColor", emission);
        }
        EditorUtility.SetDirty(mat);
    }
}
