using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BundledResourceManager : IContentResolver {      

    [ConfigVar(Name = "resources.forcebundles", DefaultValue = "0", Description = "Force use of bundles even in editor")]
    static ConfigVar forceBundles;

    [ConfigVar(Name = "resources.verbose", DefaultValue = "0", Description = "Verbose logging about resources")]
    static ConfigVar verbose;

    private string GetBundlePath()
    {
#if UNITY_PS4
        return Application.streamingAssetsPath + "/" + SimpleBundleManager.assetBundleFolder;
#else
        string bundlePath = SimpleBundleManager.GetRuntimeBundlePath();
        return bundlePath;
#endif
    }
                                 
    public BundledResourceManager(GameWorld world, string registryName)
    {
        bool useBundles = !Application.isEditor || forceBundles.IntValue > 0;

        m_world = world;

#if UNITY_EDITOR
        if (!useBundles)
        {
            string assetPath = "Assets/" + registryName + ".asset";
            m_assetRegistryRoot = AssetDatabase.LoadAssetAtPath<AssetRegistryRoot>(assetPath);
            if (verbose.IntValue > 0)
                GameDebug.Log("resource: loading resource: " + assetPath);
        }
#endif

        if (useBundles)
        {
            var bundlePath = GetBundlePath();
            var registryPathOriginal = registryName.Replace('\\', '/');
            var registryPathLower = registryPathOriginal.ToLowerInvariant();

            var candidateRegistryPaths = new List<string> { registryPathLower };
            if (!string.Equals(registryPathOriginal, registryPathLower))
                candidateRegistryPaths.Add(registryPathOriginal);

            string selectedRegistryPath = null;
            foreach (var candidateRegistryPath in candidateRegistryPaths)
            {
                var assetPath = bundlePath + "/" + candidateRegistryPath;
                if (verbose.IntValue > 0)
                    GameDebug.Log("resource: loading bundle (" + assetPath + ")");

                m_assetRegistryRootBundle = AssetBundle.LoadFromFile(assetPath);
                if (m_assetRegistryRootBundle != null)
                {
                    selectedRegistryPath = candidateRegistryPath;
                    break;
                }
            }

            if (m_assetRegistryRootBundle == null)
            {
                GameDebug.LogError("Failed to load registry bundle: " + bundlePath + "/" + registryPathLower);
                if (!string.Equals(registryPathOriginal, registryPathLower))
                    GameDebug.LogError("Also attempted registry bundle path: " + bundlePath + "/" + registryPathOriginal);
                return;
            }

            var registryRoots = m_assetRegistryRootBundle.LoadAllAssets<AssetRegistryRoot>();

            if(registryRoots.Length == 1)
                m_assetRegistryRoot = registryRoots[0];
            else
                GameDebug.LogError("Wrong number(" + registryRoots.Length + ") of registry roots in "+ registryName);

            m_assetResourceFolder = selectedRegistryPath + "_assets";
            m_fallbackAssetResourceFolder = selectedRegistryPath + "_Assets";
        }

        // Update asset registry map
        if(m_assetRegistryRoot != null)
        {
            foreach(var registry in m_assetRegistryRoot.assetRegistries)
            {
                if(registry == null)
                {
                    continue;
                }

                System.Type type = registry.GetType();
                m_assetRegistryMap.Add(type, registry);
            }
            if (!useBundles)
                m_assetResourceFolder = registryName + "_Assets";
        }
    }

    public void Shutdown()
    {
        foreach(var bundle in m_resources.Values)
        {
            // If we are in editor we may not have loaded these as bundles
            if(bundle.bundle != null)
                bundle.bundle.Unload(false);
        }

        if(m_assetRegistryRootBundle != null)
            m_assetRegistryRootBundle.Unload(false);      

        m_assetRegistryRoot = null;
        m_assetRegistryRootBundle = null;
        m_assetRegistryMap.Clear();
        m_assetResourceFolder = "";
        m_resources.Clear();
    }
    

    public T GetResourceRegistry<T>() where T : ScriptableObject
    {
        ScriptableObject result = null;
        m_assetRegistryMap.TryGetValue(typeof(T), out result);
        return (T)result;
    }

    public Entity CreateEntity(string guid)
    {
        if (guid == null || guid == "")
        {
            GameDebug.LogError("Guid invalid");
            return Entity.Null;
        }
        
        var reference = new WeakAssetReference(guid);
        return CreateEntity(reference);
    }
    
    public Entity CreateEntity(WeakAssetReference assetGuid)
    {
        var resource = GetSingleAssetResource(assetGuid);
        if (resource == null)
            return Entity.Null;

        var prefab = resource as GameObject;
        if (prefab != null)
        {
            var gameObjectEntity = m_world.Spawn<GameObjectEntity>(prefab);
            return gameObjectEntity.Entity;
        }

        var factory = resource as ReplicatedEntityFactory;
        if (factory != null)
        {
            return factory.Create(m_world.GetEntityManager(), this, m_world);
        }

        return Entity.Null;
    }

//    public Object LoadSingleAssetResource(string guid)
//    {
//        if (guid == null || guid == "")
//        {
//            GameDebug.LogError("Guid invalid");
//            return null;
//        }
//        
//        var reference = new WeakAssetReference(guid);
//        return GetSingleAssetResource(reference);
//    }
    
    public Object GetSingleAssetResource(WeakAssetReference reference)        
    {
        
        var def = new SingleResourceBundle();

        if(m_resources.TryGetValue(reference, out def)) {
            return def.asset;
        }

        def = new SingleResourceBundle();
        var useBundles = !Application.isEditor || forceBundles.IntValue > 0;

        var guidStr = reference.GetGuidStr();
        
#if UNITY_EDITOR
        if(!useBundles)
        {
            var path = AssetDatabase.GUIDToAssetPath(guidStr);

            def.asset = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));

            if (def.asset == null)
                GameDebug.LogWarning("Failed to load resource " + guidStr + " at " + path);
            if (verbose.IntValue > 0)
                GameDebug.Log("resource: loading non-bundled asset " + path + "(" + guidStr + ")");
        }
#endif
        if(useBundles)
        {
            var bundlePath = GetBundlePath();
            var primaryBundlePath = bundlePath + "/" + m_assetResourceFolder + "/" + guidStr;
            def.bundle = AssetBundle.LoadFromFile(primaryBundlePath);

            if (def.bundle == null && !string.IsNullOrEmpty(m_fallbackAssetResourceFolder))
            {
                var fallbackBundlePath = bundlePath + "/" + m_fallbackAssetResourceFolder + "/" + guidStr;
                def.bundle = AssetBundle.LoadFromFile(fallbackBundlePath);
            }

            if (verbose.IntValue > 0)
                GameDebug.Log("resource: loading bundled asset: " + m_assetResourceFolder + "/" + guidStr);

            if (def.bundle == null)
            {
                GameDebug.LogWarning("Failed to load bundled asset bundle for guid " + guidStr + " from root " + bundlePath + "/" + m_assetResourceFolder);
                m_resources.Add(reference, def);
                return null;
            }

            var handles = def.bundle.LoadAllAssets();
            if (handles.Length > 0)
                def.asset = handles[0];
            else
                GameDebug.LogWarning("Failed to load resource " + guidStr);
        }

        m_resources.Add(reference, def);
        return def.asset;
    }

    public bool TryResolveAsset(WeakAssetReference reference, out Object asset)
    {
        asset = GetSingleAssetResource(reference);
        return asset != null;
    }

    class SingleResourceBundle
    {
        public AssetBundle bundle;
        public Object asset;
    }

    GameWorld m_world;
    AssetRegistryRoot m_assetRegistryRoot;
    AssetBundle m_assetRegistryRootBundle;
    Dictionary<System.Type, ScriptableObject> m_assetRegistryMap = new Dictionary<System.Type, ScriptableObject>();
    string m_assetResourceFolder = "";
    string m_fallbackAssetResourceFolder = "";

    Dictionary<WeakAssetReference, SingleResourceBundle> m_resources = new Dictionary<WeakAssetReference, SingleResourceBundle>();
}
