using System;
using UnityEditor;
using UnityEngine;

public static class VFXCustomSpawnerRepairTool
{
    static readonly string[] ReimportPaths =
    {
        "Assets/VFX/Script/CustomSpawners/CancelByDistance.cs",
        "Assets/VFX/Graphs/Environment/Environment_Tunnel_Digger_SideGrind.vfx",
        "Assets/VFX/Graphs/Weapons/Robot_Weapon_A_Impact_Generic.vfx",
        "Assets/VFX/Graphs/Weapons/Terraformer_Weapon_A_Impact_Generic.vfx"
    };

    [MenuItem("FPS Sample/VFX/Repair Custom Spawner Graphs")]
    public static void RepairCustomSpawnerGraphs()
    {
        Debug.Log("VFX repair: starting reimport of custom spawner script + affected graphs.");

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var path in ReimportPaths)
            {
                if (!AssetDatabase.AssetPathExists(path))
                {
                    Debug.LogWarning("VFX repair: missing asset path: " + path);
                    continue;
                }

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Debug.Log("VFX repair: reimported " + path);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.Refresh();

        Debug.Log("VFX repair: completed. If this still repros, switch to a non-alpha Unity 6 editor build or report against com.unity.visualeffectgraph 17.5.0.");
    }
}
