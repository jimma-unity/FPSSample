using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Threading;
using UnityEditor.Build;

public class BuildTools
{
    const string ContentDirectoryMigrationModePrefKey = "FPSSample.Build.ContentDirectoryMigrationMode";

    static void EnsureDirectoryClean(string rootPath)
    {
        if (!Directory.Exists(rootPath))
            return;

        EnsureDirectoryWritable(rootPath);

        foreach (var filePath in Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories))
        {
            TryDeleteFile(filePath);
        }

        var directories = Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories)
            .OrderByDescending(path => path.Length)
            .ToArray();
        foreach (var dirPath in directories)
        {
            TryDeleteDirectory(dirPath);
        }
    }

    static void TryDeleteFile(string filePath)
    {
        const int maxAttempts = 3;
        Exception last = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (!File.Exists(filePath))
                    return;

                var attributes = File.GetAttributes(filePath);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                    File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);

                File.Delete(filePath);
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                Thread.Sleep(50);
            }
        }

        throw new IOException("Failed to delete build output file: " + filePath + ". The file may be locked by a running client/server process. Close QuickStart-launched processes and retry.", last);
    }

    static void TryDeleteDirectory(string dirPath)
    {
        const int maxAttempts = 3;
        Exception last = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (!Directory.Exists(dirPath))
                    return;

                Directory.Delete(dirPath, false);
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                Thread.Sleep(50);
            }
        }

        throw new IOException("Failed to delete build output directory: " + dirPath + ". The directory may be locked by a running client/server process. Close QuickStart-launched processes and retry.", last);
    }

    static void StopRunningBuildExecutable(string buildPath, string exeName)
    {
        if (string.IsNullOrWhiteSpace(buildPath) || string.IsNullOrWhiteSpace(exeName))
            return;

        var expectedExePath = Path.GetFullPath(Path.Combine(buildPath, exeName));
        var processName = Path.GetFileNameWithoutExtension(exeName);
        if (string.IsNullOrWhiteSpace(processName))
            return;

        foreach (var process in System.Diagnostics.Process.GetProcessesByName(processName))
        {
            try
            {
                string processPath;
                try
                {
                    processPath = process.MainModule?.FileName;
                }
                catch
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(processPath))
                    continue;

                var normalizedProcessPath = Path.GetFullPath(processPath);
                if (!string.Equals(normalizedProcessPath, expectedExePath, StringComparison.OrdinalIgnoreCase))
                    continue;

                Debug.Log("Stopping running build process locking output: pid=" + process.Id + " path=" + normalizedProcessPath);
                process.Kill();
                process.WaitForExit(5000);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to stop running build process: " + ex.Message);
            }
        }
    }

    enum ContentDirectoryMigrationMode
    {
        FallbackPreferred = 0,
        StrictOnly = 1
    }

    static void EnsureDirectoryWritable(string rootPath)
    {
        if (!Directory.Exists(rootPath))
            return;

        foreach (var filePath in Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories))
        {
            try
            {
                var attributes = File.GetAttributes(filePath);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                    File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not clear read-only on " + filePath + ": " + e.Message);
            }
        }
    }

    public static bool IsBuildingContent { get; private set; }

    public static void CopyDirectory(string SourcePath, string DestinationPath)
    {
        //Create all of the directories
        foreach (string dirPath in Directory.GetDirectories(SourcePath, "*",
            SearchOption.AllDirectories))
            Directory.CreateDirectory(dirPath.Replace(SourcePath, DestinationPath));

        //Copy all the files & Replaces any files with the same name
        foreach (string newPath in Directory.GetFiles(SourcePath, "*.*",
            SearchOption.AllDirectories))
            File.Copy(newPath, newPath.Replace(SourcePath, DestinationPath), true);
    }

    public static UnityEditor.Build.Reporting.BuildReport BuildGame(string buildPath, string exeName, BuildTarget target,
        BuildOptions opts, string buildId, bool il2cpp)
    {
        var levels = new List<string>
        {
            "Assets/Scenes/bootstrapper.unity",
            "Assets/Scenes/empty.unity"
        };

        var sceneRoot = AssetDatabase.LoadAssetAtPath<SceneListRootAsset>("Assets/Resources/Content/SceneListRoot.asset");
        if (sceneRoot != null && sceneRoot.scenes != null)
        {
            foreach (var sceneEntry in sceneRoot.scenes)
            {
                if (!string.IsNullOrEmpty(sceneEntry.mainScenePath) && !levels.Contains(sceneEntry.mainScenePath))
                    levels.Add(sceneEntry.mainScenePath);

                if (sceneEntry.additiveScenePaths == null)
                    continue;

                foreach (var additivePath in sceneEntry.additiveScenePaths)
                {
                    if (!string.IsNullOrEmpty(additivePath) && !levels.Contains(additivePath))
                        levels.Add(additivePath);
                }
            }
        }
        else
        {
            Debug.Log("BuildGame: SceneListRoot asset not found at Assets/Resources/Content/SceneListRoot.asset; only bootstrap scenes will be included in player build.");
        }

        Debug.Log("BuildGame: Including " + levels.Count + " scenes in player build.");
        foreach (var level in levels)
            Debug.Log("  Scene: " + level);

        var exePathName = buildPath + "/" + exeName;

        Debug.Log("Building: " + exePathName);
        Directory.CreateDirectory(buildPath);

        // Set all files to be writeable (As Unity 2017.1 sets them to read only)
        string[] fileNames = Directory.GetFiles(buildPath, "*.*", SearchOption.AllDirectories);

        //Contentpipeline compile player scripts

        foreach (var fileName in fileNames)
        {
            FileAttributes attributes = File.GetAttributes(fileName);
            attributes &= ~FileAttributes.ReadOnly;
            File.SetAttributes(fileName, attributes);
        }

        string bundlePathSrc = buildPath + "/" + SimpleBundleManager.assetBundleFolder;
        string bundlePathDst = "Assets/StreamingAssets/" + SimpleBundleManager.assetBundleFolder;
        if (target == BuildTarget.PS4)
        {
            if (!Directory.Exists(bundlePathSrc))
            {
                EditorUtility.DisplayDialog("No bundles found", "No Asset Bundles found. Please build them first",
                    "Ok");
                return null;
            }

            CopyDirectory(bundlePathSrc, bundlePathDst);
        }

        var monoDirs = Directory.GetDirectories(buildPath).Where(s => s.Contains("MonoBleedingEdge"));
        var il2cppDirs = Directory.GetDirectories(buildPath).Where(s => s.Contains("BackUpThisFolder_ButDontShipItWithYourGame"));
        var clearFolder = (il2cpp && monoDirs.Count() > 0) || (!il2cpp && il2cppDirs.Count() > 0);
        if (clearFolder)
        {
            Debug.Log(" deleting old folders ..");
            foreach(var file in Directory.GetFiles(buildPath))
                File.Delete(file);
            foreach(var dir in monoDirs)
                Directory.Delete(dir,true);
            foreach(var dir in il2cppDirs)
                Directory.Delete(dir,true);
            foreach(var dir in Directory.GetDirectories(buildPath).Where(s => s.EndsWith("_Data")))
                Directory.Delete(dir,true);
        }

        if (il2cpp)
        {
            UnityEditor.PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            UnityEditor.PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Standalone, Il2CppCompilerConfiguration.Release);
        }
        else
        {
            UnityEditor.PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        }
        
        /// Colossal hack to work around build postprocessing expecting everything to be writable in the unity
        /// installation, but if people have unity in p4 it will be readonly.
        var editorHome = EditorApplication.applicationPath.BeforeLast("/") + "/Data/PlaybackEngines/windowsstandalonesupport";
        Debug.Log("Checking for read/only files in standalone players");
        if (Directory.Exists(editorHome))
        {
            var files = Directory.GetFiles(editorHome, "*.*", SearchOption.AllDirectories);
            foreach(var f in files)
            {
                var attr = File.GetAttributes(f);
                if((attr & FileAttributes.ReadOnly) != 0)
                {
                    attr = attr & ~FileAttributes.ReadOnly;
                    Debug.Log("Setting " + f + " to read/write");
                    File.SetAttributes(f, attr);
                }
            }
        }
        Debug.Log("Done.");
        
        Environment.SetEnvironmentVariable("BUILD_ID", buildId, EnvironmentVariableTarget.Process);
        var result = BuildPipeline.BuildPlayer(levels.ToArray(), exePathName, target, opts);
        Environment.SetEnvironmentVariable("BUILD_ID", "", EnvironmentVariableTarget.Process);

        if (target == BuildTarget.PS4)
        {
            Directory.Delete(bundlePathDst, true);
        }


        Debug.Log(" ==== Build Done =====");


        var stepCount = result.steps.Count();
        Debug.Log(" Steps:"+ stepCount);
        for(var i=0;i<stepCount;i++)
        {
            var step = result.steps[i];
            Debug.Log("-- " + (i+1) + "/" + stepCount + " " + step.name + " " + step.duration.Seconds + "s --");
            foreach (var msg in step.messages)
                Debug.Log(msg.content);
        }

        return result;
    }

    public static List<LevelInfo> LoadLevelInfos()
    {
        return LoadAssetsOfType<LevelInfo>();
    }

    static void AddKeys(Dictionary<string, int> dictionary, string[] keys)
    {
        foreach (var key in keys)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key]++;
            }
            else
            {
                dictionary[key] = 1;
            }
        }
    }

    public static List<T> LoadAssetsOfType<T>() where T : UnityEngine.Object
    {
        var result = new List<T>();
        var assets = AssetDatabase.FindAssets("t:" + typeof(T).Name);
        foreach (var a in assets)
        {
            var path = AssetDatabase.GUIDToAssetPath(a);
            result.Add(AssetDatabase.LoadAssetAtPath<T>(path));
        }
        return result;
    }

    static AssetBundleBuild MakeSceneBundleBuild(UnityEngine.Object mainScene, string name)
    {
        var build = new AssetBundleBuild();
        build.assetBundleName = name;
        build.assetBundleVariant = "";

        var path = AssetDatabase.GetAssetPath(mainScene);
        var scenes = new List<string>();
        scenes.Add(path.ToLower());

        if (EditorLevelManager.IsLayeredLevel(path))
        {
            foreach (var l in EditorLevelManager.GetLevelLayers(path))
            {
                scenes.Add(l.ToLower());
            }
        }

        build.assetNames = scenes.ToArray();
        return build;
    }

    static AssetBundleBuild MakeAssetBundleBuild(List<string> assets, string name)
    {
        var build = new AssetBundleBuild();
        build.assetBundleName = name;
        build.assetBundleVariant = "";
        build.assetNames = assets.ToArray();
        return build;
    }

    static List<string> FindSharedDependencies(List<AssetBundleBuild> builds)
    {
        var dependenciesCount = new Dictionary<string, int>();

        foreach (var build in builds)
        {
            foreach (var asset in build.assetNames)
            {
                var dependencies = AssetDatabase.GetDependencies(asset, true);
                AddKeys(dependenciesCount, dependencies);
            }
        }

        var shared = new List<string>();
        foreach (var dependency in dependenciesCount)
        {
            if (dependency.Key.EndsWith(".unity"))
                continue;
            if (dependency.Key.EndsWith(".cs"))
                continue;

            if (dependency.Value > 1)
                shared.Add(dependency.Key);
        }
        return shared;
    }

    public static void BuildBundles(string bundlePath, BuildTarget target, bool buildBundledAssets, bool buildBundledLevels, bool force = false, List<LevelInfo> buildOnlyLevels = null)
    {
        DateTime startTime = DateTime.Now;
        Debug.Log($"AssetBundle build started - {startTime:yyyy-MM-dd HH:mm:ss.fff}");

        IsBuildingContent = true;
        try
        {
            var path = bundlePath + "/" + SimpleBundleManager.assetBundleFolder;

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            EnsureDirectoryWritable(path);

            if (force)
            {
                Debug.Log("Cleaning existing bundle output directory: " + path);
                EnsureDirectoryClean(path);
            }

            BuildAssetBundleOptions assetBundleOptions = BuildAssetBundleOptions.UncompressedAssetBundle;
            if (force)
            {
                Debug.Log("Forcing rebuild");
                assetBundleOptions |= BuildAssetBundleOptions.ForceRebuildAssetBundle;
            }

            if (buildBundledLevels)
                BuildLevelBundles(path, target, assetBundleOptions, buildOnlyLevels);

            if (buildBundledAssets)
                BundledResourceBuilder.BuildBundles(path, target, assetBundleOptions);
        }
        finally
        {
            IsBuildingContent = false;
            DateTime endTime = DateTime.Now;
            Debug.Log($"AssetBundle build finished - {endTime:yyyy-MM-dd HH:mm:ss.fff}");
            Debug.Log($"Duration: {endTime - startTime}");
        }
    }

    public static void BuildContentDirectories(BuildTarget target, string outputPath, params string[] roots)
    {
        BuildContentDirectoriesInternal(target, outputPath, true, roots);
    }

    public static void BuildContentDirectoriesStrict(BuildTarget target, string outputPath, params string[] roots)
    {
        BuildContentDirectoriesInternal(target, outputPath, false, roots);
    }

    static void BuildContentDirectoriesInternal(BuildTarget target, string outputPath, bool allowLegacyFallback, params string[] roots)
    {
        var rootAssetPaths = new List<string>();
        if (roots != null)
        {
            foreach (var root in roots)
            {
                if (string.IsNullOrWhiteSpace(root))
                    continue;

                var normalized = root.Replace('\\', '/').Trim();
                rootAssetPaths.Add(normalized);
            }
        }

        if (rootAssetPaths.Count == 0)
        {
            var defaultCandidates = new[]
            {
                "Assets/ContentRoots/ClientContentRoot.asset",
                "Assets/ContentRoots/ServerContentRoot.asset",
                "Assets/Resources/Content/SceneListRoot.asset"
            };

            foreach (var path in defaultCandidates)
            {
                if (File.Exists(path))
                    rootAssetPaths.Add(path);
            }
        }

        if (rootAssetPaths.Count == 0)
            throw new Exception("BuildContentDirectories: no valid rootAssetPaths specified or discovered.");

        Directory.CreateDirectory(outputPath);

        var hasKnownImportWorkerIncompatibleRoots = rootAssetPaths.Any(path =>
            path.EndsWith("SceneListRoot.asset", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("ContentRoot.asset", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("ClientContentRoot.asset", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("ServerContentRoot.asset", StringComparison.OrdinalIgnoreCase));

        if (allowLegacyFallback && hasKnownImportWorkerIncompatibleRoots)
        {
            Debug.LogWarning("BuildContentDirectories route: fallback=BuildBundles, reason=KnownUnityEntitiesImportWorkerLimitation, outputPath=" + outputPath);
            BuildBundles(outputPath, target, true, true, true);
            return;
        }

        if (!allowLegacyFallback && hasKnownImportWorkerIncompatibleRoots)
        {
            Debug.Log("BuildContentDirectories route: strict=BuildPipeline.BuildContentDirectory, outputPath=" + outputPath + ", roots=" + string.Join(", ", rootAssetPaths));
        }

        var buildParameters = new BuildContentDirectoryParameters
        {
            rootAssetPaths = rootAssetPaths.ToArray(),
            outputPath = outputPath,
            targetPlatform = target,
            options = BuildContentOptions.CleanBuildCache | BuildContentOptions.DetailedBuildReport
        };

        var originalWorkerCount = AssetDatabase.DesiredWorkerCount;
        var requestedWorkerCount = allowLegacyFallback ? 1 : 0;
        var autoRefreshDisabled = false;
        try
        {
            AssetDatabase.DisallowAutoRefresh();
            autoRefreshDisabled = true;

            AssetDatabase.DesiredWorkerCount = requestedWorkerCount;
            AssetDatabase.ForceToDesiredWorkerCount();

            var report = BuildPipeline.BuildContentDirectory(buildParameters);
            if (report == null)
                throw new Exception("BuildPipeline.BuildContentDirectory returned null report.");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("BuildPipeline.BuildContentDirectory failed: " + report.summary.result);
        }
        catch (ArgumentException ex) when (ex.Message != null && ex.Message.IndexOf("Importing dependent assets on an import workers is currently not supported", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (!allowLegacyFallback)
                throw;

            Debug.LogWarning("BuildContentDirectories route: fallback=BuildBundles, reason=ImportWorkerException, outputPath=" + outputPath + ", error=" + ex.Message);
            BuildBundles(outputPath, target, true, true, true);
        }
        finally
        {
            if (autoRefreshDisabled)
                AssetDatabase.AllowAutoRefresh();

            AssetDatabase.DesiredWorkerCount = originalWorkerCount;
            AssetDatabase.ForceToDesiredWorkerCount();
        }

        Debug.Log("BuildContentDirectories route: primary=BuildPipeline.BuildContentDirectory, outputPath=" + outputPath + ", roots=" + string.Join(", ", rootAssetPaths));
        Debug.Log("BuildContentDirectories completed: outputPath=" + outputPath + ", roots=" + string.Join(", ", rootAssetPaths));
    }

    public static void BuildLevelBundles(string path, BuildTarget target, BuildAssetBundleOptions assetBundleOptions, List<LevelInfo> buildOnlyLevels = null)
    {
        var builds = new List<AssetBundleBuild>();
        foreach (var levelInfo in LoadLevelInfos())
        {
            if (buildOnlyLevels != null && !buildOnlyLevels.Contains(levelInfo))
                continue;
            if (!levelInfo.includeInBuild)
                continue;
            Debug.Log(" - adding level: " + AssetDatabase.GetAssetPath(levelInfo.main_scene));
            var build = MakeSceneBundleBuild(levelInfo.main_scene, levelInfo.name);
            builds.Add(build);
        }

        // TODO (petera) enable once we've done proper shared assets support
        //var dependencies = FindSharedDependencies(builds);
        //var sharedbuild = MakeAssetBundleBuild(dependencies, "shared_assets");
        //builds.Add(sharedbuild);

        // TODO (mogensh) Settle on what buildpipeline to use. LegacyBuildPipeline uses SBP internally and is faster.      
        //        LegacyBuildPipeline.BuildAssetBundles(path, builds.ToArray(), assetBundleOptions, EditorUserBuildSettings.activeBuildTarget);
        BuildPipeline.BuildAssetBundles(path, builds.ToArray(), assetBundleOptions, EditorUserBuildSettings.activeBuildTarget);

        // Set write time so tools can show time since build
        Directory.SetLastWriteTime(path, DateTime.Now);

        Debug.Log("Scene cooking done");
    }

    static string GetBuildName()
    {
        var buildNumber = System.Environment.GetEnvironmentVariable("BUILD_NUMBER");
        if (buildNumber == null)
        {
            buildNumber = "Dev";
        }

        var changeSet = System.Environment.GetEnvironmentVariable("P4_CHANGELIST");
        if (changeSet == null)
        {
            changeSet = "0";
        }

        var now = System.DateTime.Now;
        var name = now.ToString("yyyyMMdd") + "." + buildNumber + "." + changeSet;
        return name;
    }

    static string GetLongBuildName(BuildTarget target, string buildName)
    {
        return Application.productName + "_" + target.ToString() + "_" + buildName;
    }

    static string GetProjectRoot()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }

    static string GetBuildPath(BuildTarget target, string buildName)
    {
        return Path.Combine(GetProjectRoot(), "Builds", target.ToString(), GetLongBuildName(target, buildName));
    }

    static string GetBundlePath(BuildTarget target, string buildPath)
    {
        if (target == BuildTarget.StandaloneOSX)
            return buildPath + "/" + GetAppNameWithExtension(target) + "/Contents/";
        return buildPath + "/" + Application.productName + "_data";
    }

    static string GetAppNameWithExtension(BuildTarget target)
    {
        if (target == BuildTarget.StandaloneOSX)
            return Application.productName + ".app";
        if (target == BuildTarget.StandaloneWindows64)
            return Application.productName + ".exe";
        Debug.LogError("Unsupported Platform");
        return "";
    }

    static string GetBuildFolderPath(BuildTarget target)
    {
        return "Builds/" + target.ToString();
    }

    static ContentDirectoryMigrationMode GetContentDirectoryMigrationMode()
    {
        var value = EditorPrefs.GetInt(ContentDirectoryMigrationModePrefKey, (int)ContentDirectoryMigrationMode.FallbackPreferred);
        return Enum.IsDefined(typeof(ContentDirectoryMigrationMode), value)
            ? (ContentDirectoryMigrationMode)value
            : ContentDirectoryMigrationMode.FallbackPreferred;
    }

    static void SetContentDirectoryMigrationMode(ContentDirectoryMigrationMode mode)
    {
        EditorPrefs.SetInt(ContentDirectoryMigrationModePrefKey, (int)mode);
        Debug.Log("ContentDirectories migration mode set to: " + mode);
    }

    static void BuildContentDirectoriesForMigrationMode(BuildTarget target, string outputPath, params string[] roots)
    {
        var mode = GetContentDirectoryMigrationMode();
        Debug.Log("BuildContentDirectories migration mode active: " + mode);

        if (mode == ContentDirectoryMigrationMode.StrictOnly)
            BuildContentDirectoriesStrict(target, outputPath, roots);
        else
            BuildContentDirectories(target, outputPath, roots);
    }

    [MenuItem("FPS Sample/BuildSystem/ContentDirectories/MigrationMode/FallbackPreferred (Stable)")]
    public static void SetContentDirectoryMigrationModeFallbackPreferred()
    {
        SetContentDirectoryMigrationMode(ContentDirectoryMigrationMode.FallbackPreferred);
    }

    [MenuItem("FPS Sample/BuildSystem/ContentDirectories/MigrationMode/FallbackPreferred (Stable)", true)]
    public static bool ValidateContentDirectoryMigrationModeFallbackPreferred()
    {
        Menu.SetChecked("FPS Sample/BuildSystem/ContentDirectories/MigrationMode/FallbackPreferred (Stable)",
            GetContentDirectoryMigrationMode() == ContentDirectoryMigrationMode.FallbackPreferred);
        return true;
    }

    [MenuItem("FPS Sample/BuildSystem/ContentDirectories/MigrationMode/StrictOnly (Validation)")]
    public static void SetContentDirectoryMigrationModeStrictOnly()
    {
        SetContentDirectoryMigrationMode(ContentDirectoryMigrationMode.StrictOnly);
    }

    [MenuItem("FPS Sample/BuildSystem/ContentDirectories/MigrationMode/StrictOnly (Validation)", true)]
    public static bool ValidateContentDirectoryMigrationModeStrictOnly()
    {
        Menu.SetChecked("FPS Sample/BuildSystem/ContentDirectories/MigrationMode/StrictOnly (Validation)",
            GetContentDirectoryMigrationMode() == ContentDirectoryMigrationMode.StrictOnly);
        return true;
    }

    [MenuItem("Assets/ResirializeAssets")]
    public static void ReserializeProject()
    {
        if (Selection.assetGUIDs.Length == 0)
            return;

        List<string> paths = new List<string>();
        foreach (var g in Selection.assetGUIDs)
            paths.Add(AssetDatabase.GUIDToAssetPath(g));

        if (EditorUtility.DisplayDialog("Reserialize " + paths.Count + " assets", "Do you want to reserialize " + paths.Count + " assets?", "Yes, I do!"))
        {
            foreach(var p in paths)
            {
                var a = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p);
                EditorUtility.SetDirty(a);
            }
            AssetDatabase.SaveAssets();
        }
    }

    [MenuItem("FPS Sample/BuildSystem/Win64/OpenBuildFolder")]
    public static void OpenBuildFolder()
    {
        var target = BuildTarget.StandaloneWindows64;
        var buildPath = GetBuildFolderPath(target);
        if (Directory.Exists(buildPath))
        {
            Debug.Log("Opening " + buildPath);
            var p = new System.Diagnostics.Process();
            p.StartInfo = new System.Diagnostics.ProcessStartInfo("explorer.exe", Path.GetFullPath(buildPath));
            p.Start();
        }
        else
        {
            Debug.LogWarning("No build folder found here: " + buildPath);
        }
    }

    [MenuItem("FPS Sample/BuildSystem/Win64/BuildContentDirectories")]
    public static void BuildContentDirectoriesWindows64()
    {
        var target = BuildTarget.StandaloneWindows64;
        var buildName = GetBuildName();
        var buildPath = GetBuildPath(target, buildName);
        var outputPath = GetBundlePath(target, buildPath);

        Directory.CreateDirectory(buildPath);
        BuildContentDirectories(target, outputPath,
            "Assets/ContentRoots/ClientContentRoot.asset",
            "Assets/ContentRoots/ServerContentRoot.asset",
            "Assets/Resources/Content/SceneListRoot.asset");
    }

    [MenuItem("FPS Sample/BuildSystem/Win64/Deploy")]
    public static void Deploy()
    {
        Debug.Log("Window64 Deploying...");
        var target = BuildTarget.StandaloneWindows64;
        var buildName = GetBuildName();
        var buildPath = GetBuildPath(target, buildName);
        //string executableName = Application.productName + ".exe";

        var platform = target.ToString();
        var clientApi = "b5176262e35ba4aa8280a68aae7b0492";

        // TODO: Figure out if it's possible to initialize cloud / unityconnect here instead
        var projectId = CloudProjectSettings.projectId;
        var orgId = CloudProjectSettings.organizationId;
        var projectName = CloudProjectSettings.projectId;
        var accessToken = CloudProjectSettings.accessToken;

        var deploy = new ConnectedGames.Build.DeployTools(OnProgressUpdate, clientApi, projectId, orgId, projectName, accessToken);

        var dstPath = buildName + ".zip";

        Debug.Log("Starting upload src=" + buildPath + " platform=" + platform + " isClient=N/A" +
            " clientApi=" + clientApi + " projectId=" + projectId + " orgId=" + orgId +
            " projectName=" + projectName + " accessToken=" + accessToken);

        deploy.CompressAndUpload(buildPath, buildPath+"/"+dstPath, platform, buildName);
        while (!deploy.Done)
        {
            deploy.UpdateLoop();
            Thread.Sleep(100);
        }
    }

    private static void OnProgressUpdate(string fileName, double progress)
    {
        Debug.Log(fileName + ":" + progress);
        return;
    }

    [MenuItem("FPS Sample/BuildSystem/Win64/PostProcess")]
    public static void PostProcessWindows64()
    {
        Debug.Log("Window64 build postprocessing...");
        var target = BuildTarget.StandaloneWindows64;
        var buildName = GetBuildName();
        var zipName = GetLongBuildName(target, buildName) + ".zip";
        var buildPath = GetBuildPath(target, buildName);
        string executableName = GetAppNameWithExtension(target);

        if (!Directory.Exists(buildPath) || !File.Exists(buildPath + "/" + executableName))
        {
            Debug.Log("No build here: " + buildPath);
            return;
        }

        Debug.Log("Writing config files");
        // Build server bat
        var serverBat = new string[]
        {
            "REM start game server on level_01",
            executableName + " -nographics -batchmode -noboot +serve level_01 +game.modename assault"
        };
        File.WriteAllLines(buildPath + "/server.bat", serverBat);
        Debug.Log("  server.bat");

        // Build empty user.cfg
        File.WriteAllLines(buildPath + "/user.cfg", new string[] { });
        Debug.Log("  user.cfg");

        // Build boot.cfg
        var bootCfg = new string[]
        {
           "client",
           "load level_menu"
        };
        File.WriteAllLines(buildPath + "/" + Game.k_BootConfigFilename, bootCfg);
        Debug.Log("  " + Game.k_BootConfigFilename);

        Debug.Log("Writing steam upload bat file.");
        var steamBat = new string[]
        {
@"@echo off",
@"color",
@"echo Verifying Steam SDK...",
@"if not exist c:\steam\sdk\tools\ContentBuilder (",
@"    echo Failed. Steam SDK must be installed at c:\steam",
@"    goto :err",
@")",
@"echo OK. Found SDK at c:\steam",
@"echo Looking for zipped game...",
@"if not exist " + zipName + " (",
@"    echo Failed. Did not locate zip: " + zipName,
@"    goto :err",
@")",
@"echo OK. Found game at "+zipName,
@"echo Removing old stage area",
@"rmdir /s /q c:\steam\stage",
@"echo Extracting build to staging area",
@"""c:\Program Files\7-Zip\7z.exe"" x "+zipName + @" -oc:\steam\stage",
@" cd c:\steam\sdk\tools\ContentBuilder",
@"set /p steamuser=""Enter steam user:""",
@"builder\steamcmd.exe +login %steamuser% +run_app_build_http ..\scripts\app_build_962460.vdf +quit",
@"echo All done!",
@"goto :ok",
@":err",
@"color 4f",
@"goto :done",
@":ok",
@"color 2f",
@":done",
@"pause"
        };
        var steamBatName = "steam_upload_" + buildName + ".bat";
        File.WriteAllLines(buildPath + "/../" + steamBatName, steamBat);
        Debug.Log("  " + steamBatName);


        Debug.Log("Window64 build postprocessing done.");
    }

    [MenuItem("FPS Sample/BuildSystem/Win64/CreateBuildWindows64")]
    public static void CreateBuildWindows64()
    {
        CreateBuildWindows64(false);
    }

    [MenuItem("FPS Sample/BuildSystem/Win64/CreateBuildWindows64-IL2CPP")]
    public static void CreateBuildWindows64IL2CPP()
    {
        CreateBuildWindows64(true);
    }

    static void CreateBuildWindows64(bool useIL2CPP)
    {
        Debug.Log("Window64 build started. (" + (useIL2CPP ? "IL2CPP" : "Mono") + ")");
        var target = BuildTarget.StandaloneWindows64;
        var buildName = GetBuildName();
        var buildPath = GetBuildPath(target, buildName);
        var bundlePath = GetBundlePath(target, buildPath);
        string executableName = GetAppNameWithExtension(target);

        Directory.CreateDirectory(buildPath);

        BuildBundles(bundlePath, target, true, true, true);
        var res = BuildGame(buildPath, executableName, target, BuildOptions.None, buildName, useIL2CPP);

        if (!res)
            throw new Exception("BuildPipeline.BuildPlayer failed");
        if (res.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("BuildPipeline.BuildPlayer failed: " + res.ToString());

        Debug.Log("Window64 build completed...");
        PostProcessWindows64();
    }

    [MenuItem("FPS Sample/BuildSystem/Win64/CreateAutoBuildLike")]
    public static void CreateAutoBuildLikeWindows64()
    {
        Debug.Log("Window64 Autobuild-like build started.");

        var target = BuildTarget.StandaloneWindows64;
        var buildPath = Path.Combine(GetProjectRoot(), "Autobuild");
        var exeName = "Autobuild.exe";
        var dataPath = Path.Combine(buildPath, "Autobuild_Data");

        StopRunningBuildExecutable(buildPath, exeName);

        Directory.CreateDirectory(buildPath);
        BuildContentDirectoriesForMigrationMode(target, dataPath,
            "Assets/ContentRoots/ClientContentRoot.asset",
            "Assets/ContentRoots/ServerContentRoot.asset",
            "Assets/Resources/Content/SceneListRoot.asset");

        var res = BuildGame(buildPath, exeName, target, BuildOptions.None, "AutoBuild", false);
        if (!res)
            throw new Exception("BuildPipeline.BuildPlayer failed");
        if (res.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("BuildPipeline.BuildPlayer failed: " + res.summary.result);

        var configDir = Path.Combine(GetProjectRoot(), "Configs");
        var srcBoot = Path.Combine(configDir, "boot.cfg");
        var srcUser = Path.Combine(configDir, "user.cfg");
        var dstBoot = Path.Combine(buildPath, Game.k_BootConfigFilename);
        var dstUser = Path.Combine(buildPath, "user.cfg");

        if (File.Exists(srcBoot))
            File.Copy(srcBoot, dstBoot, true);
        else
            File.WriteAllLines(dstBoot, new[] { "client", "load level_menu" });

        if (File.Exists(srcUser))
            File.Copy(srcUser, dstUser, true);
        else
            File.WriteAllLines(dstUser, Array.Empty<string>());

        var serverBat = new[]
        {
            "REM start game server on level_01",
            exeName + " -nographics -batchmode -noboot +serve level_01 +game.modename assault"
        };
        File.WriteAllLines(Path.Combine(buildPath, "server.bat"), serverBat);

        Debug.Log("Window64 Autobuild-like build completed at: " + buildPath);
    }

    [MenuItem("FPS Sample/BuildSystem/Win64/CreateAutoBuildLike-ContentDirectoriesOnly")]
    public static void CreateAutoBuildLikeWindows64ContentDirectoriesOnly()
    {
        Debug.Log("Window64 Autobuild-like strict content-directory build started.");

        var target = BuildTarget.StandaloneWindows64;
        var buildPath = Path.Combine(GetProjectRoot(), "Autobuild");
        var exeName = "Autobuild.exe";
        var dataPath = Path.Combine(buildPath, "Autobuild_Data");

        StopRunningBuildExecutable(buildPath, exeName);

        Directory.CreateDirectory(buildPath);
        BuildContentDirectoriesStrict(target, dataPath,
            "Assets/ContentRoots/ClientContentRoot.asset",
            "Assets/ContentRoots/ServerContentRoot.asset",
            "Assets/Resources/Content/SceneListRoot.asset");

        var res = BuildGame(buildPath, exeName, target, BuildOptions.None, "AutoBuild", false);
        if (!res)
            throw new Exception("BuildPipeline.BuildPlayer failed");
        if (res.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("BuildPipeline.BuildPlayer failed: " + res.summary.result);

        var configDir = Path.Combine(GetProjectRoot(), "Configs");
        var srcBoot = Path.Combine(configDir, "boot.cfg");
        var srcUser = Path.Combine(configDir, "user.cfg");
        var dstBoot = Path.Combine(buildPath, Game.k_BootConfigFilename);
        var dstUser = Path.Combine(buildPath, "user.cfg");

        if (File.Exists(srcBoot))
            File.Copy(srcBoot, dstBoot, true);
        else
            File.WriteAllLines(dstBoot, new[] { "client", "load level_menu" });

        if (File.Exists(srcUser))
            File.Copy(srcUser, dstUser, true);
        else
            File.WriteAllLines(dstUser, Array.Empty<string>());

        var serverBat = new[]
        {
            "REM start game server on level_01",
            exeName + " -nographics -batchmode -noboot +serve level_01 +game.modename assault"
        };
        File.WriteAllLines(Path.Combine(buildPath, "server.bat"), serverBat);

        Debug.Log("Window64 Autobuild-like strict content-directory build completed at: " + buildPath);
    }

    static void WriteShellScriptAndMakeExecutable(string fullPath, string[] script)
    {
        File.WriteAllLines(fullPath, script);

        // chmod +x
        var chmod = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "/bin/chmod",
            Arguments = $"+x \"{fullPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var p = System.Diagnostics.Process.Start(chmod);
        p!.WaitForExit();
        if (p.ExitCode != 0)
            Debug.LogError("chmod failed: " + p.StandardError.ReadToEnd());
    }

    [MenuItem("FPS Sample/BuildSystem/macOS/PostProcess")]
    public static void PostProcessMacOs()
    {
        Debug.Log("macOS build postprocessing...");
        var target = BuildTarget.StandaloneOSX;
        var buildName = GetBuildName();
        //var zipName = GetLongBuildName(target, buildName) + ".zip";
        var buildPath = GetBuildPath(target, buildName);
        string executableName = GetAppNameWithExtension(target);
        var exePath = buildPath + "/" + executableName;

        if (!Directory.Exists(buildPath) || !Directory.Exists(exePath))
        {
            Debug.Log("No build here: " + buildPath);
            return;
        }

        Debug.Log("Writing config files");
        // Build server sh
        var serverSh = new[]
        {
            "#!/bin/zsh",
            "# start game server on level_01",
            "open " + "./" + executableName + " " + "-n " + "--args " + "-nographics -batchmode -noboot +serve level_01 +game.modename assault"
        };
        var serverScriptPath = buildPath + "/server.sh";
        WriteShellScriptAndMakeExecutable(serverScriptPath, serverSh);
        Debug.Log("  server.sh");

        // Build client sh
        var clientSh = new[]
        {
            "#!/bin/zsh",
            "# start game client",
            "open " + "./" + executableName + " " + "-n "
        };
        var clientScriptPath = buildPath + "/client.sh";
        WriteShellScriptAndMakeExecutable(clientScriptPath, clientSh);
        Debug.Log("  client.sh");

        // Build boot.cfg
        var bootCfg = new[]
        {
           "client",
           "load level_menu"
        };
        File.WriteAllLines(buildPath + "/" + Game.k_BootConfigFilename, bootCfg);
        Debug.Log("  " + Game.k_BootConfigFilename);
    }
    
    [MenuItem("FPS Sample/BuildSystem/macOS/CreateBuildMacOS")]
    public static void CreateBuildMacOs()
    {
        CreateBuildMacOs(false);
    }

    [MenuItem("FPS Sample/BuildSystem/macOS/CreateBuildMacOS-IL2CPP")]
    public static void CreateBuildMacOsIL2CPP()
    {
        CreateBuildMacOs(true);
    }
    static void CreateBuildMacOs(bool useIL2CPP)
    {
        Debug.Log("macOS build started. (" + (useIL2CPP ? "IL2CPP" : "Mono") + ")");
        var target = BuildTarget.StandaloneOSX;
        var buildName = GetBuildName();
        var buildPath = GetBuildPath(target, buildName);
        var bundlePath = GetBundlePath(target, buildPath);
        string executableName = GetAppNameWithExtension(target);

        Directory.CreateDirectory(buildPath);

        BuildBundles(bundlePath, target, true, true, true);
        var res = BuildGame(buildPath, executableName, target, BuildOptions.None, buildName, useIL2CPP);

        if (!res)
            throw new Exception("BuildPipeline.BuildPlayer failed");
        if (res.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("BuildPipeline.BuildPlayer failed: " + res.ToString());

        Debug.Log("macOS build completed...");
        PostProcessMacOs();
    }

    [MenuItem("FPS Sample/BuildSystem/PS5/CreateBuildPS5")]
    public static void CreateBuildPS5()
    {
        var target = BuildTarget.PS5;
        var buildName = GetBuildName();
        var buildPath = GetBuildPath(target, buildName);
        var bundlePath = GetBundlePath(target, buildPath);
        string executableName = Application.productName;

        Directory.CreateDirectory(buildPath);
        BuildBundles(bundlePath, target, true, true, true);
        var res = BuildGame(buildPath, executableName, target, BuildOptions.None, buildName, false);

        if (!res)
            throw new Exception("BuildPipeline.BuildPlayer failed");
        if (res.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("BuildPipeline.BuildPlayer failed: " + res.ToString());

        PostProcessBuildPS5();
    }

    [MenuItem("FPS Sample/BuildSystem/PS5/PostProcessBuildPS5")]
    public static void PostProcessBuildPS5()
    {
        var target = BuildTarget.PS5;
        var buildName = GetBuildName();
        var buildPath = GetBuildPath(target, buildName);
        var bootcfgPath = buildPath + "/Build/Media/StreamingAssets/" + Game.k_BootConfigFilename;
        
        // Build boot.cfg
        var bootCfg = new[]
        {
            "client 10.33.32.73"
        };
        
        File.WriteAllLines(bootcfgPath, bootCfg);
        Debug.Log("  " + Game.k_BootConfigFilename);
    }
    
    [MenuItem("FPS Sample/BuildSystem/Xbox/CreateBuildXbox")]
    public static void CreateBuildXbox()
    {
        var target = BuildTarget.GameCoreXboxSeries;
        var buildName = GetBuildName();
        var buildPath = GetBuildPath(target, buildName);
        var bundlePath = GetBundlePath(target, buildPath);
        string executableName = Application.productName;

        Directory.CreateDirectory(buildPath);
        BuildBundles(bundlePath, target, true, true, true);
        var res = BuildGame(buildPath, executableName, target, BuildOptions.None, buildName, false);

        if (!res)
            throw new Exception("BuildPipeline.BuildPlayer failed");
        if (res.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("BuildPipeline.BuildPlayer failed: " + res.ToString());

        PostProcessBuildXbox();
    }

    [MenuItem("FPS Sample/BuildSystem/Xbox/PostProcessBuildXbox")]
    public static void PostProcessBuildXbox()
    {
        var target = BuildTarget.GameCoreXboxSeries;
        var buildName = GetBuildName();
        var buildPath = GetBuildPath(target, buildName);
        var bootcfgPath = GetBundlePath(target, buildPath) + "/" + Game.k_BootConfigFilename;
        
        // Build boot.cfg
        //"client 10.33.32.14"
        var bootCfg = new[]
        {
            "client 10.33.32.14",
            //"load level_menu"
        };
        
        File.WriteAllLines(bootcfgPath, bootCfg);
        Debug.Log("  " + "Writing "  + bootcfgPath);
    }

    [MenuItem("FPS Sample/BuildSystem/Linux64/PostProcess")]
    public static void PostProcessLinux()
    {
        Debug.Log("Linux build postprocessing...");
        var target = BuildTarget.StandaloneLinux64;
        var buildName = GetBuildName();
        var buildPath = GetBuildPath(target, buildName);
        string executableName = "server-linux.x86_64";


        if (!Directory.Exists(buildPath) || !File.Exists(buildPath + "/" + executableName))
        {
            Debug.Log("No build here: " + buildPath);
            return;
        }

        Debug.Log("Writing config files");

        // Build empty user.cfg
        File.WriteAllLines(buildPath + "/user.cfg", new string[] { });
        Debug.Log("  user.cfg");

        // Build boot.cfg
        var bootCfg = new string[]
        {
           "game.modename assault",
           "serve level_01"
        };
        File.WriteAllLines(buildPath + "/" + Game.k_BootConfigFilename, bootCfg);
        Debug.Log("  " + Game.k_BootConfigFilename);

        Debug.Log("Linux build postprocessing done.");
    }

    [MenuItem("FPS Sample/BuildSystem/Linux64/CreateBuildLinux64")]
    public static void CreateBuildLinux64()
    {
        var target = BuildTarget.StandaloneLinux64;
        var buildName = GetBuildName();
        var buildPath = GetBuildPath(target, buildName);
        string executableName = "server-linux.x86_64";

        Directory.CreateDirectory(buildPath);
        BuildBundles(buildPath, target, true, true, true);
        var res = BuildGame(buildPath, executableName, target, BuildOptions.EnableHeadlessMode, buildName, false);

        if (!res)
            throw new Exception("BuildPipeline.BuildPlayer failed");
        if (res.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("BuildPipeline.BuildPlayer failed: " + res.ToString());

        GameDebug.Log("Linux build completed...");
        PostProcessLinux();
    }

    // This is just a little convenience for when iterating on Linux specific code
    // Build a full build and then use this to just build the executable into the same build folder.
    [MenuItem("FPS Sample/BuildSystem/Linux64/CreateBuildLinux64_OnlyExecutable")]
    public static void CreateBuildLinux64_OnlyExecutable()
    {
        var target = BuildTarget.StandaloneLinux64;
        var buildName = GetBuildName();
        var buildPath = GetBuildPath(target, buildName);
        string executableName = "server-linux.x86_64";

        Directory.CreateDirectory(buildPath);
        var res = BuildGame(buildPath, executableName, target, BuildOptions.EnableHeadlessMode, buildName, false);

        if (!res)
            throw new Exception("BuildPipeline.BuildPlayer failed");
        if (res.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("BuildPipeline.BuildPlayer failed: " + res.ToString());

        GameDebug.Log("Linux build completed...");
    }
}
