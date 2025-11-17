using System.Collections.Generic;
using UnityEngine;

namespace VertexSnapper.Helper;

public abstract class RaycastUtils
{
    public static bool IsSphereCastOnBlockSuccessful(Camera camera, out RaycastHit hit, List<BlockProperties> allowedBlocks = null)
    {
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        const float maxDistance = 250f;


        return (Physics.Raycast(ray, out hit, maxDistance) || TryDynamicSphereCast(out hit, ray, maxDistance, allowedBlocks))
               && PassesFilter(hit, allowedBlocks);
    }

    public static bool TryGetBlocksFromHit(RaycastHit hit, out BlockProperties block)
    {
        return block = hit.collider.GetComponentInParent<BlockProperties>();
    }

    private static bool TryDynamicSphereCast(out RaycastHit hit, Ray ray, float maxDistance, List<BlockProperties> allowedBlocks = null)
    {
        const float maxRadius = 1f;
        const float radiusStep = 0.1f;
        const float startRadius = 0.1f;

        for (float radius = startRadius; radius <= maxRadius; radius += radiusStep)
        {
            if (!Physics.SphereCast(ray, radius, out hit, maxDistance) || !PassesFilter(hit, allowedBlocks))
            {
                continue;
            }

            BlockProperties block = hit.collider.GetComponentInParent<BlockProperties>();
            return block;
        }

        hit = default;
        return false;
    }


    private static bool PassesFilter(RaycastHit hit, List<BlockProperties> filter)
    {
        BlockProperties block = hit.collider.GetComponentInParent<BlockProperties>();
        return filter == null || filter.Count == 0 ? block : block && filter.Contains(block);
    }
}