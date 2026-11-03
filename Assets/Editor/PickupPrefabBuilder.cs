using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>能力解放ピックアップのプレハブを生成する。Tools > Gravity > Build Pickup Prefabs。</summary>
public static class PickupPrefabBuilder
{
    private const string Dir = "Assets/Pickups";

    [MenuItem("Tools/Gravity/Build Pickup Prefabs")]
    public static void BuildAll()
    {
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets", "Pickups");
        Build(Progress.Ability.Dash, new Color(0.1f, 0.9f, 1f), new Color(1f, 8f, 10f));
        Build(Progress.Ability.QuickGravity, new Color(1f, 0.2f, 0.9f), new Color(9f, 1.5f, 8f));
        AssetDatabase.SaveAssets();
        Debug.Log("[PickupPrefabBuilder] done");
    }

    private static void Build(Progress.Ability ability, Color color, Color emission)
    {
        string matPath = $"{Dir}/Pickup_{ability}_Mat.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.SetColor("_Color", color);
        mat.SetFloat("_Metallic", 0.3f);
        mat.SetFloat("_Glossiness", 0.7f);
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mat.SetColor("_EmissionColor", emission);
        EditorUtility.SetDirty(mat);

        var root = new GameObject($"Pickup_{ability}");
        var col = root.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.9f;
        root.AddComponent<UnlockPickup>().ability = ability;
        var rotator = root.AddComponent<SimpleRotator>();
        rotator.rotationSpeed = new Vector3(40f, 90f, 25f);

        // ダッシュ: 矢印状の細長い結晶 / クイック重力: 十字
        if (ability == Progress.Ability.Dash)
        {
            Primitive(PrimitiveType.Cube, root.transform, Vector3.zero, Quaternion.Euler(0f, 0f, 45f), new Vector3(0.9f, 0.25f, 0.25f), mat);
            Primitive(PrimitiveType.Cube, root.transform, Vector3.zero, Quaternion.Euler(0f, 0f, -45f), new Vector3(0.9f, 0.25f, 0.25f), mat);
        }
        else
        {
            Primitive(PrimitiveType.Cube, root.transform, Vector3.zero, Quaternion.identity, new Vector3(1.1f, 0.22f, 0.22f), mat);
            Primitive(PrimitiveType.Cube, root.transform, Vector3.zero, Quaternion.identity, new Vector3(0.22f, 1.1f, 0.22f), mat);
            Primitive(PrimitiveType.Cube, root.transform, Vector3.zero, Quaternion.identity, new Vector3(0.22f, 0.22f, 1.1f), mat);
        }
        Primitive(PrimitiveType.Sphere, root.transform, Vector3.zero, Quaternion.identity, Vector3.one * 0.35f, mat);

        var light = new GameObject("Light").AddComponent<Light>();
        light.transform.SetParent(root.transform, false);
        light.type = LightType.Point;
        light.color = color;
        light.range = 6f;
        light.intensity = 1.5f;

        PrefabUtility.SaveAsPrefabAsset(root, $"{Dir}/Pickup_{ability}.prefab");
        Object.DestroyImmediate(root);
        Debug.Log($"[PickupPrefabBuilder] saved {Dir}/Pickup_{ability}.prefab");
    }

    private static void Primitive(PrimitiveType type, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = scale;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
    }
}
