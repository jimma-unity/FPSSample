using System;
using System.Collections.Generic;
using UnityEngine;

public enum ContentResolverBackend
{
    Legacy,
    LoadableIndexed
}

public static class ContentResolverFactory
{
    const string RuntimeResourcesRoot = "ContentDirectoryRuntimeGenerated";
    const string ClientContentRootResource = RuntimeResourcesRoot + "/ClientContentRoot";
    const string ServerContentRootResource = RuntimeResourcesRoot + "/ServerContentRoot";

    [ConfigVar(Name = "res.contentdirectoryonly", DefaultValue = "0", Description = "Use content-directory artifacts only and disable legacy bundle fallback (0=off, 1=on)")]
    public static ConfigVar contentDirectoryOnly;

    public static bool UseContentDirectoryOnlyMode()
    {
        return contentDirectoryOnly != null && contentDirectoryOnly.IntValue > 0;
    }

    public static ContentResolverBackend ResolveConfiguredBackend()
    {
        if (Game.contentResolverBackend != null)
        {
            var value = Game.contentResolverBackend.IntValue;
            if (value == (int)ContentResolverBackend.LoadableIndexed)
                return ContentResolverBackend.LoadableIndexed;
        }

        return ContentResolverBackend.Legacy;
    }

    public static IContentResolver Create(GameWorld world, string registryName, IEnumerable<ContentRootAsset> roots = null)
    {
        return Create(world, registryName, ResolveConfiguredBackend(), roots);
    }

    static List<ContentRootAsset> ResolveRuntimeRoots(string registryName, IEnumerable<ContentRootAsset> roots)
    {
        var resolved = new List<ContentRootAsset>();

        if (roots != null)
        {
            foreach (var root in roots)
            {
                if (root == null)
                    continue;

                resolved.Add(root);
            }

            if (resolved.Count > 0)
                return resolved;
        }

        var loadClientRoot = string.IsNullOrWhiteSpace(registryName) ||
                             registryName.IndexOf("/Client", StringComparison.OrdinalIgnoreCase) >= 0;
        var loadServerRoot = string.IsNullOrWhiteSpace(registryName) ||
                             registryName.IndexOf("/Server", StringComparison.OrdinalIgnoreCase) >= 0;

        if (TryLoadAndAddRootsFromRegisteredContentDirectories(loadClientRoot, loadServerRoot, resolved))
            return resolved;

        if (loadClientRoot)
            TryLoadAndAddRoot(ClientContentRootResource, resolved);
        if (loadServerRoot)
            TryLoadAndAddRoot(ServerContentRootResource, resolved);

        if (resolved.Count == 0)
        {
            var fallbackRoots = Resources.LoadAll<ContentRootAsset>(RuntimeResourcesRoot);
            if (fallbackRoots != null)
            {
                for (var i = 0; i < fallbackRoots.Length; i++)
                {
                    if (fallbackRoots[i] != null)
                        resolved.Add(fallbackRoots[i]);
                }
            }
        }

        return resolved;
    }

    static bool TryLoadAndAddRootsFromRegisteredContentDirectories(bool loadClientRoot, bool loadServerRoot, List<ContentRootAsset> roots)
    {
        if (roots == null)
            return false;

        if (!LoadableAssetResolver.TryGetRootAssets<ContentRootAsset>(out var runtimeRoots) || runtimeRoots == null || runtimeRoots.Length == 0)
            return false;

        var added = 0;
        for (var i = 0; i < runtimeRoots.Length; i++)
        {
            var root = runtimeRoots[i];
            if (root == null)
                continue;

            var rootName = root.name ?? string.Empty;
            var isClientRoot = rootName.IndexOf("Client", StringComparison.OrdinalIgnoreCase) >= 0;
            var isServerRoot = rootName.IndexOf("Server", StringComparison.OrdinalIgnoreCase) >= 0;

            if (loadClientRoot && !loadServerRoot && isServerRoot)
                continue;

            if (loadServerRoot && !loadClientRoot && isClientRoot)
                continue;

            if (roots.Contains(root))
                continue;

            roots.Add(root);
            added++;
        }

        return added > 0;
    }

    static void TryLoadAndAddRoot(string resourcePath, List<ContentRootAsset> roots)
    {
        if (roots == null)
            return;

        var root = Resources.Load<ContentRootAsset>(resourcePath);
        if (root != null)
            roots.Add(root);
    }

    public static IContentResolver Create(GameWorld world, string registryName, ContentResolverBackend backend, IEnumerable<ContentRootAsset> roots = null)
    {
        var strictContentDirectoryOnly = UseContentDirectoryOnlyMode();

        if (strictContentDirectoryOnly && backend == ContentResolverBackend.Legacy)
        {
            GameDebug.LogWarning("ContentResolverFactory: strict content-directory mode enabled; overriding legacy backend to LoadableIndexed.");
            backend = ContentResolverBackend.LoadableIndexed;
        }

        if (backend == ContentResolverBackend.LoadableIndexed)
        {
            var resolvedRoots = ResolveRuntimeRoots(registryName, roots);
            IContentResolver legacyFallback = null;
            if (!strictContentDirectoryOnly)
                legacyFallback = new LegacyContentResolver(world, registryName);

            if (resolvedRoots.Count == 0)
                GameDebug.LogWarning("ContentResolverFactory: no ContentRootAsset roots resolved for registry '" + registryName + "'.");

            GameDebug.Log("ContentResolverFactory: backend=LoadableIndexed, strictContentDirectoryOnly=" + strictContentDirectoryOnly + ", roots=" + resolvedRoots.Count + ", registry=" + registryName);
            return new LoadableIndexedContentResolver(world, resolvedRoots, legacyFallback);
        }

        return new LegacyContentResolver(world, registryName);
    }
}
