using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Loading;

public static class BuildContentDirectoryMinimalRepro
{
    const string ReproAssetPath = "Assets/Temp/BuildContentDirectoryRepro/LoadableSceneRoot.asset";
    const string ReproOutputPath = "Temp/BuildContentDirectoryReproOutput";
    static readonly string[] FpsSampleRootAssetPaths =
    {
        "Assets/ContentRoots/ClientContentRoot.asset",
        "Assets/ContentRoots/ServerContentRoot.asset",
        "Assets/Resources/Content/SceneListRoot.asset"
    };

    [Serializable]
    class LoadableSceneRootAsset : ScriptableObject
    {
        public LoadableScene scene;
    }

    [MenuItem("FPS Sample/BuildSystem/Repro/Create Repro Asset")]
    public static void CreateReproAsset()
    {
        var dir = Path.GetDirectoryName(ReproAssetPath)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
        {
            var parts = dir.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        var asset = ScriptableObject.CreateInstance<LoadableSceneRootAsset>();
        AssetDatabase.CreateAsset(asset, ReproAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Created repro asset: " + ReproAssetPath + " (assign any scene into the 'scene' field, then run repro build)");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ReproAssetPath);
    }

    [MenuItem("FPS Sample/BuildSystem/Repro/Run Repro Build")]
    public static void RunReproBuild()
    {
        if (!File.Exists(ReproAssetPath))
            throw new Exception("Repro asset missing. Run 'Create Repro Asset' first.");

        Directory.CreateDirectory(ReproOutputPath);

        var previousWorkerCount = AssetDatabase.DesiredWorkerCount;
        try
        {
            AssetDatabase.DisallowAutoRefresh();
            AssetDatabase.DesiredWorkerCount = 0;
            AssetDatabase.ForceToDesiredWorkerCount();

            var parameters = new BuildContentDirectoryParameters
            {
                rootAssetPaths = new[] { ReproAssetPath },
                outputPath = ReproOutputPath,
                targetPlatform = BuildTarget.StandaloneWindows64,
                options = BuildContentOptions.CleanBuildCache | BuildContentOptions.DetailedBuildReport
            };

            Debug.Log("Running BuildPipeline.BuildContentDirectory minimal repro...");
            BuildReport report = BuildPipeline.BuildContentDirectory(parameters);

            if (report == null)
                throw new Exception("BuildContentDirectory returned null report.");

            Debug.Log("BuildContentDirectory repro result: " + report.summary.result + ", outputPath=" + ReproOutputPath);
        }
        finally
        {
            AssetDatabase.DesiredWorkerCount = previousWorkerCount;
            AssetDatabase.ForceToDesiredWorkerCount();
            AssetDatabase.AllowAutoRefresh();
        }
    }

    [MenuItem("FPS Sample/BuildSystem/Repro/Run FPS Sample Shape Repro (Default Workers)")]
    public static void RunFpsSampleShapeReproDefaultWorkers()
    {
        RunFpsSampleShapeReproInternal(forceWorkerCount: null, outputPath: "Temp/BuildContentDirectoryFpsSampleShape_DefaultWorkers");
    }

    [MenuItem("FPS Sample/BuildSystem/Repro/Run FPS Sample Shape Repro (Workers=0)")]
    public static void RunFpsSampleShapeReproWorkersZero()
    {
        RunFpsSampleShapeReproInternal(forceWorkerCount: 0, outputPath: "Temp/BuildContentDirectoryFpsSampleShape_Workers0");
    }

    static void RunFpsSampleShapeReproInternal(int? forceWorkerCount, string outputPath)
    {
        foreach (var rootPath in FpsSampleRootAssetPaths)
        {
            if (!File.Exists(rootPath))
                throw new Exception("FPS Sample repro root missing: " + rootPath);
        }

        Directory.CreateDirectory(outputPath);

        var previousWorkerCount = AssetDatabase.DesiredWorkerCount;
        var changedWorkers = false;
        try
        {
            AssetDatabase.DisallowAutoRefresh();

            if (forceWorkerCount.HasValue)
            {
                AssetDatabase.DesiredWorkerCount = forceWorkerCount.Value;
                AssetDatabase.ForceToDesiredWorkerCount();
                changedWorkers = true;
            }

            var parameters = new BuildContentDirectoryParameters
            {
                rootAssetPaths = FpsSampleRootAssetPaths,
                outputPath = outputPath,
                targetPlatform = BuildTarget.StandaloneWindows64,
                options = BuildContentOptions.CleanBuildCache | BuildContentOptions.DetailedBuildReport
            };

            Debug.Log("Running FPS Sample-shaped BuildContentDirectory repro. outputPath=" + outputPath + ", workers=" + (forceWorkerCount.HasValue ? forceWorkerCount.Value.ToString() : "default") + ", roots=" + string.Join(", ", FpsSampleRootAssetPaths));
            var report = BuildPipeline.BuildContentDirectory(parameters);
            if (report == null)
                throw new Exception("BuildContentDirectory returned null report.");

            Debug.Log("FPS Sample-shaped repro result: " + report.summary.result + ", outputPath=" + outputPath);
        }
        finally
        {
            if (changedWorkers)
            {
                AssetDatabase.DesiredWorkerCount = previousWorkerCount;
                AssetDatabase.ForceToDesiredWorkerCount();
            }

            AssetDatabase.AllowAutoRefresh();
        }
    }
}
