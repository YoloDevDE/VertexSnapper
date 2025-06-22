using System.Collections.Generic;
using UnityEngine;
using VertexSnapper.Core;

namespace VertexSnapper.Utils;

public class RaycastHelper
{
    private readonly VertexSnapLogger logger;

    public RaycastHelper(VertexSnapLogger logger)
    {
        this.logger = logger;
    }

    public bool TryGetWorldPoint(Ray ray, BlockProperties target, Transform cursor, out Vector3 worldPoint)
    {
        logger.LogMethodEntry(nameof(TryGetWorldPoint));
        logger.LogVariableValue("target", target?.name ?? "null");

        worldPoint = Vector3.zero;

        if (target?.transform == null)
        {
            logger.LogWarning("Target or target transform is null");
            logger.LogMethodExit(nameof(TryGetWorldPoint), "false (null target)");
            return false;
        }

        // Raycast against target's colliders
        Collider[] colliders = target.transform.GetComponentsInChildren<Collider>();
        logger.LogObjectCount("target colliders", colliders.Length);

        float closestDistance = float.MaxValue;
        bool hitFound = false;

        foreach (Collider collider in colliders)
        {
            if (collider != null && collider.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    worldPoint = hit.point;
                    hitFound = true;
                }
            }
        }

        if (hitFound)
        {
            logger.LogVariableValue("worldPoint found", worldPoint);
            logger.LogVariableValue("hit distance", closestDistance);
            logger.LogMethodExit(nameof(TryGetWorldPoint), "true");
            return true;
        }

        logger.LogDebug("No raycast hit found on target");
        logger.LogMethodExit(nameof(TryGetWorldPoint), "false (no hit)");
        return false;
    }

    public bool TryGetHitTarget(Ray ray, List<BlockProperties> selectedItems, out BlockProperties hitTarget)
    {
        logger.LogMethodEntry(nameof(TryGetHitTarget));
        logger.LogObjectCount("selectedItems", selectedItems?.Count ?? 0);

        hitTarget = null;

        if (selectedItems == null || selectedItems.Count == 0)
        {
            logger.LogWarning("No selected items to check");
            logger.LogMethodExit(nameof(TryGetHitTarget), "false (no items)");
            return false;
        }

        float closestDistance = float.MaxValue;
        bool hitFound = false;

        foreach (BlockProperties item in selectedItems)
        {
            if (item?.transform == null)
            {
                continue;
            }

            Collider[] colliders = item.transform.GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                if (collider != null && collider.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    if (hit.distance < closestDistance)
                    {
                        closestDistance = hit.distance;
                        hitTarget = item;
                        hitFound = true;
                    }
                }
            }
        }

        if (hitFound)
        {
            logger.LogVariableValue("hit target", hitTarget?.name ?? "null");
            logger.LogVariableValue("hit distance", closestDistance);
            logger.LogMethodExit(nameof(TryGetHitTarget), "true");
            return true;
        }

        logger.LogDebug("No target hit among selected items");
        logger.LogMethodExit(nameof(TryGetHitTarget), "false (no hit)");
        return false;
    }

    public bool TryFindTargetVertex(Ray ray, Transform cursor, List<GameObject> holograms, List<BlockProperties> excludeItems, out Vector3 targetVertex)
    {
        logger.LogMethodEntry(nameof(TryFindTargetVertex));
        logger.LogObjectCount("excludeItems", excludeItems?.Count ?? 0);

        targetVertex = Vector3.zero;

        // Get all hits along the ray
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);
        logger.LogObjectCount("raycast hits", hits.Length);

        float closestDistance = float.MaxValue;
        bool validHitFound = false;

        foreach (RaycastHit hit in hits)
        {
            // Skip if hit is on cursor
            if (cursor != null && IsTransformOrChild(hit.transform, cursor))
            {
                continue;
            }

            // Skip if hit is on hologram
            if (IsHologram(hit.transform.gameObject, holograms))
            {
                continue;
            }

            // Skip if hit is on excluded items
            if (IsExcludedItem(hit.transform, excludeItems))
            {
                continue;
            }

            // Valid hit found
            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                targetVertex = hit.point;
                validHitFound = true;
            }
        }

        if (validHitFound)
        {
            // Find closest vertex to hit point
            MeshFilter meshFilter = GetMeshFilterFromHit(hits, targetVertex);
            if (meshFilter != null)
            {
                VertexCalculator vertexCalculator = new VertexCalculator(logger);
                targetVertex = vertexCalculator.GetClosestVertex(new[] { meshFilter }, targetVertex);

                logger.LogVariableValue("target vertex found", targetVertex);
                logger.LogMethodExit(nameof(TryFindTargetVertex), "true");
                return true;
            }
        }

        logger.LogDebug("No valid target vertex found");
        logger.LogMethodExit(nameof(TryFindTargetVertex), "false");
        return false;
    }

    private bool IsTransformOrChild(Transform transform, Transform parent)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current == parent)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool IsHologram(GameObject obj, List<GameObject> holograms)
    {
        if (holograms == null)
        {
            return false;
        }

        foreach (GameObject hologram in holograms)
        {
            if (hologram != null && (obj == hologram || IsTransformOrChild(obj.transform, hologram.transform)))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsExcludedItem(Transform transform, List<BlockProperties> excludeItems)
    {
        if (excludeItems == null)
        {
            return false;
        }

        foreach (BlockProperties item in excludeItems)
        {
            if (item?.transform != null && IsTransformOrChild(transform, item.transform))
            {
                return true;
            }
        }

        return false;
    }

    private MeshFilter GetMeshFilterFromHit(RaycastHit[] hits, Vector3 targetPoint)
    {
        logger.LogMethodEntry(nameof(GetMeshFilterFromHit));

        float closestDistance = float.MaxValue;
        MeshFilter closestMeshFilter = null;

        foreach (RaycastHit hit in hits)
        {
            if (Vector3.Distance(hit.point, targetPoint) < 0.1f) // Close to our target point
            {
                MeshFilter meshFilter = hit.transform.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    float distance = Vector3.Distance(hit.point, targetPoint);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestMeshFilter = meshFilter;
                    }
                }
            }
        }

        logger.LogVariableValue("meshFilter found", closestMeshFilter != null);
        logger.LogMethodExit(nameof(GetMeshFilterFromHit));

        return closestMeshFilter;
    }
}