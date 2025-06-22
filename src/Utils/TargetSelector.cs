using UnityEngine;
using VertexSnapper.Core;

namespace VertexSnapper.Utils;

public class TargetSelector
{
    private readonly VertexSnapData data;
    private readonly VertexSnapLogger logger;
    private readonly RaycastHelper raycastHelper;

    public TargetSelector(VertexSnapLogger logger, VertexSnapData data)
    {
        this.logger = logger;
        this.data = data;
        raycastHelper = new RaycastHelper(logger);
    }

    public void UpdateCurrentTarget()
    {
        if (data.Camera == null || data.SelectedItems.Count == 0)
        {
            return;
        }

        logger.LogMethodEntry(nameof(UpdateCurrentTarget));

        Ray ray = data.Camera.ScreenPointToRay(Input.mousePosition);
        BlockProperties previousTarget = data.CurrentTarget;

        if (raycastHelper.TryGetHitTarget(ray, data.SelectedItems, out BlockProperties newTarget))
        {
            if (newTarget != data.CurrentTarget)
            {
                logger.LogVariableValue("target changed from", previousTarget?.name ?? "null");
                logger.LogVariableValue("target changed to", newTarget?.name ?? "null");

                data.CurrentTarget = newTarget;

                // Update mesh filters for new target
                if (newTarget?.transform != null)
                {
                    data.MeshFilters = newTarget.transform.GetComponentsInChildren<MeshFilter>();
                    logger.LogVariableValue("meshFilters updated", data.MeshFilters?.Length ?? 0);
                }
            }
        }
        else
        {
            // Keep current target if no hit found
            logger.LogDebug("No target hit, keeping current target");
        }

        logger.LogMethodExit(nameof(UpdateCurrentTarget));
    }

    public bool HasValidTarget()
    {
        bool isValid = data.CurrentTarget != null &&
                       data.CurrentTarget.transform != null &&
                       data.SelectedItems.Contains(data.CurrentTarget);

        logger.LogVariableValue("target valid", isValid);
        return isValid;
    }

    public void ClearTarget()
    {
        logger.LogMethodEntry(nameof(ClearTarget));

        string previousTarget = data.CurrentTarget?.name ?? "null";
        data.CurrentTarget = null;
        data.MeshFilters = null;

        logger.LogVariableValue("cleared target", previousTarget);
        logger.LogMethodExit(nameof(ClearTarget));
    }

    public void SetTarget(BlockProperties target)
    {
        logger.LogMethodEntry(nameof(SetTarget), target?.name ?? "null");

        if (target != null && data.SelectedItems.Contains(target))
        {
            data.CurrentTarget = target;

            if (target.transform != null)
            {
                data.MeshFilters = target.transform.GetComponentsInChildren<MeshFilter>();
                logger.LogVariableValue("meshFilters set", data.MeshFilters?.Length ?? 0);
            }

            logger.LogDebug($"Target set to {target.name}");
        }
        else
        {
            logger.LogWarning($"Cannot set target to {target?.name ?? "null"} - not in selected items");
        }

        logger.LogMethodExit(nameof(SetTarget));
    }
}