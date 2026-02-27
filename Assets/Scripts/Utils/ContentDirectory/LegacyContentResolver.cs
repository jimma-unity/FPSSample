using Unity.Entities;
using UnityEngine;

public sealed class LegacyContentResolver : IContentResolver
{
    readonly BundledResourceManager m_resourceManager;

    public LegacyContentResolver(GameWorld world, string registryName)
    {
        m_resourceManager = new BundledResourceManager(world, registryName);
    }

    public LegacyContentResolver(BundledResourceManager resourceManager)
    {
        m_resourceManager = resourceManager;
    }

    public T GetResourceRegistry<T>() where T : ScriptableObject
    {
        return m_resourceManager.GetResourceRegistry<T>();
    }

    public Object GetSingleAssetResource(WeakAssetReference reference)
    {
        return m_resourceManager.GetSingleAssetResource(reference);
    }

    public bool TryResolveAsset(WeakAssetReference reference, out Object asset)
    {
        return m_resourceManager.TryResolveAsset(reference, out asset);
    }

    public Entity CreateEntity(WeakAssetReference assetGuid)
    {
        return m_resourceManager.CreateEntity(assetGuid);
    }

    public Entity CreateEntity(string guid)
    {
        return m_resourceManager.CreateEntity(guid);
    }

    public void Shutdown()
    {
        m_resourceManager.Shutdown();
    }
}
