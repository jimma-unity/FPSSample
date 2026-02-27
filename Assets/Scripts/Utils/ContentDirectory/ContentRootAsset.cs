using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Loading;

[CreateAssetMenu(fileName = "ContentRoot", menuName = "FPS Sample/Content/Root Asset")]
public class ContentRootAsset : ScriptableObject
{
    [Header("Legacy registry roots (temporary compatibility)")]
    public AssetRegistryRoot legacyRegistryRoot; // optional while migrating

    [Header("Scenes / Levels")]
    public List<LevelEntry> levels = new();

    [Header("Replicated Entities")]
    public List<ReplicatedEntityEntry> replicatedEntities = new();

    [Header("Characters / Heroes")]
    public List<CharacterEntry> characters = new();
    public List<HeroEntry> heroes = new();

    [Header("Projectiles")]
    public List<ProjectileEntry> projectiles = new();

    [Header("Presentation")]
    public List<PresentationEntry> presentations = new();

    [Serializable]
    public struct LevelEntry
    {
        public string key; // e.g. "level_00"
        public LoadableScene scene;
    }

    [Serializable]
    public struct ReplicatedEntityEntry
    {
        public string guidKey; // keep GUID string for lookup bridge
        public Loadable<GameObject> prefabLoadable;
        // New: for entries that are ReplicatedEntityFactory assets
        public Loadable<ScriptableObject> factoryLoadable;
        public WeakAssetReference legacyGuid; // temporary
    }

    [Serializable]
    public struct CharacterEntry
    {
        public string guidKey;
        public Loadable<GameObject> serverPrefab;
        public Loadable<GameObject> clientPrefab;
        public Loadable<GameObject> firstPersonPrefab;
        public WeakAssetReference legacyServerPrefab; // temporary
        public WeakAssetReference legacyClientPrefab; // temporary
        public WeakAssetReference legacyFirstPersonPrefab; // temporary
    }

    [Serializable]
    public struct HeroEntry
    {
        public string key; // hero name or id
        public Loadable<ScriptableObject> heroAsset; // replace with concrete HeroTypeAsset if available
        public WeakAssetReference legacyHeroGuid; // temporary
    }

    [Serializable]
    public struct ProjectileEntry
    {
        public string guidKey;
        public Loadable<GameObject> clientProjectilePrefab;
        public WeakAssetReference legacyProjectileGuid; // temporary
    }

    [Serializable]
    public struct PresentationEntry
    {
        public string ownerGuidKey; // owner asset guid as string
        public Loadable<GameObject> presentationPrefab;
        public WeakAssetReference legacyPresentationGuid; // temporary
    }
}