#if UNITY_EDITOR && FPS_API_PROBE
using UnityEditor;
using UnityEngine;
using UnityEngine.Loading;
using System.Linq;

public static class ApiProbe
{
    [MenuItem("FPS Sample/Diagnostics/Probe ContentDirectory API")]
    public static void Probe()
    {
        var hasBuildContentDirectory = typeof(BuildPipeline)
            .GetMethods()
            .Any(m => m.Name == "BuildContentDirectory");

        Debug.Log($"BuildContentDirectory: {hasBuildContentDirectory}");
        Debug.Log($"ContentLoadManager: {typeof(ContentLoadManager) != null}");
        Debug.Log($"LoadableScene: {typeof(LoadableScene) != null}");
    }
}
#endif