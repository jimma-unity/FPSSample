using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

public class ScreenFaderMixerBehaviour : PlayableBehaviour
{
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (!Application.isPlaying)
            return;

        int inputCount = playable.GetInputCount();

        float blendedAlpha = 0.0f;
        float totalWeight = 0.0f;
        
        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);

            ScriptPlayable<ScreenFaderBehaviour> inputPlayable = (ScriptPlayable<ScreenFaderBehaviour>)playable.GetInput(i);
            ScreenFaderBehaviour input = inputPlayable.GetBehaviour();

            blendedAlpha += input.evaluatedAlpha * inputWeight;
            totalWeight += inputWeight;
        }

        if (totalWeight > 0.0f && Game.game && !Game.game.levelManager.IsLoadingLevel())
        {
            if (Game.game.screenFader)
            {
                Game.game.screenFader.SetTarget(blendedAlpha, 0.0f);
            }
        }
    }
}
