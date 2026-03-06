using UnityEngine;
using UnityEditor;

public class RefreshLightProbesVolumes : EditorWindow
{
    [MenuItem("Lighting/Refresh lightprobes volumes")]
    static void Refresh()
    {
        var volumes = FindObjectsByType<LightProbesVolumeSettings>(FindObjectsInactive.Exclude);
        foreach (var volume in volumes)
        {
            volume.Populate();
        }
    }
}