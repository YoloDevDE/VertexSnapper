using UnityEngine;
using VertexSnapper.Core;

namespace VertexSnapper.Utils;

public class MaterialManager
{
    private readonly VertexSnapData data;
    private readonly VertexSnapLogger logger;

    public MaterialManager(VertexSnapLogger logger, VertexSnapData data)
    {
        this.logger = logger;
        this.data = data;
    }

    public void ApplyOriginMaterials()
    {
        logger.LogMethodEntry(nameof(ApplyOriginMaterials));

        data.OriginRenderers.Clear();
        int materialsApplied = 0;

        foreach (BlockProperties item in data.StoredSelectedItems)
        {
            if (item?.transform != null)
            {
                Renderer[] renderers = item.transform.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    // Store original materials
                    if (!data.OriginalMaterials.ContainsKey(renderer))
                    {
                        data.OriginalMaterials[renderer] = renderer.materials;
                    }

                    // Apply origin material
                    Material originMaterial = CreateOriginMaterial();
                    Material[] materials = new Material[renderer.materials.Length];
                    for (int i = 0; i < materials.Length; i++)
                    {
                        materials[i] = originMaterial;
                    }

                    renderer.materials = materials;

                    data.OriginRenderers.Add(renderer);
                    materialsApplied++;
                }
            }
        }

        logger.LogVariableValue("origin materials applied", materialsApplied);
        logger.LogObjectCount("origin renderers", data.OriginRenderers.Count);
        logger.LogMethodExit(nameof(ApplyOriginMaterials));
    }

    public void RestoreOriginMaterials()
    {
        logger.LogMethodEntry(nameof(RestoreOriginMaterials));

        int restored = 0;
        foreach (Renderer renderer in data.OriginRenderers)
        {
            if (renderer != null && data.OriginalMaterials.ContainsKey(renderer))
            {
                renderer.materials = data.OriginalMaterials[renderer];
                restored++;
            }
        }

        data.OriginRenderers.Clear();
        data.OriginalMaterials.Clear();

        logger.LogVariableValue("materials restored", restored);
        logger.LogMethodExit(nameof(RestoreOriginMaterials));
    }

    public void UpdateOriginMaterialPulse()
    {
        if (data.OriginRenderers.Count == 0)
        {
            return;
        }

        data.PulseTime += Time.deltaTime;
        float pulseValue = (Mathf.Sin(data.PulseTime * 2f) + 1f) * 0.5f;
        float intensity = Mathf.Lerp(0.5f, 1.2f, pulseValue);

        foreach (Renderer renderer in data.OriginRenderers)
        {
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = Color.red * intensity;
            }
        }
    }

    public void UpdateHologramPulse()
    {
        if (data.Holograms.Count == 0)
        {
            return;
        }

        float pulseValue = (Mathf.Sin(data.PulseTime * 3f) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(0.3f, 0.8f, pulseValue);

        foreach (GameObject hologram in data.Holograms)
        {
            if (hologram != null)
            {
                Renderer renderer = hologram.GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    Color color = renderer.material.color;
                    color.a = alpha;
                    renderer.material.color = color;
                }
            }
        }
    }

    private Material CreateOriginMaterial()
    {
        logger.LogMethodEntry(nameof(CreateOriginMaterial));

        Material material = new Material(Shader.Find("Standard"));
        material.color = Color.red;
        material.SetFloat("_Metallic", 0.3f);
        material.SetFloat("_Smoothness", 0.7f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", Color.red * 0.3f);

        logger.LogDebug("Origin material created");
        logger.LogMethodExit(nameof(CreateOriginMaterial));
        return material;
    }
}