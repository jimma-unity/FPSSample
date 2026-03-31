using UnityEngine;

public class CameraBackgroundSwitcher : MonoBehaviour
{
    public Camera targetCamera;
    public Color solidColor = Color.black;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    [ContextMenu("Use Solid Color")]
    public void UseSolidColor()
    {
        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = solidColor;
    }

    [ContextMenu("Use Skybox")]
    public void UseSkybox()
    {
        targetCamera.clearFlags = CameraClearFlags.Skybox;

        // Ensure a skybox is available
        if (UnityEngine.RenderSettings.skybox == null)
        {
            // Optionally assign a skybox material you have
            // RenderSettings.skybox = mySkyboxMaterial;
            Debug.LogWarning("No RenderSettings.skybox assigned. Add a Skybox Material to RenderSettings or a Skybox component on the camera.");
        }
    }

    [ContextMenu("Use Depth Only (Transparent)")]
    public void UseDepthOnly()
    {
        // Clears only the depth buffer; background remains whatever was already rendered behind
        targetCamera.clearFlags = CameraClearFlags.Depth;
    }

    [ContextMenu("Use Nothing (Don’t clear)")]
    public void UseNothing()
    {
        // Rarely recommended: can leave garbage pixels if nothing is rendered beforehand
        targetCamera.clearFlags = CameraClearFlags.Nothing;
    }
}
