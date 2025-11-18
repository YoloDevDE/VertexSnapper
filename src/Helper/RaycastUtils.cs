using System.Collections.Generic;
using UnityEngine;

namespace VertexSnapper.Helper;

public abstract class RaycastUtils
{
    public static bool IsSphereCastOnBlockSuccessful(
        Camera camera,
        out RaycastHit hit,
        List<BlockProperties> allowedBlocks = null,
        List<BlockProperties> disallowedBlocks = null)
    {
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        const float maxDistance = 10000f;

        return (Physics.Raycast(ray, out hit, maxDistance) ||
                TryDynamicSphereCast(out hit, ray, maxDistance, allowedBlocks, disallowedBlocks))
               && PassesFilter(hit, allowedBlocks, disallowedBlocks);
    }

    public static bool TryGetBlocksFromHit(RaycastHit hit, out BlockProperties block)
    {
        return block = hit.collider.GetComponentInParent<BlockProperties>();
    }

    private static bool TryDynamicSphereCast(
        out RaycastHit hit,
        Ray ray,
        float maxDistance,
        List<BlockProperties> allowedBlocks = null,
        List<BlockProperties> disallowedBlocks = null)
    {
        const float maxRadius = 1f;
        const float radiusStep = 0.1f;
        const float startRadius = 0.1f;

        for (float radius = startRadius; radius <= maxRadius; radius += radiusStep)
        {
            if (!Physics.SphereCast(ray, radius, out hit, maxDistance) ||
                !PassesFilter(hit, allowedBlocks, disallowedBlocks))
            {
                continue;
            }

            // We already know it passed the filter, so just return true
            return true;
        }

        hit = default;
        return false;
    }

    private static bool PassesFilter(
        RaycastHit hit,
        List<BlockProperties> allowedBlocks = null,
        List<BlockProperties> disallowedBlocks = null)
    {
        BlockProperties block = hit.collider.GetComponentInParent<BlockProperties>();
        if (!block)
        {
            return false;
        }

        // If an allowed list is provided and non‑empty, the block must be in it
        if (allowedBlocks != null && allowedBlocks.Count > 0 && !allowedBlocks.Contains(block))
        {
            return false;
        }

        // If a disallowed list is provided and non‑empty, the block must NOT be in it
        if (disallowedBlocks != null && disallowedBlocks.Count > 0 && disallowedBlocks.Contains(block))
        {
            return false;
        }

        return true;
    }
}