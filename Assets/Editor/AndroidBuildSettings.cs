using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Android 向け Player Settings の適用と APK ビルド。
/// 値の根拠（Unity 6.5 マニュアル「Android requirements and compatibility」「Android Player settings」、Google Play「Target API level requirements」）:
///   - Minimum API Level 26: Unity 6.5 が対応する最小が Android 8.0 (API 26)
///   - Target API Level Auto: インストール済み SDK の最高 (platforms/android-36) に合わせる。Google Play は 2026-08-31 以降 API 36 以上を要求
///   - Scripting Backend IL2CPP: ARM64 は IL2CPP でのみ選べる
///   - Target Architectures ARM64: 64bit のみ（Google Play の 64bit 要件、APK を小さく保つ）
///   - Default Orientation LandscapeLeft（自動回転は横 2 方向のみ）: HUD・字幕・タッチ UI が横画面前提
///   - Render Outside Safe Area 有効: 画面全体に描き、UI 側で Screen.safeArea を避ける（Android 15 以降は既定で全面描画）
/// 使い方: メニュー Build > Android > Apply Player Settings / Build Development APK、
///   または CLI: Unity.exe -batchmode -quit -buildTarget Android -projectPath . -executeMethod AndroidBuildSettings.BuildDevelopmentApk
/// </summary>
public static class AndroidBuildSettings
{
    public const string PackageName = "com.vyth.gravitiria";   // 仮案。公開前に決める
    public const int MinSdk = 26;

    [MenuItem("Build/Android/Apply Player Settings")]
    public static void Apply()
    {
        var android = NamedBuildTarget.Android;
        PlayerSettings.SetApplicationIdentifier(android, PackageName);
        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)MinSdk;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, PlayerSettings.Android.bundleVersionCode);

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        PlayerSettings.Android.renderOutsideSafeArea = true;   // 全画面（6000.5 では startInFullscreen は非推奨。既定で全面描画）
        PlayerSettings.runInBackground = false;

        // Auto Graphics API（既定）: Vulkan → GLES3.2/3.1/3.0 の順に自動選択
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, true);

        EditorUserBuildSettings.buildAppBundle = false;   // まずは APK
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

        AssetDatabase.SaveAssets();
        Debug.Log($"[AndroidBuildSettings] applied: id={PlayerSettings.GetApplicationIdentifier(android)} minSdk={PlayerSettings.Android.minSdkVersion} " +
                  $"targetSdk={PlayerSettings.Android.targetSdkVersion} backend={PlayerSettings.GetScriptingBackend(android)} arch={PlayerSettings.Android.targetArchitectures} " +
                  $"orientation={PlayerSettings.defaultInterfaceOrientation}");
    }

    [MenuItem("Build/Android/Build Development APK")]
    public static void BuildDevelopmentApk()
    {
        Apply();
        string outputDir = GetArg("-outputDir") ?? Path.Combine(Directory.GetCurrentDirectory(), "Build", "Android");
        Directory.CreateDirectory(outputDir);

        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled && !string.IsNullOrEmpty(s.path)).Select(s => s.path).ToArray();
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(outputDir, PlayerSettings.productName + ".apk"),
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.Development | BuildOptions.AllowDebugging,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log($"[AndroidBuildSettings] result={summary.result} size={summary.totalSize} errors={summary.totalErrors} warnings={summary.totalWarnings} output={summary.outputPath}");
        if (summary.result != BuildResult.Succeeded && Application.isBatchMode) EditorApplication.Exit(1);
    }

    private static string GetArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
        return null;
    }
}
