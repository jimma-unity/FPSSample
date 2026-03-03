using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class LoadableIndexedContentResolver : IContentResolver
{
    sealed class IndexedEntry
    {
        public object loadable;
        public Type expectedType;
        public string source;
    }

    readonly GameWorld m_world;
    readonly List<ContentRootAsset> m_roots = new List<ContentRootAsset>();
    readonly Dictionary<WeakAssetReference, IndexedEntry> m_legacyGuidToEntry = new Dictionary<WeakAssetReference, IndexedEntry>();
    readonly IContentResolver m_fallback;
    static bool s_loadableApiWarningLogged;

    public LoadableIndexedContentResolver(GameWorld world, IEnumerable<ContentRootAsset> roots, IContentResolver fallback = null)
    {
        m_world = world;
        m_fallback = fallback;

        if (roots == null)
            return;

        foreach (var root in roots)
        {
            if (root == null)
                continue;

            m_roots.Add(root);
            IndexRoot(root);
        }
    }

    public T GetResourceRegistry<T>() where T : ScriptableObject
    {
        if (typeof(T) == typeof(ContentRootAsset) && m_roots.Count > 0)
            return m_roots[0] as T;

        if (m_fallback != null)
            return m_fallback.GetResourceRegistry<T>();

        return null;
    }

    public Object GetSingleAssetResource(WeakAssetReference reference)
    {
        if (TryResolveAsset(reference, out var asset))
            return asset;

        return null;
    }

    public bool TryResolveAsset(WeakAssetReference reference, out Object asset)
    {
        if (m_legacyGuidToEntry.TryGetValue(reference, out var entry) && TryResolveIndexedEntry(entry, out asset))
            return true;

        if (m_fallback != null)
            return m_fallback.TryResolveAsset(reference, out asset);

        asset = null;
        return false;
    }

    public Entity CreateEntity(WeakAssetReference assetGuid)
    {
        if (m_fallback != null && m_fallback.TryResolveAsset(assetGuid, out _))
            return m_fallback.CreateEntity(assetGuid);

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
            if (m_fallback != null)
                return m_fallback.CreateEntity(assetGuid);

            GameDebug.LogWarning("LoadableIndexedContentResolver: Factory-based entity creation requires a fallback resolver.");
            return Entity.Null;
        }

        return Entity.Null;
    }

    public Entity CreateEntity(string guid)
    {
        if (string.IsNullOrWhiteSpace(guid))
            return Entity.Null;

        return CreateEntity(new WeakAssetReference(guid));
    }

    public void Shutdown()
    {
        m_fallback?.Shutdown();
        m_legacyGuidToEntry.Clear();
        m_roots.Clear();
    }

    void IndexRoot(ContentRootAsset root)
    {
        for (var i = 0; i < root.replicatedEntities.Count; i++)
        {
            var entry = root.replicatedEntities[i];
            var legacyRef = ResolveLegacyReference(entry.guidKey, entry.legacyGuid);
            AddEntry(legacyRef, entry.prefabLoadable, typeof(GameObject), root.name + ".replicatedEntities.prefabLoadable");
            AddEntry(legacyRef, entry.factoryLoadable, typeof(ScriptableObject), root.name + ".replicatedEntities.factoryLoadable");
        }

        for (var i = 0; i < root.characters.Count; i++)
        {
            var entry = root.characters[i];
            AddEntry(entry.legacyServerPrefab, entry.serverPrefab, typeof(GameObject), root.name + ".characters.serverPrefab");
            AddEntry(entry.legacyClientPrefab, entry.clientPrefab, typeof(GameObject), root.name + ".characters.clientPrefab");
            AddEntry(entry.legacyFirstPersonPrefab, entry.firstPersonPrefab, typeof(GameObject), root.name + ".characters.firstPersonPrefab");
        }

        for (var i = 0; i < root.heroes.Count; i++)
        {
            var entry = root.heroes[i];
            AddEntry(entry.legacyHeroGuid, entry.heroAsset, typeof(ScriptableObject), root.name + ".heroes.heroAsset");
        }

        for (var i = 0; i < root.projectiles.Count; i++)
        {
            var entry = root.projectiles[i];
            AddEntry(ResolveLegacyReference(entry.guidKey, entry.legacyProjectileGuid), entry.clientProjectilePrefab, typeof(GameObject), root.name + ".projectiles.clientProjectilePrefab");
        }

        for (var i = 0; i < root.presentations.Count; i++)
        {
            var entry = root.presentations[i];
            AddEntry(ResolveLegacyReference(entry.ownerGuidKey, entry.legacyPresentationGuid), entry.presentationPrefab, typeof(GameObject), root.name + ".presentations.presentationPrefab");
        }
    }

    static WeakAssetReference ResolveLegacyReference(string guidKey, WeakAssetReference fallback)
    {
        if (!string.IsNullOrWhiteSpace(guidKey))
            return new WeakAssetReference(guidKey);

        return fallback;
    }

    void AddEntry<T>(WeakAssetReference legacyGuid, T loadable, Type expectedType, string source)
    {
        if (!legacyGuid.IsSet())
            return;

        var boxed = (object)loadable;
        if (boxed == null)
            return;

        var loadableType = boxed.GetType();
        if (loadableType.IsValueType)
        {
            var defaultValue = Activator.CreateInstance(loadableType);
            if (boxed.Equals(defaultValue))
                return;
        }

        m_legacyGuidToEntry[legacyGuid] = new IndexedEntry
        {
            loadable = boxed,
            expectedType = expectedType,
            source = source
        };
    }

    bool TryResolveIndexedEntry(IndexedEntry entry, out Object asset)
    {
        if (entry == null || entry.loadable == null)
        {
            asset = null;
            return false;
        }

        if (TryResolveFromLoadableObject(entry.loadable, out asset))
        {
            if (asset == null)
                return false;

            if (entry.expectedType == null || entry.expectedType.IsAssignableFrom(asset.GetType()))
                return true;
        }

        return false;
    }

    static bool TryResolveFromLoadableObject(object loadable, out Object asset)
    {
        asset = null;
        if (loadable == null)
            return false;

        if (TryInvokeInstanceGetter(loadable, out asset))
            return true;

        if (TryInvokeContentLoadManager(loadable, out asset))
            return true;

        if (!s_loadableApiWarningLogged)
        {
            s_loadableApiWarningLogged = true;
            GameDebug.LogWarning("LoadableIndexedContentResolver: Unable to resolve Loadable<T> via reflection. Falling back to legacy resolver when available.");
        }

        return false;
    }

    static bool TryInvokeInstanceGetter(object loadable, out Object asset)
    {
        asset = null;
        var type = loadable.GetType();

        var propertyNames = new[] { "Asset", "Value", "Result", "Object" };
        for (var i = 0; i < propertyNames.Length; i++)
        {
            var property = type.GetProperty(propertyNames[i], BindingFlags.Instance | BindingFlags.Public);
            if (property == null || property.GetIndexParameters().Length > 0)
                continue;

            var value = property.GetValue(loadable, null) as Object;
            if (value != null)
            {
                asset = value;
                return true;
            }
        }

        var methodNames = new[] { "Load", "Resolve", "Get" };
        for (var i = 0; i < methodNames.Length; i++)
        {
            var method = type.GetMethod(methodNames[i], BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
            if (method == null)
                continue;

            var value = method.Invoke(loadable, null) as Object;
            if (value != null)
            {
                asset = value;
                return true;
            }
        }

        return false;
    }

    static bool TryInvokeContentLoadManager(object loadable, out Object asset)
    {
        asset = null;
        var contentLoadManagerType = Type.GetType("UnityEngine.Loading.ContentLoadManager, UnityEngine.CoreModule");
        if (contentLoadManagerType == null)
            return false;

        var methods = contentLoadManagerType.GetMethods(BindingFlags.Public | BindingFlags.Static);
        for (var i = 0; i < methods.Length; i++)
        {
            var method = methods[i];
            if (!string.Equals(method.Name, "Load", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(method.Name, "Resolve", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(method.Name, "Get", StringComparison.OrdinalIgnoreCase))
                continue;

            var parameters = method.GetParameters();
            if (parameters.Length != 1)
                continue;

            if (!parameters[0].ParameterType.IsInstanceOfType(loadable))
                continue;

            var value = method.Invoke(null, new[] { loadable }) as Object;
            if (value != null)
            {
                asset = value;
                return true;
            }
        }

        return false;
    }
}
