using UnityEngine;
using UnityEngine.Rendering;

public class FrameTimeDisplay : MonoBehaviour
{
    [Range(0.0f, 1.0f)]
    public float smoothing = 0.1f;

    // Base sizes for ~1080p; they will be multiplied by a scale factor.
    public int baseFontSize   = 18;
    public int baseMargin     = 10;
    public int baseWidth      = 260;
    public int baseLineHeight = 20;

    private double mainThreadMs;
    private double renderThreadMs;
    private double gpuMs;
    private double fps;

    private FrameTiming[] frameTimings = new FrameTiming[1];

    void Update()
    {
        // Approximate main thread CPU time using deltaTime
        double currentMainMs = Time.deltaTime * 1000.0;
        mainThreadMs = Smooth(mainThreadMs, currentMainMs, smoothing);

        // FPS from deltaTime
        if (Time.deltaTime > 0f)
        {
            double currentFps = 1.0 / Time.deltaTime;
            fps = Smooth(fps, currentFps, smoothing);
        }

        // Issue timing collection for this frame
        FrameTimingManager.CaptureFrameTimings();
    }

    void LateUpdate()
    {
        // Try to get the latest frame timing data
        uint count = FrameTimingManager.GetLatestTimings(1, frameTimings);
        if (count > 0)
        {
            FrameTiming ft = frameTimings[0];

            double cpuFrameTimeMs = ft.cpuFrameTime;

            double cpuRenderMs = ft.cpuRenderThreadFrameTime > 0.0
                ? ft.cpuRenderThreadFrameTime
                : (cpuFrameTimeMs - mainThreadMs);

            double gpuFrameTimeMs = ft.gpuFrameTime;

            renderThreadMs = Smooth(renderThreadMs, cpuRenderMs, smoothing);
            gpuMs          = Smooth(gpuMs, gpuFrameTimeMs, smoothing);
        }
    }

    private double Smooth(double previous, double current, float factor)
    {
        if (previous <= 0.0)
            return current;
        return Mathf.Lerp((float)previous, (float)current, factor);
    }

    void OnGUI()
    {
        // Simple scale factor based on screen height; 1080 is “1x”
        float scale = Screen.height / 1080f;
        if (scale < 0.75f) scale = 0.75f; // optional lower clamp

        int margin     = Mathf.RoundToInt(baseMargin * scale);
        int lineHeight = Mathf.RoundToInt(baseLineHeight * scale);
        int width      = Mathf.RoundToInt(baseWidth * scale);
        int height     = lineHeight * 4 + Mathf.RoundToInt(10 * scale);

        Rect rect = new Rect(Screen.width - width - margin, margin, width, height);

        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.Box(rect, GUIContent.none);

        GUI.color = Color.white;
        GUI.skin.label.fontSize = Mathf.RoundToInt(baseFontSize * scale);

        // Inner padding
        float innerPadX = 5f * scale;
        float innerPadY = 5f * scale;

        float x = rect.x + innerPadX;
        float y = rect.y + innerPadY;
        float innerWidth = rect.width - innerPadX * 2f;

        // Column split: left for label, right for value
        float valueColWidth = innerWidth * 0.45f; // right ~45%
        float labelColWidth = innerWidth - valueColWidth;

        // Helper to draw one aligned line
        void DrawLine(string label, string value)
        {
            Rect labelRect = new Rect(x, y, labelColWidth, lineHeight);
            Rect valueRect = new Rect(x + labelColWidth, y, valueColWidth, lineHeight);

            // Left-aligned label
            TextAnchor oldAlign = GUI.skin.label.alignment;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.Label(labelRect, label);

            // Right-aligned value
            GUI.skin.label.alignment = TextAnchor.UpperRight;
            GUI.Label(valueRect, value);

            // Restore (not strictly necessary since we set each time)
            GUI.skin.label.alignment = oldAlign;

            y += lineHeight;
        }

        DrawLine("FPS",               $"{fps:0.0}");
        DrawLine("Main Thread CPU",   $"{mainThreadMs:0.00} ms");
        DrawLine("Render Thread CPU", $"{renderThreadMs:0.00} ms");
        DrawLine("GPU",               $"{gpuMs:0.00} ms");
    }
}
