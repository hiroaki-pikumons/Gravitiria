using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 敵プレハブ（PatrolDrone / Magnet / LaserTurret / LaserBolt）とそのマテリアルをコードから生成する。
/// Tools > Gravity > Build Enemy Prefabs。生成先は Assets/Enemies/。
/// </summary>
public static class EnemyPrefabBuilder
{
    private const string Dir = "Assets/Enemies";
    private const string BodyMat = Dir + "/Enemy_Body_Mat.mat";
    private const string OrangeMat = Dir + "/Enemy_Orange_Mat.mat";
    private const string RedMat = Dir + "/Enemy_Red_Mat.mat";
    private const string BoltMat = "Assets/Resources/Enemies/LaserBolt_Mat.mat";

    [MenuItem("Tools/Gravity/Build Enemy Prefabs")]
    public static void BuildAll()
    {
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets", "Enemies");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Enemies")) AssetDatabase.CreateFolder("Assets/Resources", "Enemies");
        EnsureMaterials();

        var bolt = BuildBolt();
        BuildDrone();
        BuildMagnet();
        BuildTurret(bolt);
        AssetDatabase.SaveAssets();
        Debug.Log("[EnemyPrefabBuilder] done");
    }

    private static GameObject BuildBolt()
    {
        var root = new GameObject("LaserBolt");
        var rb = root.AddComponent<Rigidbody>();
        rb.useGravity = false;
        var col = root.AddComponent<CapsuleCollider>();
        col.isTrigger = true;
        col.direction = 2;              // Z 軸方向
        col.radius = 0.18f;
        col.height = 1.2f;
        root.AddComponent<LaserBolt>();

        var visual = Primitive(PrimitiveType.Capsule, "Visual", root.transform, Vector3.zero, Quaternion.Euler(90f, 0f, 0f), new Vector3(0.28f, 0.5f, 0.28f), BoltMat);
        var glow = Primitive(PrimitiveType.Capsule, "Glow", root.transform, Vector3.zero, Quaternion.Euler(90f, 0f, 0f), new Vector3(0.55f, 0.55f, 0.55f), "Assets/Color/Laser_Glow_Mat.mat");
        return Save(root, Dir + "/LaserBolt.prefab");
    }

    private static void BuildDrone()
    {
        var root = new GameObject("PatrolDrone");
        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 2f;
        var col = root.AddComponent<BoxCollider>();
        col.size = new Vector3(1.3f, 0.7f, 1.3f);
        var drone = root.AddComponent<PatrolDrone>();
        drone.speed = 3f;
        drone.patrolDistance = 6f;

        Primitive(PrimitiveType.Cube, "Body", root.transform, Vector3.zero, Quaternion.identity, new Vector3(1.2f, 0.5f, 1.2f), BodyMat);
        Primitive(PrimitiveType.Cube, "Stripe", root.transform, new Vector3(0f, 0.26f, 0f), Quaternion.identity, new Vector3(1.25f, 0.06f, 0.3f), OrangeMat);
        Primitive(PrimitiveType.Cube, "StripeZ", root.transform, new Vector3(0f, 0.26f, 0f), Quaternion.identity, new Vector3(0.3f, 0.06f, 1.25f), OrangeMat);
        foreach (var (x, z) in new[] { (0.55f, 0.55f), (-0.55f, 0.55f), (0.55f, -0.55f), (-0.55f, -0.55f) })
        {
            Primitive(PrimitiveType.Cylinder, "Rotor", root.transform, new Vector3(x, 0.3f, z), Quaternion.identity, new Vector3(0.45f, 0.02f, 0.45f), OrangeMat);
        }
        Save(root, Dir + "/PatrolDrone.prefab");
    }

    private static void BuildMagnet()
    {
        var root = new GameObject("Magnet");
        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 1.5f;
        var col = root.AddComponent<SphereCollider>();
        col.radius = 0.6f;
        var magnet = root.AddComponent<Magnet>();
        magnet.speed = 2.5f;
        magnet.range = 14f;

        Primitive(PrimitiveType.Sphere, "Shell", root.transform, Vector3.zero, Quaternion.identity, Vector3.one * 1.1f, BodyMat);
        Primitive(PrimitiveType.Sphere, "Core", root.transform, Vector3.zero, Quaternion.identity, Vector3.one * 0.7f, RedMat);
        Primitive(PrimitiveType.Cube, "RingX", root.transform, Vector3.zero, Quaternion.identity, new Vector3(1.4f, 0.08f, 0.08f), RedMat);
        Primitive(PrimitiveType.Cube, "RingZ", root.transform, Vector3.zero, Quaternion.identity, new Vector3(0.08f, 0.08f, 1.4f), RedMat);
        Save(root, Dir + "/Magnet.prefab");
    }

    private static void BuildTurret(GameObject boltPrefab)
    {
        var root = new GameObject("LaserTurret");
        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        var col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.5f, 0f);
        col.size = new Vector3(1.2f, 1.0f, 1.2f);
        var turret = root.AddComponent<LaserTurret>();
        turret.boltPrefab = boltPrefab;

        Primitive(PrimitiveType.Cylinder, "Base", root.transform, new Vector3(0f, 0.2f, 0f), Quaternion.identity, new Vector3(1.2f, 0.2f, 1.2f), BodyMat);
        Primitive(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0f, 0.7f, 0f), Quaternion.identity, Vector3.one * 0.8f, BodyMat);
        var barrel = Primitive(PrimitiveType.Cube, "Barrel", root.transform, new Vector3(0f, 0.7f, 0.55f), Quaternion.identity, new Vector3(0.28f, 0.28f, 0.9f), BodyMat);
        Primitive(PrimitiveType.Cube, "Lens", root.transform, new Vector3(0f, 0.7f, 1.0f), Quaternion.identity, new Vector3(0.22f, 0.22f, 0.05f), RedMat);
        var muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(root.transform, false);
        muzzle.localPosition = new Vector3(0f, 0.7f, 1.05f);
        turret.muzzle = muzzle;
        Save(root, Dir + "/LaserTurret.prefab");
    }

    private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, string matPath)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = scale;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        r.shadowCastingMode = ShadowCastingMode.Off;
        return go;
    }

    private static GameObject Save(GameObject root, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[EnemyPrefabBuilder] saved {path}");
        return prefab;
    }

    private static void EnsureMaterials()
    {
        Standard(BodyMat, new Color(0.14f, 0.15f, 0.18f), 0.8f, 0.6f, Color.black);
        Standard(OrangeMat, new Color(0.3f, 0.12f, 0.02f), 0.2f, 0.5f, new Color(4f, 1.6f, 0.3f));
        Standard(RedMat, new Color(0.3f, 0.02f, 0.02f), 0.2f, 0.5f, new Color(5f, 0.3f, 0.2f));
        Standard(BoltMat, new Color(1f, 0.5f, 0.4f), 0f, 0.5f, new Color(8f, 2.5f, 1.5f));
    }

    private static void Standard(string path, Color color, float metallic, float smoothness, Color emission)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }
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
