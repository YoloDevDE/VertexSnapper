using UnityEngine;
using VertexSnapper.Core;

namespace VertexSnapper.States;

public class InactiveState : IVertexSnapState
{
    private readonly VertexSnapData data;

    private readonly VertexSnapLogger logger;
    private readonly VertexSnapStateMachine stateMachine;

    public InactiveState(VertexSnapLogger logger, VertexSnapData data, VertexSnapStateMachine stateMachine)
    {
        this.logger = logger;
        this.data = data;
        this.stateMachine = stateMachine;
    }

    public VertexSnapMode Mode => VertexSnapMode.Inactive;

    public void OnEnter()
    {
        logger.LogMethodEntry(nameof(OnEnter));

        // Clean up any remaining state
        CleanupCursor();
        CleanupHolograms();
        ClearCurrentTarget();

        logger.LogMethodExit(nameof(OnEnter));
    }

    public void OnExit()
    {
        logger.LogMethodEntry(nameof(OnExit));
        // Nothing to clean up when leaving inactive state
        logger.LogMethodExit(nameof(OnExit));
    }

    public void Update()
    {
        // Inactive state doesn't need to update anything
    }

    public void HandleInput(bool keyDown, bool leftMousePressed)
    {
        logger.LogMethodEntry(nameof(HandleInput), $"keyDown: {keyDown}, leftMouse: {leftMousePressed}");

        // Check if we should transition to positioning mode
        if (keyDown && data.Central != null && data.SelectedItems.Count > 0)
        {
            logger.LogVariableValue("selectedItems.Count", data.SelectedItems.Count);

            // Set initial target to first selected item if none is set
            if (data.CurrentTarget == null || !data.SelectedItems.Contains(data.CurrentTarget))
            {
                data.CurrentTarget = data.SelectedItems[0];
                logger.LogVariableValue("currentTarget", data.CurrentTarget?.name ?? "null");

                if (data.CurrentTarget?.transform != null)
                {
                    data.MeshFilters = data.CurrentTarget.transform.GetComponentsInChildren<MeshFilter>();
                    logger.LogVariableValue("meshFilters.Length", data.MeshFilters?.Length ?? 0);
                }
            }

            stateMachine.TransitionTo(VertexSnapMode.Positioning, $"Key pressed with {data.SelectedItems.Count} object(s) selected");
        }

        logger.LogMethodExit(nameof(HandleInput));
    }

    private void CleanupCursor()
    {
        logger.LogMethodEntry(nameof(CleanupCursor));

        if (data.Cursor != null)
        {
            Object.Destroy(data.Cursor.gameObject);
            data.Cursor = null;
            logger.LogDebug("Cursor destroyed");
        }

        logger.LogMethodExit(nameof(CleanupCursor));
    }

    private void CleanupHolograms()
    {
        logger.LogMethodEntry(nameof(CleanupHolograms));

        foreach (GameObject hologram in data.Holograms)
        {
            if (hologram != null)
            {
                Object.Destroy(hologram);
            }
        }

        int hologramCount = data.Holograms.Count;
        data.Holograms.Clear();

        logger.LogVariableValue("destroyed holograms", hologramCount);
        logger.LogMethodExit(nameof(CleanupHolograms));
    }

    private void ClearCurrentTarget()
    {
        logger.LogMethodEntry(nameof(ClearCurrentTarget));

        string previousTarget = data.CurrentTarget?.name ?? "null";
        data.CurrentTarget = null;
        data.MeshFilters = null;

        logger.LogVariableValue("previousTarget", previousTarget);
        logger.LogMethodExit(nameof(ClearCurrentTarget));
    }
}