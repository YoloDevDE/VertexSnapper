using System;
using System.Collections;
using UnityEngine;

namespace VertexSnapper.Components;

/// <summary>
///     Verantwortlich für: Material, Animate-In/Out, Zerstörung nach Out.
///     Manager ruft nur Initialize(...) und RequestDespawn() auf.
/// </summary>
[DisallowMultipleComponent]
public class VertexSphere : MonoBehaviour
{
    private Coroutine animCo;
    private Color color = Color.cyan;
    private bool despawning;
    private readonly AnimationCurve inCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private readonly float inDuration = 0.15f;

    private Material matInstance;
    private readonly AnimationCurve outCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private readonly float outDuration = 0.12f;


    private float targetRadius = 0.2f;

    private void Awake()
    {
        // Falls jemand die Komponente als Prefab nutzt ohne Initialize aufzurufen:
        SetupMaterial();
    }

    private void OnDestroy()
    {
        if (matInstance != null)
        {
            // Aufräumen
            Destroy(matInstance);
            matInstance = null;
        }
    }

    /// <summary> Vom Manager sofort nach AddComponent aufzurufen. </summary>
    public void Initialize(float radius, Color color)
    {
        targetRadius = radius;
        this.color = color;
        SetupMaterial();
        // Startzustand: klein -> Animate-In
        SetScale(0f);
        PlayIn();
    }

    /// <summary> Manager ruft dies auf, um ein Ausblenden + Zerstören zu starten. </summary>
    public void RequestDespawn(Action onComplete = null)
    {
        if (despawning)
        {
            return;
        }

        despawning = true;
        if (animCo != null)
        {
            StopCoroutine(animCo);
        }

        animCo = StartCoroutine(AnimateScale(ScaleNow(), 0f, outDuration, outCurve, () =>
        {
            onComplete?.Invoke();
            // In Playmode: Destroy; im Editor-Mode außerhalb Play: DestroyImmediate wäre möglich,
            // aber hier bewusst einheitlich Destroy, um Animationsframe zu erlauben.
            Destroy(gameObject);
        }));
    }

    private void SetupMaterial()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (!renderer)
        {
            return;
        }

        if (matInstance == null)
        {
            Shader shader = Shader.Find("Standard");
            matInstance = new Material(shader);
            renderer.material = matInstance; // Instanz, nicht sharedMaterial
        }

        matInstance.color = color;
    }

    private void PlayIn()
    {
        if (animCo != null)
        {
            StopCoroutine(animCo);
        }

        animCo = StartCoroutine(AnimateScale(0f, targetRadius * 2f, inDuration, inCurve));
    }

    private IEnumerator AnimateScale(float from, float to, float duration, AnimationCurve curve, Action onComplete = null)
    {
        duration = Mathf.Max(0.0001f, duration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // unscaled, damit auch bei Pausen sichtbar
            float u = Mathf.Clamp01(t / duration);
            float k = curve != null ? curve.Evaluate(u) : u;
            SetScale(Mathf.Lerp(from, to, k));
            yield return null;
        }

        SetScale(to);
        onComplete?.Invoke();
    }

    private void SetScale(float diameter)
    {
        // Unity-Sphere hat Durchmesser = localScale.x (bei 1.0).
        transform.localScale = Vector3.one * diameter;
    }

    private float ScaleNow()
    {
        return transform.localScale.x;
    }
}