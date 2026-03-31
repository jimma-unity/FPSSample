using System;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class ScreenFaderBehaviour : PlayableBehaviour
{
    public float from = 0.0f;
    public float to = 1.0f;
    public AnimationCurve blendCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [HideInInspector] public float evaluatedAlpha;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (!Application.isPlaying)
            return;

        double time = playable.GetTime();
        double duration = playable.GetDuration();

        if (duration <= 0.0f)
        {
            evaluatedAlpha = to;
            return;
        }

        float t = Mathf.Clamp01((float)(time / duration));
        float curveValue = blendCurve.Evaluate(t);

        evaluatedAlpha = Mathf.Lerp(from, to, curveValue);
    }
}
