using System.Collections.Generic;

public enum ContentResolverBackend
{
    Legacy,
    LoadableIndexed
}

public static class ContentResolverFactory
{
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

    public static IContentResolver Create(GameWorld world, string registryName, ContentResolverBackend backend, IEnumerable<ContentRootAsset> roots = null)
    {
        if (backend == ContentResolverBackend.LoadableIndexed)
        {
            var legacyFallback = new LegacyContentResolver(world, registryName);
            return new LoadableIndexedContentResolver(world, roots, legacyFallback);
        }

        return new LegacyContentResolver(world, registryName);
    }
}
