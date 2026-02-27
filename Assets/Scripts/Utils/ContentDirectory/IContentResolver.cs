using Unity.Entities;
using UnityEngine;

public interface IContentResolver
{
    T GetResourceRegistry<T>() where T : ScriptableObject;
    Object GetSingleAssetResource(WeakAssetReference reference);
    bool TryResolveAsset(WeakAssetReference reference, out Object asset);
    Entity CreateEntity(WeakAssetReference assetGuid);
    Entity CreateEntity(string guid);
    void Shutdown();
}
