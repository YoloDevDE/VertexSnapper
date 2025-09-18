using System;
using System.Collections;
using UnityEngine;

namespace VertexSnapper.Util;

public class SphereAnimator : MonoBehaviour
{
    private readonly float animationDuration = 0.8f / 4;
    private readonly float overshootAmount = 1.3f; // How much to overshoot (30% bigger)
    private float animationTime;
    private bool isAnimatingIn = true;
    private Vector3 targetScale;

    private void Update()
    {
        if (!isAnimatingIn)
        {
            return;
        }

        animationTime += Time.deltaTime;
        float progress = animationTime / animationDuration;

        if (progress >= 1f)
        {
            progress = 1f;
            isAnimatingIn = false;
            transform.localScale = targetScale;
        }
        else
        {
            float bounceScale = CalculateBounceScale(progress);
            transform.localScale = targetScale * bounceScale;
        }
    }

    public void Initialize(float targetRadius)
    {
        targetScale = Vector3.one * targetRadius;
        transform.localScale = Vector3.zero;
        animationTime = 0f;
    }

    public void AnimateOut(Action onComplete = null)
    {
        isAnimatingIn = false;
        StartCoroutine(AnimateToScale(Vector3.zero, onComplete));
    }

    private IEnumerator AnimateToScale(Vector3 target, Action onComplete)
    {
        Vector3 startScale = transform.localScale;
        float elapsedTime = 0f;
        float duration = 0.3f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // Smooth ease out for shrinking
            progress = 1f - Mathf.Pow(1f - progress, 3f);

            transform.localScale = Vector3.Lerp(startScale, target, progress);
            yield return null;
        }

        transform.localScale = target;
        onComplete?.Invoke();
    }

    private float CalculateBounceScale(float t)
    {
        switch (t)
        {
            // Phase 1: Grow quickly to overshoot (0 to 0.4 of animation)
            case < 0.4f:
            {
                float phase1Progress = t / 0.4f;
                return Mathf.SmoothStep(0f, overshootAmount, phase1Progress);
            }
            // Phase 2: Bounce back down below target (0.4 to 0.7 of animation)
            case < 0.7f:
            {
                float phase2Progress = (t - 0.4f) / 0.3f;
                return Mathf.Lerp(overshootAmount, 0.9f, phase2Progress);
            }
            default:
            {
                // Phase 3: Settle to final size (0.7 to 1.0 of animation)

                float phase3Progress = (t - 0.7f) / 0.3f;
                return Mathf.SmoothStep(0.9f, 1f, phase3Progress);
            }
        }
    }
}