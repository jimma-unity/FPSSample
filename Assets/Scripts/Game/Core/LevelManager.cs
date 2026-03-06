using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Loading;
using UnityEngine.SceneManagement;


public enum LevelState
{
    Loading,
    Loaded,
}

public struct LevelLayer
{
    public AsyncOperation loadOperation;
    public string expectedScenePath;
    public string expectedSceneName;
    public bool additive;
}

public class Level
{
    public LevelState state;
    public string name;
    public bool usedLegacyBundles;
    public bool recoveryAttempted;
    public List<LevelLayer> layers = new List<LevelLayer>(10);
}

public class LevelManager
{
    public static bool forceLegacyLevelLoadingInEditor = true;

    [ConfigVar(Name = "debug.clientstrip", DefaultValue = "0", Description = "Enable client-side strip pass in development builds (1=on, 0=off)")]
    public static ConfigVar debugClientStrip;

    public static readonly string[] layerNames = new string[]
    {
        "background",
        "gameplay",
    };

    public Level currentLevel { get; private set; }

    public void Init()
    {
    }

    public bool IsCurrentLevelLoaded()
    {
        return currentLevel != null && currentLevel.state == LevelState.Loaded;
    }

    public bool IsLoadingLevel()
    {
        return currentLevel != null && currentLevel.state == LevelState.Loading;
    }

    public bool CanLoadLevel(string name)
    {
        var strictContentDirectoryOnly = ContentResolverFactory.UseContentDirectoryOnlyMode();

        if (!(Application.isEditor && forceLegacyLevelLoadingInEditor))
        {
            if (TryResolveSceneEntry(name, out _))
                return true;
        }

        if (strictContentDirectoryOnly)
            return false;

        // TODO (petera). We can't really promise you can load a level before trying.
        // Refactor to handle errors during load.
        var bundle = SimpleBundleManager.LoadLevelAssetBundle(name);
        return bundle != null;
    }

    public bool LoadLevel(string name)
    {
        if (currentLevel != null)
            UnloadLevel(false);

        // This is a pretty ugly hack to handle problems with loading camera and post processing volumes
        // and those not being initalized at the same time. We simply disable the old camera and the 
        Game.game.TopCamera().enabled = false;
        Game.game.BlackFade(true);

        var newLevel = new Level();
        newLevel.name = name;

        var strictContentDirectoryOnly = ContentResolverFactory.UseContentDirectoryOnlyMode();
        var isEditor = Application.isEditor;
        var forceLegacy = isEditor && forceLegacyLevelLoadingInEditor && !strictContentDirectoryOnly;
        var preferredLoader = forceLegacy ? "LegacyBundle" : "SceneListRoot";
        GameDebug.Log("Level load policy: level=" + newLevel.name + ", runtime=" + (isEditor ? "Editor" : "Player") + ", forceLegacyInEditor=" + forceLegacyLevelLoadingInEditor + ", preferredLoader=" + preferredLoader);

        if (forceLegacy)
        {
            GameDebug.Log("Level load path: Legacy bundle forced in editor for " + newLevel.name);
            return LoadLevelFromLegacyBundle(newLevel);
        }

        if (LoadLevelFromSceneList(newLevel))
        {
            GameDebug.Log("Level load path: SceneListRoot (LoadableScene) for " + newLevel.name);
            currentLevel = newLevel;
            return true;
        }

        if (strictContentDirectoryOnly)
        {
            GameDebug.Log("Level load path: SceneListRoot strict mode failed for " + newLevel.name + ". Legacy bundle fallback is disabled.");
            return false;
        }

        GameDebug.Log("Level load path: Legacy bundle fallback for " + newLevel.name);
        return LoadLevelFromLegacyBundle(newLevel);
    }

    bool LoadLevelFromSceneList(Level newLevel)
    {
        if (!TryResolveSceneEntry(newLevel.name, out var sceneEntry))
            return false;

        var additiveLoadableCount = sceneEntry.additiveScenes != null ? sceneEntry.additiveScenes.Count : 0;
        var additivePathCount = sceneEntry.additiveScenePaths != null ? sceneEntry.additiveScenePaths.Count : 0;
        var additiveCount = Mathf.Max(additiveLoadableCount, additivePathCount);
        GameDebug.Log("SceneListRoot resolved level " + newLevel.name + " (additive scenes: " + additiveCount + ")");

        var mainLoadOperation = LoadLoadableScene(sceneEntry.mainScene, LoadSceneMode.Single);
        if (mainLoadOperation == null)
        {
            var configuredPath = string.IsNullOrEmpty(sceneEntry.mainScenePath) ? "<empty>" : sceneEntry.mainScenePath;
            var normalizedPath = string.IsNullOrEmpty(sceneEntry.mainScenePath) ? string.Empty : sceneEntry.mainScenePath.Replace('\\', '/');
            var buildIndex = string.IsNullOrEmpty(normalizedPath) ? -1 : SceneUtility.GetBuildIndexByScenePath(normalizedPath);
            GameDebug.Log("SceneListRoot fallback to path load for " + newLevel.name + ": mainScenePath=" + configuredPath + ", buildIndex=" + buildIndex);
            mainLoadOperation = LoadSceneByPath(sceneEntry.mainScenePath, LoadSceneMode.Single);
            GameDebug.Log("SceneListRoot path load result for " + newLevel.name + ": " + (mainLoadOperation != null ? "success" : "null"));
        }

        if (mainLoadOperation == null)
        {
            GameDebug.Log("SceneListRoot entry found but unable to load scene for: " + newLevel.name + ". Falling back to legacy bundle flow.");
            return false;
        }

        var mainExpectedPath = sceneEntry.mainScenePath;
        var mainExpectedName = string.IsNullOrEmpty(mainExpectedPath)
            ? newLevel.name
            : Path.GetFileNameWithoutExtension(mainExpectedPath.Replace('\\', '/'));
        newLevel.layers.Add(new LevelLayer
        {
            loadOperation = mainLoadOperation,
            expectedScenePath = mainExpectedPath,
            expectedSceneName = mainExpectedName,
            additive = false
        });

        for (var i = 0; i < additiveCount; i++)
        {
            AsyncOperation additiveLoadOperation = null;
            string additivePath = null;

            if (sceneEntry.additiveScenes != null && i < sceneEntry.additiveScenes.Count)
                additiveLoadOperation = LoadLoadableScene(sceneEntry.additiveScenes[i], LoadSceneMode.Additive);

            if (sceneEntry.additiveScenePaths != null && i < sceneEntry.additiveScenePaths.Count)
                additivePath = sceneEntry.additiveScenePaths[i];

            if (additiveLoadOperation == null && !string.IsNullOrEmpty(additivePath))
            {
                var normalizedPath = additivePath.Replace('\\', '/');
                var additiveBuildIndex = SceneUtility.GetBuildIndexByScenePath(normalizedPath);
                GameDebug.Log("SceneListRoot additive fallback for " + newLevel.name + " layer " + i + ": path=" + normalizedPath + ", buildIndex=" + additiveBuildIndex);
                additiveLoadOperation = LoadSceneByPath(additivePath, LoadSceneMode.Additive);
            }

            if (additiveLoadOperation != null)
            {
                var additiveExpectedName = !string.IsNullOrEmpty(additivePath)
                    ? Path.GetFileNameWithoutExtension(additivePath.Replace('\\', '/'))
                    : string.Empty;
                newLevel.layers.Add(new LevelLayer
                {
                    loadOperation = additiveLoadOperation,
                    expectedScenePath = additivePath,
                    expectedSceneName = additiveExpectedName,
                    additive = true
                });
                GameDebug.Log("SceneListRoot additive load success for " + newLevel.name + " layer " + i + (string.IsNullOrEmpty(additivePath) ? string.Empty : ": " + additivePath));
            }
            else
            {
                GameDebug.Log("Warning : Unable to load additive scene layer " + i + " for " + newLevel.name);
            }
        }

        return true;
    }

    AsyncOperation LoadSceneByPath(string scenePath, LoadSceneMode mode)
    {
        if (string.IsNullOrEmpty(scenePath))
        {
            GameDebug.Log("LoadSceneByPath skipped: scene path is empty.");
            return null;
        }

        var normalizedPath = scenePath.Replace('\\', '/');
        var sceneName = Path.GetFileNameWithoutExtension(normalizedPath);

        var buildIndex = SceneUtility.GetBuildIndexByScenePath(normalizedPath);
        if (buildIndex >= 0)
        {
            try
            {
                return SceneManager.LoadSceneAsync(buildIndex, mode);
            }
            catch (Exception ex)
            {
                GameDebug.Log("LoadSceneByPath build-index load failed for " + normalizedPath + " (" + buildIndex + "): " + ex.Message);
            }
        }

        try
        {
            var op = SceneManager.LoadSceneAsync(normalizedPath, mode);
            if (op != null)
                return op;
        }
        catch (Exception ex)
        {
            GameDebug.Log("LoadSceneByPath path-load failed for " + normalizedPath + " (buildIndex: " + buildIndex + "): " + ex.Message);
        }

        try
        {
            if (string.IsNullOrEmpty(sceneName))
                return null;

            var op = SceneManager.LoadSceneAsync(sceneName, mode);
            if (op != null)
                return op;

            return null;
        }
        catch (Exception ex)
        {
            GameDebug.Log("LoadSceneByPath failed for " + normalizedPath + " (sceneName: " + sceneName + ", buildIndex: " + buildIndex + "): " + ex.Message);
            return null;
        }
    }

    bool LoadLevelFromLegacyBundle(Level newLevel)
    {
        newLevel.usedLegacyBundles = true;

        // TODO (petera) Use async? Seem to be not needed here.
        var bundle = SimpleBundleManager.LoadLevelAssetBundle(newLevel.name);
        if (bundle == null)
        {
            GameDebug.Log("Could not load asset bundle for scene " + newLevel.name);
            return false;
        }

        // Load using the name found in GetAllScenePaths because SceneManager.LoadSceneAsync is case sensitive
        // yet name may not have correct casing as file system may be case insensitive 

        var scenePaths = new List<string>(bundle.GetAllScenePaths());
        if (scenePaths.Count < 1)
        {
            GameDebug.Log("No scenes in asset bundle " + newLevel.name);
            return false;
        }

        // If there is a main scene, load that first
        // TODO (petera) switch to LevelInfo based layers
        var mainScenePath = scenePaths.Find(x => x.ToLower().EndsWith("_main.unity"));
        var useLayers = true;
        if (mainScenePath == null)
        {
            useLayers = false;
            mainScenePath = scenePaths[0];
        }

        GameDebug.Log("Loading " + mainScenePath);
        var mainLoadOperation = SceneManager.LoadSceneAsync(mainScenePath, LoadSceneMode.Single);
        if (mainLoadOperation == null)
        {
            GameDebug.Log("Failed to load level : " + newLevel.name);
            return false;
        }

        currentLevel = newLevel;
        currentLevel.layers.Add(new LevelLayer { loadOperation = mainLoadOperation });

        if (!useLayers)
            return true;

        // Now load all additional layers that may be here
        foreach (var l in layerNames)
        {
            var layerScenePath = scenePaths.Find(x => x.ToLower().EndsWith(l + ".unity"));
            if (layerScenePath == null)
                continue;

            // TODO : Are we guaranteed that the scenes are initialized in order without setting allowactivation = false?
            GameDebug.Log("+Loading " + layerScenePath);
            var layerLoadOperation = SceneManager.LoadSceneAsync(layerScenePath, LoadSceneMode.Additive);
            if (layerLoadOperation != null)
            {
                currentLevel.layers.Add(new LevelLayer { loadOperation = layerLoadOperation });
            }
            else
            {
                GameDebug.Log("Warning : Unable to load level layer : " + layerScenePath);
            }
        }

        return true;
    }

    bool TryResolveSceneEntry(string name, out SceneListRootAsset.SceneEntry entry)
    {
        var sceneRoot = SceneListRootAsset.LoadDefault();
        if (sceneRoot == null)
        {
            entry = default;
            return false;
        }

        return sceneRoot.TryGetScene(name, out entry);
    }

    AsyncOperation LoadLoadableScene(LoadableScene scene, LoadSceneMode mode)
    {
        var methods = typeof(SceneManager).GetMethods(BindingFlags.Public | BindingFlags.Static);
        for (var i = 0; i < methods.Length; i++)
        {
            var method = methods[i];
            if (!string.Equals(method.Name, nameof(SceneManager.LoadSceneAsync), StringComparison.Ordinal))
                continue;

            var parameters = method.GetParameters();
            if (parameters.Length < 1 || parameters.Length > 2)
                continue;

            if (parameters[0].ParameterType.Name != nameof(LoadableScene))
                continue;

            try
            {
                if (parameters.Length == 1)
                    return method.Invoke(null, new object[] { scene }) as AsyncOperation;

                if (parameters[1].ParameterType == typeof(LoadSceneMode))
                    return method.Invoke(null, new object[] { scene, mode }) as AsyncOperation;

                if (parameters[1].ParameterType == typeof(LoadSceneParameters))
                    return method.Invoke(null, new object[] { scene, new LoadSceneParameters(mode) }) as AsyncOperation;
            }
            catch (Exception ex)
            {
                GameDebug.Log("LoadLoadableScene reflection invoke failed: " + ex.Message);
            }
        }

        GameDebug.Log("LoadLoadableScene: No compatible SceneManager.LoadSceneAsync overload found for LoadableScene.");
        return null;
    }

    public void UnloadLevel()
    {
        UnloadLevel(true);
    }

    void UnloadLevel(bool loadEmptyScene)
    {
        if (currentLevel == null)
            return;

        if (currentLevel.state == LevelState.Loading)
            throw new NotImplementedException("TODO : Implement unload during load");

        if (loadEmptyScene)
        {
            // TODO : Load empty scene for now
            SceneManager.LoadScene(1);
        }

        if (currentLevel.usedLegacyBundles)
            SimpleBundleManager.ReleaseLevelAssetBundle(currentLevel.name);

        currentLevel = null;
    }

    public void Update()
    {
        if (currentLevel != null && currentLevel.state == LevelState.Loading)
        {
            var done = currentLevel.layers.All(l => l.loadOperation.isDone);
            if (done)
            {
                if (TryRecoverMissingExpectedScenes(currentLevel))
                    return;

                // Do activation here?
                currentLevel.state = LevelState.Loaded;

                if (Game.GameLoopCount == 1)
                {
                    if (Game.GetGameLoop<ServerGameLoop>() != null)
                        StripCode(BuildType.Server, true);
                    else if (Game.GetGameLoop<ClientGameLoop>() != null)
                        StripCode(BuildType.Client, true);  
                    else
                        StripCode(BuildType.Default, true);
                }
                else
                    StripCode(BuildType.Default, true);

                var topCamera = Game.game.TopCamera();
                if (topCamera != null && !topCamera.enabled)
                {
                    topCamera.enabled = true;
                    GameDebug.Log("Re-enabled top camera after level load: " + topCamera.name);
                }

                Game.game.BlackFade(false);

                LogLoadedScenes();
                
                GameDebug.Log("Scene " + currentLevel.name + " loaded");
            }
        }
    }

    bool TryRecoverMissingExpectedScenes(Level level)
    {
        if (level == null)
            return false;

        var missingAdditiveLayers = new List<LevelLayer>();
        for (var i = 0; i < level.layers.Count; i++)
        {
            var layer = level.layers[i];
            if (string.IsNullOrEmpty(layer.expectedScenePath) && string.IsNullOrEmpty(layer.expectedSceneName))
                continue;

            if (IsExpectedSceneLoaded(layer.expectedScenePath, layer.expectedSceneName))
                continue;

            if (!layer.additive)
            {
                GameDebug.Log("Main scene appears missing after load completion: expectedPath=" + layer.expectedScenePath + ", expectedName=" + layer.expectedSceneName);
                continue;
            }

            missingAdditiveLayers.Add(layer);
        }

        if (missingAdditiveLayers.Count == 0)
            return false;

        if (level.recoveryAttempted)
        {
            GameDebug.Log("Scene recovery already attempted; additive scenes still missing for level " + level.name + ".");
            return false;
        }

        level.recoveryAttempted = true;
        var recovered = 0;
        for (var i = 0; i < missingAdditiveLayers.Count; i++)
        {
            var layer = missingAdditiveLayers[i];
            AsyncOperation op = null;
            if (!string.IsNullOrEmpty(layer.expectedScenePath))
                op = LoadSceneByPath(layer.expectedScenePath, LoadSceneMode.Additive);

            if (op == null && !string.IsNullOrEmpty(layer.expectedSceneName))
            {
                try
                {
                    op = SceneManager.LoadSceneAsync(layer.expectedSceneName, LoadSceneMode.Additive);
                }
                catch (Exception ex)
                {
                    GameDebug.Log("Recovery additive load failed for sceneName=" + layer.expectedSceneName + ": " + ex.Message);
                }
            }

            if (op != null)
            {
                recovered++;
                level.layers.Add(new LevelLayer
                {
                    loadOperation = op,
                    expectedScenePath = layer.expectedScenePath,
                    expectedSceneName = layer.expectedSceneName,
                    additive = true
                });
                GameDebug.Log("Scene recovery queued additive scene: path=" + layer.expectedScenePath + ", name=" + layer.expectedSceneName);
            }
            else
            {
                GameDebug.Log("Scene recovery failed to queue additive scene: path=" + layer.expectedScenePath + ", name=" + layer.expectedSceneName);
            }
        }

        if (recovered > 0)
        {
            GameDebug.Log("Scene recovery queued " + recovered + " additive scene(s) for level " + level.name + ". Waiting for completion...");
            return true;
        }

        return false;
    }

    bool IsExpectedSceneLoaded(string expectedPath, string expectedName)
    {
        var normalizedExpectedPath = string.IsNullOrEmpty(expectedPath) ? string.Empty : expectedPath.Replace('\\', '/');
        var normalizedExpectedName = string.IsNullOrEmpty(expectedName) ? string.Empty : expectedName.ToLowerInvariant();

        var sceneCount = SceneManager.sceneCount;
        for (var i = 0; i < sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            var scenePath = string.IsNullOrEmpty(scene.path) ? string.Empty : scene.path.Replace('\\', '/');
            if (!string.IsNullOrEmpty(normalizedExpectedPath) &&
                string.Equals(scenePath, normalizedExpectedPath, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrEmpty(normalizedExpectedName) &&
                string.Equals(scene.name, normalizedExpectedName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    void LogLoadedScenes()
    {
        var sceneCount = SceneManager.sceneCount;
        var activeScene = SceneManager.GetActiveScene();
        GameDebug.Log("Loaded scenes summary: count=" + sceneCount + ", active=" + activeScene.name + " (" + activeScene.path + ")");
        for (var i = 0; i < sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid())
                continue;

            GameDebug.Log("Loaded scene[" + i + "]: name=" + scene.name + ", path=" + scene.path + ", loaded=" + scene.isLoaded + ", roots=" + scene.rootCount);
        }
    }


    // TODO (petera) this code was moved here to make it available outside of editor - until we start cooking for client/server
    public enum BuildType
    {
        Default,
        Client,
        Server,
    }

    public static void StripCode(BuildType buildType, bool isDevelopmentBuild)
    {
        GameDebug.Log("Stripping code for " + buildType.ToString() + " (" + (isDevelopmentBuild ? "DevBuild" : "NonDevBuild") + ")");

        if (buildType == BuildType.Client && isDevelopmentBuild && debugClientStrip.IntValue == 0)
        {
            GameDebug.Log("Stripping skipped for Client DevBuild (debug.clientstrip=0)");
            return;
        }

        var deleteBehaviors = new List<MonoBehaviour>();
        var deleteGameObjects = new List<GameObject>();

        foreach (var behavior in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
        {
            if (behavior.GetType().GetCustomAttributes(typeof(EditorOnlyComponentAttribute), false).Length > 0)
                deleteBehaviors.Add(behavior);
            else if (behavior.GetType().GetCustomAttributes(typeof(EditorOnlyGameObjectAttribute), false).Length > 0)
                deleteGameObjects.Add(behavior.gameObject);
            else if (buildType == BuildType.Server && behavior.GetType().GetCustomAttributes(typeof(ClientOnlyComponentAttribute), false).Length > 0)
                deleteBehaviors.Add(behavior);
            else if (buildType == BuildType.Client && behavior.GetType().GetCustomAttributes(typeof(ServerOnlyComponentAttribute), false).Length > 0)
                deleteBehaviors.Add(behavior);
            else if (!isDevelopmentBuild && behavior.GetType().GetCustomAttributes(typeof(DevelopmentOnlyComponentAttribute), false).Length > 0)
                deleteBehaviors.Add(behavior);
        }

        GameDebug.Log(string.Format("Stripping {0} game object(s) and {1} behavior(s)", deleteGameObjects.Count, deleteBehaviors.Count));

        for (var i = 0; i < deleteGameObjects.Count; i++)
        {
            var go = deleteGameObjects[i];
            if (go == null)
                continue;

            var sceneName = go.scene.IsValid() ? go.scene.name : "<no-scene>";
            GameDebug.Log("Strip GO: scene=" + sceneName + ", path=" + GetHierarchyPath(go.transform));
        }

        for (var i = 0; i < deleteBehaviors.Count; i++)
        {
            var behavior = deleteBehaviors[i];
            if (behavior == null)
                continue;

            var go = behavior.gameObject;
            var sceneName = go != null && go.scene.IsValid() ? go.scene.name : "<no-scene>";
            var path = go != null ? GetHierarchyPath(go.transform) : "<no-gameobject>";
            GameDebug.Log("Strip MB: type=" + behavior.GetType().Name + ", scene=" + sceneName + ", path=" + path);
        }

        foreach (var gameObject in deleteGameObjects)
            UnityEngine.Object.DestroyImmediate(gameObject);

        foreach (var behavior in deleteBehaviors)
            UnityEngine.Object.DestroyImmediate(behavior);
    }

    static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
            return "<null>";

        var path = transform.name;
        var current = transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
