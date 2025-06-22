using System;
using UnityEngine;
using UnityEngine.Rendering;
using VertexSnapper.Core;
using Object = UnityEngine.Object;

namespace VertexSnapper.Utils;

public class HologramManager
{
    private readonly VertexSnapData data;
    private readonly VertexSnapLogger logger;

    public HologramManager(VertexSnapLogger logger, VertexSnapData data)
    {
        this.logger = logger;
        this.data = data;
    }

    public void CreateAllHolograms()
    {
        logger.LogMethodEntry(nameof(CreateAllHolograms));
        logger.LogObjectCount("stored items for holograms", data.StoredSelectedItems.Count);

        if (data.Holograms.Count > 0)
        {
            logger.LogDebug("Holograms already exist, skipping creation");
            logger.LogMethodExit(nameof(CreateAllHolograms));
            return;
        }

        int created = 0;
        for (int i = 0; i < data.StoredSelectedItems.Count; i++)
        {
            BlockProperties item = data.StoredSelectedItems[i];
            if (item?.transform != null)
            {
                GameObject hologram = CreateHologramForItem(item, i);
                if (hologram != null)
                {
                    data.Holograms.Add(hologram);
                    created++;
                }
            }
        }

        logger.LogObjectCount("holograms created", created);
        logger.LogMethodExit(nameof(CreateAllHolograms));
    }

    public void UpdateHologramPositions(Vector3 targetVertex, Vector3 originalVertex)
    {
        logger.LogMethodEntry(nameof(UpdateHologramPositions));
        logger.LogVariableValue("targetVertex", targetVertex);
        logger.LogVariableValue("originalVertex", originalVertex);

        Vector3 offset = targetVertex - originalVertex;
        logger.LogVariableValue("calculated offset", offset);

        int updated = 0;
        for (int i = 0; i < data.Holograms.Count && i < data.StoredRelativePositions.Count; i++)
        {
            if (data.Holograms[i] != null)
            {
                Vector3 newPosition = data.StoredRelativePositions[i] + offset;
                data.Holograms[i].transform.position = newPosition;
                updated++;
            }
        }

        logger.LogVariableValue("holograms positioned", updated);
        logger.LogMethodExit(nameof(UpdateHologramPositions));
    }

    public void DestroyAllHolograms()
    {
        logger.LogMethodEntry(nameof(DestroyAllHolograms));

        int destroyed = 0;
        foreach (GameObject hologram in data.Holograms)
        {
            if (hologram != null)
            {
                Object.Destroy(hologram);
                destroyed++;
            }
        }

        data.Holograms.Clear();

        logger.LogVariableValue("holograms destroyed", destroyed);
        logger.LogMethodExit(nameof(DestroyAllHolograms));
    }

    public void UpdateHologramPulse()
    {
        if (data.Holograms.Count == 0)
        {
            return;
        }

        data.PulseTime += Time.deltaTime;
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

    private GameObject CreateHologramForItem(BlockProperties item, int index)
    {
        logger.LogMethodEntry(nameof(CreateHologramForItem), $"item: {item.name}, index: {index}");

        try
        {
            GameObject hologramObject = Object.Instantiate(item.gameObject);
            hologramObject.name = $"Hologram_{item.name}_{index}";

            // Remove any colliders and physics
            RemovePhysicsComponents(hologramObject);

            // Set up hologram material
            SetupHologramMaterial(hologramObject);

            logger.LogDebug($"Hologram created for {item.name}");
            logger.LogMethodExit(nameof(CreateHologramForItem), "success");
            return hologramObject;
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to create hologram for {item.name}", ex);
            logger.LogMethodExit(nameof(CreateHologramForItem), "failed");
            return null;
        }
    }

    private void RemovePhysicsComponents(GameObject obj)
    {
        logger.LogMethodEntry(nameof(RemovePhysicsComponents));

        int removed = 0;

        // Remove colliders
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            Object.Destroy(collider);
            removed++;
        }

        // Remove rigidbodies
        Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            Object.Destroy(rb);
            removed++;
        }

        // Remove BlockProperties to avoid editor interference
        BlockProperties[] blockProps = obj.GetComponentsInChildren<BlockProperties>();
        foreach (BlockProperties prop in blockProps)
        {
            Object.Destroy(prop);
            removed++;
        }

        logger.LogVariableValue("components removed", removed);
        logger.LogMethodExit(nameof(RemovePhysicsComponents));
    }

    private void SetupHologramMaterial(GameObject obj)
    {
        logger.LogMethodEntry(nameof(SetupHologramMaterial));

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        int materialsSet = 0;

        foreach (Renderer renderer in renderers)
        {
            Material hologramMaterial = new Material(Shader.Find("Standard"));
            hologramMaterial.SetFloat("_Mode", 3); // Transparent mode
            hologramMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            hologramMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            hologramMaterial.SetInt("_ZWrite", 0);
            hologramMaterial.DisableKeyword("_ALPHATEST_ON");
            hologramMaterial.EnableKeyword("_ALPHABLEND_ON");
            hologramMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            hologramMaterial.renderQueue = 3000;

            hologramMaterial.color = new Color(0f, 1f, 1f, 0.5f); // Cyan with transparency
            hologramMaterial.SetFloat("_Metallic", 0.8f);
            hologramMaterial.SetFloat("_Smoothness", 0.9f);

            renderer.material = hologramMaterial;
            materialsSet++;
        }

        logger.LogVariableValue("hologram materials set", materialsSet);
        logger.LogMethodExit(nameof(SetupHologramMaterial));
    }
}