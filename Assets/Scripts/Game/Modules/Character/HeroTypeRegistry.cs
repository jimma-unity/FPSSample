using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "FPS Sample/Hero/HeroTypeRegistry", fileName = "HeroTypeRegistry")]
public class HeroTypeRegistry : RegistryBase
{
    public List<HeroTypeAsset> entries = new List<HeroTypeAsset>();

    public bool TryGetCharacterPrefab(int heroIndex, bool server, out GameObject characterPrefab)
    {
        characterPrefab = null;
        if (heroIndex < 0 || heroIndex >= entries.Count)
            return false;

        var characterType = entries[heroIndex].character;
        if (characterType == null)
            return false;

        return server
            ? LoadableAssetResolver.TryResolve(characterType.prefabServerLoadable, out characterPrefab)
            : LoadableAssetResolver.TryResolve(characterType.prefabClientLoadable, out characterPrefab);
    }

    public bool TryGetItemPrefab(int heroIndex, int itemIndex, bool server, out GameObject itemPrefab)
    {
        itemPrefab = null;
        if (heroIndex < 0 || heroIndex >= entries.Count)
            return false;

        var items = entries[heroIndex].items;
        if (items == null || itemIndex < 0 || itemIndex >= items.Length)
            return false;

        var itemType = items[itemIndex].itemType;
        if (itemType == null)
            return false;

        return server
            ? LoadableAssetResolver.TryResolve(itemType.prefabServerLoadable, out itemPrefab)
            : LoadableAssetResolver.TryResolve(itemType.prefabClientLoadable, out itemPrefab);
    }

    public bool TryGetAbilitiesFactory(int heroIndex, out ReplicatedEntityFactory abilitiesFactory)
    {
        abilitiesFactory = null;
        if (heroIndex < 0 || heroIndex >= entries.Count)
            return false;

        if (!LoadableAssetResolver.TryResolve(entries[heroIndex].abilitiesLoadable, out ScriptableObject asset))
            return false;

        abilitiesFactory = asset as ReplicatedEntityFactory;
        return abilitiesFactory != null;
    }
    
#if UNITY_EDITOR
    
    public override void PrepareForBuild()
    {
        Debug.Log("HeroTypeRegistry"); 

        entries.Clear();
        var guids = AssetDatabase.FindAssets("t:HeroTypeAsset");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var definition = AssetDatabase.LoadAssetAtPath<HeroTypeAsset>(path);
            Debug.Log("   Adding definition:" + definition);
            entries.Add(definition);
        }
        
        EditorUtility.SetDirty(this);
    }
    
    public override void GetSingleAssetGUIDs(List<string> guids, bool serverBuild)
    {
        foreach (var setup in entries)
        {
            foreach(var item in setup.items)
            {
                if (serverBuild && item.itemType.prefabServer.IsSet())
                    guids.Add(item.itemType.prefabServer.GetGuidStr());
                if (!serverBuild && item.itemType.prefabClient.IsSet())
                    guids.Add(item.itemType.prefabClient.GetGuidStr());
                if (!serverBuild && item.itemType.prefab1P.IsSet())
                    guids.Add(item.itemType.prefab1P.GetGuidStr());
            }
        }
    }
#endif
}

