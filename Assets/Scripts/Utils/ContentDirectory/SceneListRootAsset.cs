using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Loading;

[CreateAssetMenu(fileName = "SceneListRoot", menuName = "FPS Sample/Content/Scene List Root")]
public class SceneListRootAsset : ScriptableObject
{
    public const string DefaultResourcePath = "Content/SceneListRoot";

    [Serializable]
    public struct SceneEntry
    {
        public string key;
        public LoadableScene mainScene;
        public string mainScenePath;
        public List<LoadableScene> additiveScenes;
        public List<string> additiveScenePaths;
    }

    public List<SceneEntry> scenes = new List<SceneEntry>();

    public static SceneListRootAsset LoadDefault()
    {
        var root = Resources.Load<SceneListRootAsset>(DefaultResourcePath);
        if (root != null)
            return root;

        var fallbacks = Resources.LoadAll<SceneListRootAsset>(string.Empty);
        return fallbacks != null && fallbacks.Length > 0 ? fallbacks[0] : null;
    }

    public bool TryGetScene(string key, out SceneEntry entry)
    {
        for (var i = 0; i < scenes.Count; i++)
        {
            if (string.Equals(scenes[i].key, key, StringComparison.OrdinalIgnoreCase))
            {
                entry = scenes[i];
                return true;
            }
        }

        entry = default;
        return false;
    }
}