using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// カメラに Post Processing（ブルーム）を付ける。HDR の発光（レーザー、ゴール、ガイド）が画面上でにじむ。
/// </summary>
public static class PostFxSetup
{
    private const string ProfilePath = "Assets/Settings/GravityPostFx.asset";
    private const string ResourcesPath = "Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset";

    public static PostProcessProfile EnsureProfile()
    {
        // 資産は同じ GUID のまま中身だけ直す（シーンの参照を壊さない）。
        // AddSettings はサブアセットを作らないので、AddObjectToAsset で同じファイルに保存する。
        // そうしないと再読み込み後に settings が null になり、ブルームが掛からない。
        if (!AssetDatabase.IsValidFolder("Assets/Settings")) AssetDatabase.CreateFolder("Assets", "Settings");
        var profile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }
        profile.settings.RemoveAll(s => s == null);

        var bloom = profile.GetSetting<Bloom>();
        if (bloom == null) bloom = AddEffect<Bloom>(profile);
        bloom.enabled.Override(true);
        bloom.intensity.Override(2.6f);
        bloom.threshold.Override(1.05f);
        bloom.softKnee.Override(0.6f);
        bloom.diffusion.Override(7.5f);
        bloom.color.Override(Color.white);

        var grading = profile.GetSetting<ColorGrading>();
        if (grading == null) grading = AddEffect<ColorGrading>(profile);
        grading.enabled.Override(true);
        grading.tonemapper.Override(Tonemapper.ACES);
        grading.contrast.Override(8f);
        grading.saturation.Override(6f);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static T AddEffect<T>(PostProcessProfile profile) where T : PostProcessEffectSettings
    {
        var effect = profile.AddSettings<T>();
        effect.name = typeof(T).Name;
        effect.hideFlags = HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(effect, profile);
        return effect;
    }

    /// <summary>シーン内の MainCamera タグのカメラ全てに適用する。</summary>
    public static void ApplyToSceneCameras()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) { cam = c; break; }
        }
        if (cam == null) { Debug.LogWarning("[PostFxSetup] camera not found"); return; }
        Apply(cam);
    }

    public static void Apply(Camera cam)
    {
        var profile = EnsureProfile();
        cam.allowHDR = true;

        var layer = cam.GetComponent<PostProcessLayer>();
        if (layer == null) layer = cam.gameObject.AddComponent<PostProcessLayer>();
        layer.volumeTrigger = cam.transform;
        layer.volumeLayer = ~0;
        layer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
        var resources = AssetDatabase.LoadAssetAtPath<PostProcessResources>(ResourcesPath);
        if (resources != null) layer.Init(resources);
        else Debug.LogWarning("[PostFxSetup] PostProcessResources not found");

        var volume = cam.GetComponent<PostProcessVolume>();
        if (volume == null) volume = cam.gameObject.AddComponent<PostProcessVolume>();
        volume.isGlobal = true;
        volume.priority = 1f;
        volume.sharedProfile = profile;
    }
}
