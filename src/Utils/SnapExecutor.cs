using System;
using UnityEngine;
using VertexSnapper.Core;

namespace VertexSnapper.Utils;

public class SnapExecutor
{
    private readonly VertexSnapData data;
    private readonly VertexSnapLogger logger;

    public SnapExecutor(VertexSnapLogger logger, VertexSnapData data)
    {
        this.logger = logger;
        this.data = data;
    }

    public bool PerformSnap()
    {
        logger.LogMethodEntry(nameof(PerformSnap));
        logger.LogObjectCount("holograms to snap", data.Holograms.Count);
        logger.LogObjectCount("stored items", data.StoredSelectedItems.Count);

        if (data.Holograms.Count == 0 || data.StoredSelectedItems.Count == 0)
        {
            logger.LogWarning("Cannot perform snap - no holograms or stored items");
            logger.LogMethodExit(nameof(PerformSnap), "false (no data)");
            return false;
        }

        // Perform the actual snapping
        bool success = ExecuteSnap();

        if (success)
        {
            logger.LogInfo($"Successfully snapped {data.StoredSelectedItems.Count} objects");
            logger.LogMethodExit(nameof(PerformSnap), "true");
            return true;
        }

        logger.LogError("Snap execution failed");
        logger.LogMethodExit(nameof(PerformSnap), "false (execution failed)");
        return false;
    }

    private bool ExecuteSnap()
    {
        logger.LogMethodEntry(nameof(ExecuteSnap));

        int snapCount = 0;

        try
        {
            for (int i = 0; i < data.StoredSelectedItems.Count && i < data.Holograms.Count; i++)
            {
                BlockProperties item = data.StoredSelectedItems[i];
                GameObject hologram = data.Holograms[i];

                if (item?.transform != null && hologram != null)
                {
                    Vector3 newPosition = hologram.transform.position;

                    logger.LogVariableValue($"moving {item.name} from", item.transform.position);
                    logger.LogVariableValue($"moving {item.name} to", newPosition);

                    item.transform.position = newPosition;
                    snapCount++;
                }
            }

            logger.LogVariableValue("objects snapped", snapCount);
            logger.LogMethodExit(nameof(ExecuteSnap), "true");
            return snapCount > 0;
        }
        catch (Exception ex)
        {
            logger.LogError("Exception during snap execution", ex);
            logger.LogMethodExit(nameof(ExecuteSnap), "false (exception)");
            return false;
        }
    }

    public void CancelSnap()
    {
        logger.LogMethodEntry(nameof(CancelSnap));

        // Simply clear stored data without making changes
        data.StoredSelectedItems.Clear();
        data.StoredRelativePositions.Clear();

        logger.LogDebug("Snap operation cancelled");
        logger.LogMethodExit(nameof(CancelSnap));
    }

    public bool CanPerformSnap()
    {
        bool canSnap = data.StoredSelectedItems.Count > 0 &&
                       data.Holograms.Count > 0 &&
                       data.StoredSelectedItems.Count == data.Holograms.Count;

        logger.LogVariableValue("can perform snap", canSnap);

        if (!canSnap)
        {
            logger.LogVariableValue("stored items count", data.StoredSelectedItems.Count);
            logger.LogVariableValue("holograms count", data.Holograms.Count);
        }

        return canSnap;
    }
}