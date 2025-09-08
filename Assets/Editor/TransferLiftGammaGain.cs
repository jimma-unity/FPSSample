#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.PostProcessing;

public class LGGSceneVolumeTransfer : EditorWindow
{
    GameObject targetObject;

    [MenuItem("Tools/LGG Scene Volume Transfer")]
    public static void ShowWindow()
    {
        GetWindow<LGGSceneVolumeTransfer>("LGG Scene Volume Transfer");
    }

    void OnGUI()
    {
        GUILayout.Label("Copy Lift/Gamma/Gain from PPSv2 Volume to HDRP Volume", EditorStyles.boldLabel);

        targetObject = (GameObject)EditorGUILayout.ObjectField(
            "Target GameObject", targetObject, typeof(GameObject), true);

        EditorGUILayout.Space();

        GUI.enabled = targetObject != null;

        if (GUILayout.Button("Copy LGG Now"))
        {
            CopyLGG();
        }

        GUI.enabled = true;
    }

    void CopyLGG()
    {
        if (targetObject == null)
        {
            Debug.LogError("Assign a GameObject.");
            return;
        }

        // Get PPSv2 Volume
        var ppsVolume = targetObject.GetComponent<PostProcessVolume>();
        if (ppsVolume == null || ppsVolume.profile == null)
        {
            Debug.LogError("No PPSv2 PostProcessVolume with a profile on this GameObject.");
            return;
        }

        // Get HDRP Volume
        var hdrpVolume = targetObject.GetComponent<UnityEngine.Rendering.Volume>();
        if (hdrpVolume == null || hdrpVolume.sharedProfile == null)
        {
            Debug.LogError("No HDRP Volume with a profile on this GameObject.");
            return;
        }

        // Get ColorGrading from PPSv2
        if (!ppsVolume.profile.TryGetSettings<ColorGrading>(out var ppsColorGrading))
        {
            Debug.LogError("No ColorGrading in PPSv2 profile.");
            return;
        }

        // Get LiftGammaGain from HDRP
        if (!hdrpVolume.sharedProfile.TryGet<LiftGammaGain>(out var hdrpLGG))
        {
            Debug.LogError("No LiftGammaGain in HDRP profile. Add it as an override first.");
            return;
        }

        // Transfer
        Undo.RecordObject(hdrpVolume.sharedProfile, "Copy LGG values");
        hdrpLGG.lift.value = ppsColorGrading.lift.value;
        hdrpLGG.lift.overrideState = ppsColorGrading.lift.overrideState;
        hdrpLGG.gamma.value = ppsColorGrading.gamma.value;
        hdrpLGG.gamma.overrideState = ppsColorGrading.gamma.overrideState;
        hdrpLGG.gain.value = ppsColorGrading.gain.value;
        hdrpLGG.gain.overrideState = ppsColorGrading.gain.overrideState;
        EditorUtility.SetDirty(hdrpVolume.sharedProfile);

        Debug.Log($"Lift, Gamma, Gain transferred from '{ppsVolume.profile.name}' to '{hdrpVolume.sharedProfile.name}'.");
    }
}
#endif
