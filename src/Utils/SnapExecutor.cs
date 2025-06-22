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

        // Prepare undo data before making changes
        PrepareUndoData();

        // Perform the actual snapping
        bool success = ExecuteSnap();

        if (success)
        {
            // Finalize undo data after changes
            FinalizeUndoData();

            // Re-select the moved items
            RestoreSelection();

            logger.LogInfo($"Successfully snapped {data.StoredSelectedItems.Count} objects");
            logger.LogMethodExit(nameof(PerformSnap), "true");
            return true;
        }

        logger.LogError("Snap execution failed");
        logger.LogMethodExit(nameof(PerformSnap), "false (execution failed)");
        return false;
    }

    private void PrepareUndoData()
    {
        logger.LogMethodEntry(nameof(PrepareUndoData));

        data.BeforeData.Clear();
        data.BeforeSelection.Clear();

        foreach (BlockProperties item in data.StoredSelectedItems)
        {
            if (item != null)
            {
                data.BeforeData.Add(item.GetSaveData());
                data.BeforeSelection.Add(item.uuid);
            }
        }

        logger.LogObjectCount("before data captured", data.BeforeData.Count);
        logger.LogMethodExit(nameof(PrepareUndoData));
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

    private void FinalizeUndoData()
    {
        logger.LogMethodEntry(nameof(FinalizeUndoData));

        data.AfterData.Clear();
        data.AfterSelection.Clear();

        foreach (BlockProperties item in data.StoredSelectedItems)
        {
            if (item != null)
            {
                data.AfterData.Add(item.GetSaveData());
                data.AfterSelection.Add(item.uuid);
            }
        }

        // Create undo entry if we have valid central reference
        if (data.Central?.undo != null && data.BeforeData.Count > 0 && data.AfterData.Count > 0)
        {
            data.Central.undo.CreateUndoEntry(
                data.BeforeData.ToArray(),
                data.BeforeSelection.ToArray(),
                data.AfterData.ToArray(),
                data.AfterSelection.ToArray()
            );

            logger.LogDebug("Undo entry created");
        }

        logger.LogObjectCount("after data captured", data.AfterData.Count);
        logger.LogMethodExit(nameof(FinalizeUndoData));
    }

    private void RestoreSelection()
    {
        logger.LogMethodEntry(nameof(RestoreSelection));

        if (data.Central?.selection == null)
        {
            logger.LogWarning("Cannot restore selection - central or selection is null");
            logger.LogMethodExit(nameof(RestoreSelection));
            return;
        }

        int restored = 0;
        foreach (BlockProperties item in data.StoredSelectedItems)
        {
            if (item != null)
            {
                data.Central.selection.SelectBlock(item, false, "");
                restored++;
            }
        }

        logger.LogVariableValue("selection restored", restored);
        logger.LogMethodExit(nameof(RestoreSelection));
    }

    public void CancelSnap()
    {
        logger.LogMethodEntry(nameof(CancelSnap));

        // Simply clear stored data without making changes
        data.StoredSelectedItems.Clear();
        data.StoredRelativePositions.Clear();
        data.BeforeData.Clear();
        data.BeforeSelection.Clear();
        data.AfterData.Clear();
        data.AfterSelection.Clear();

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