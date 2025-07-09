using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VertexSnapper.States;

namespace VertexSnapper.Util;

public class VertexHologramManager : MonoBehaviour
{
    private GameStateMachine _stateMachine;
    private Material _targetMaterial, _originHologramMaterial, _originMaterial;

    private void Awake()
    {
    }


    public void Initialize(GameStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
        foreach (BlockProperties blockProperty in _stateMachine.BlockSelection)
        {
            List<Renderer> allMeshRenderers = new List<Renderer>();

            // 1. Hole alle MeshRenderer vom GameObject selbst
            allMeshRenderers.AddRange(blockProperty.GetComponents<Renderer>());

            // 2. Hole alle MeshRenderer von allen Kindern (aktive UND inaktive)
            allMeshRenderers.AddRange(blockProperty.GetComponentsInChildren<Renderer>(true));


            foreach (Renderer meshRenderer in allMeshRenderers)
            {
                if (meshRenderer == null || meshRenderer.sharedMaterial == null)
                {
                    continue;
                }

                _originMaterial = meshRenderer.sharedMaterial;
                meshRenderer.material = CreateWaterMaterial(Color.cyan);
            }
        }
    }

    public Material CreateWaterMaterial(Color baseColor)

    {
        Material material = new Material(Shader.Find("Standard"));

        // Grundfarbe mit Transparenz
        Color transparentColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.3f);
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

        // Emission für Glow-Effekt
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", baseColor * 0.5f);

        // Metallic/Smoothness für wasserähnlichen Look
        material.SetFloat("_Metallic", 0.1f);
        material.SetFloat("_Glossiness", 0.9f);

        return material;
    }
}