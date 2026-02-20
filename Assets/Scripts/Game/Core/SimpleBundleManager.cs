using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class SimpleBundleManager
{
    public static string assetBundleFolder = "AssetBundles";

    public static string GetRuntimeBundlePath()
    {
#if UNITY_PS5 || UNITY_GAMECORE_XBOXSERIES
        return Application.streamingAssetsPath + "/" + assetBundleFolder;
#elif UNITY_STANDALONE_OSX ||  UNITY_EDITOR_OSX
        if (Application.isEditor)
            return Application.dataPath + "/../Autobuild/AutoBuild.app/Contents/" + assetBundleFolder;
        else
            return Application.dataPath + "/" + assetBundleFolder;
#else
        if (Application.isEditor)
        {
            var projectToolsBundlePath = "Autobuild/Autobuild_Data/" + assetBundleFolder;
            if (Directory.Exists(projectToolsBundlePath))
                return projectToolsBundlePath;

            var legacyBundlePath = "AutoBuild/" + assetBundleFolder;
            if (Directory.Exists(legacyBundlePath))
                return legacyBundlePath;

            return projectToolsBundlePath;
        }
        else
            return m_runtimeBundlePath.Value;
#endif
    }

    public static void Init()
    {
    }

    public static AssetBundle LoadLevelAssetBundle(string name)
    {
        var bundle_pathname = GetRuntimeBundlePath() + "/" + name;

        GameDebug.Log("loading:" + bundle_pathname);

        var cacheKey = name.ToLower();

        AssetBundle result;
        if (!m_levelBundles.TryGetValue(cacheKey, out result))
        {
            result = AssetBundle.LoadFromFile(bundle_pathname);
            if (result != null)
                m_levelBundles.Add(cacheKey, result);
        }

        return result;
    }

    public static void ReleaseLevelAssetBundle(string name)
    {
        // TODO (petera) : Implement unloading of asset bundles. Ideally not by name.
    }

    static Dictionary<string, AssetBundle> m_levelBundles = new Dictionary<string, AssetBundle>();

    [ConfigVar(Name = "res.runtimebundlepath", DefaultValue = "AssetBundles", Description = "Asset bundle folder", Flags = ConfigVar.Flags.ServerInfo)]
    public static ConfigVar m_runtimeBundlePath;


}
