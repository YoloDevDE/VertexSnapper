using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using VertexSnapper.States;

namespace VertexSnapper.Util;

public class VertexHologramManager : MonoBehaviour
{
    // Pulsing-System
    private readonly List<Material> _pulseMaterials = new List<Material>();
    private float _maxAlpha = 0.7f;
    private float _maxEmission = 1.0f;
    private float _minAlpha = 0.1f;
    private float _minEmission = 0.2f;
    private float _pulseSpeed = 2f;
    private GameStateMachine _stateMachine;
    private Material _targetMaterial, _originHologramMaterial, _originMaterial;

    private void Awake()
    {
    }

    private void Update()
    {
        UpdatePulsingMaterials();
    }

    // Cleanup-Methode für wenn das GameObject zerstört wird
    private void OnDestroy()
    {
        _pulseMaterials.Clear();
    }

    private void UpdatePulsingMaterials()
    {
        if (_pulseMaterials.Count == 0)
        {
            return;
        }

        // Berechne Pulse-Werte mit Sinus-Funktion
        float time = Time.time * _pulseSpeed;
        float pulseValue = (Mathf.Sin(time) + 1f) / 2f; // Normalisiert zwischen 0 und 1

        // Interpoliere zwischen min und max Werten
        float currentAlpha = Mathf.Lerp(_minAlpha, _maxAlpha, pulseValue);
        float currentEmission = Mathf.Lerp(_minEmission, _maxEmission, pulseValue);

        // Aktualisiere alle Pulse-Materialien
        foreach (Material material in _pulseMaterials)
        {
            if (material != null)
            {
                // Alpha-Wert pulsieren lassen
                Color currentColor = material.color;
                currentColor.a = currentAlpha;
                material.color = currentColor;

                // Emission pulsieren lassen
                Color baseEmission = material.GetColor("_EmissionColor");
                Color newEmission = new Color(baseEmission.r, baseEmission.g, baseEmission.b, 1f) * currentEmission;
                material.SetColor("_EmissionColor", newEmission);
            }
        }
    }

    public void Initialize(GameStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
        _pulseMaterials.Clear(); // Lösche alte Referenzen

        foreach (BlockProperties blockProperty in _stateMachine.BlockSelection)
        {
            List<Renderer> renderers = blockProperty.GetComponentsInChildren<Renderer>().ToList();
            foreach (Renderer renderer in renderers)
            {
                // Wenn nur ein Material vorhanden ist
                if (renderer.materials.Length == 1)
                {
                    Material pulseMaterial = CreateWaterMaterial(Color.cyan);
                    renderer.material = pulseMaterial;
                    _pulseMaterials.Add(pulseMaterial);
                }
                else
                {
                    // Für mehrere Materialien
                    Material[] newMaterials = new Material[renderer.materials.Length];
                    for (int i = 0; i < newMaterials.Length; i++)
                    {
                        Material pulseMaterial = CreateWaterMaterial(Color.cyan);
                        newMaterials[i] = pulseMaterial;
                        _pulseMaterials.Add(pulseMaterial);
                    }

                    renderer.materials = newMaterials;
                }
            }
        }
    }

    public Material CreateWaterMaterial(Color baseColor)
    {
        Material material = new Material(Shader.Find("Standard"));

        // Grundfarbe mit Transparenz (startet mit minAlpha)
        Color transparentColor = new Color(baseColor.r, baseColor.g, baseColor.b, _minAlpha);
        material.color = transparentColor;

        // Transparenz-Einstellungen
        material.SetFloat("_Mode", 3); // Transparent mode
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;

        // Emission für Glow-Effekt (startet mit minEmission)
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", baseColor * _minEmission);

        // Metallic/Smoothness für wasserähnlichen Look
        material.SetFloat("_Metallic", 0.1f);
        material.SetFloat("_Glossiness", 0.9f);

        return material;
    }

    // Öffentliche Methoden um Pulse-Parameter zu ändern
    public void SetPulseSpeed(float speed)
    {
        _pulseSpeed = speed;
    }

    public void SetPulseRange(float minAlpha, float maxAlpha)
    {
        _minAlpha = minAlpha;
        _maxAlpha = maxAlpha;
    }

    public void SetEmissionRange(float minEmission, float maxEmission)
    {
        _minEmission = minEmission;
        _maxEmission = maxEmission;
    }
}