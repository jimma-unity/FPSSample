using UnityEngine;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;
    private float targetAlpha;
    private float fadeSpeed;
    private Coroutine currentFader;

    private void Awake()
    {
        targetAlpha = canvasGroup.alpha;
        fadeSpeed = 1.0f / fadeDuration;
    }

    public void FadeOut(float durationOverride = -1.0f)
    {
        SetTarget(1.0f, durationOverride);
    }

    public void FadeIn(float durationOverride = -1.0f)
    {
        SetTarget(0.0f, durationOverride);
    }

    public void SetTarget(float targetAlpha, float durationOverride = -1.0f)
    {
        targetAlpha = Mathf.Clamp01(targetAlpha);

        float duration = durationOverride >= 0.0f ? durationOverride : fadeDuration;
        if (duration == 0.0f)
        {
            canvasGroup.alpha = targetAlpha;
            return;
        }

        fadeSpeed = 1.0f / duration;

        if (currentFader == null)
        {
            currentFader = StartCoroutine(Fade());
        }
    }

    private IEnumerator Fade()
    {
        while (Mathf.Abs(canvasGroup.alpha - targetAlpha) >= 0.001f)
        {
            canvasGroup.alpha = Mathf.MoveTowards(
                canvasGroup.alpha,
                targetAlpha,
                fadeSpeed * Time.deltaTime);

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        currentFader = null;
    }
}
