using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    public static void BuildWindows()
    {
        string outputDir = GetArg("-outputDir") ?? Path.Combine(Directory.GetCurrentDirectory(), "Build");
        Directory.CreateDirectory(outputDir);

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
            .Select(s => s.path)
            .ToArray();

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(outputDir, PlayerSettings.productName + ".exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log($"[BuildScript] result={summary.result} size={summary.totalSize} errors={summary.totalErrors} warnings={summary.totalWarnings} output={summary.outputPath}");

        if (summary.result != BuildResult.Succeeded)
        {
            EditorApplication.Exit(1);
        }
    }

    /// <summary>
    /// Web (WebGL) build for Unity Play or any static host. Gzip + decompression fallback works everywhere.
    /// CLI: Unity.exe -batchmode -quit -buildTarget WebGL -projectPath . -executeMethod BuildScript.BuildWebGL -outputDir <dir>
    /// </summary>
    public static void BuildWebGL()
    {
        string outputDir = GetArg("-outputDir") ?? Path.Combine(Directory.GetCurrentDirectory(), "Build", "Web");
        Directory.CreateDirectory(outputDir);

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        PlayerSettings.runInBackground = true;

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
            .Select(s => s.path)
            .ToArray();

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log($"[BuildScript] web result={summary.result} size={summary.totalSize} errors={summary.totalErrors} warnings={summary.totalWarnings} output={summary.outputPath}");
        if (summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }

    private static string GetArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name) return args[i + 1];
        }
        return null;
    }
}
