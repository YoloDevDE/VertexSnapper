using UnityEngine;
using VertexSnapper.Core;
using VertexSnapper.Utils;

namespace VertexSnapper.States;

public class PositioningState : IVertexSnapState
{
    private readonly Utils.CursorManager cursorManager;
    private readonly VertexSnapData data;

    private readonly VertexSnapLogger logger;
    private readonly RaycastHelper raycastHelper;
    private readonly VertexSnapStateMachine stateMachine;
    private readonly TargetSelector targetSelector;
    private readonly VertexCalculator vertexCalculator;

    public PositioningState(VertexSnapLogger logger, VertexSnapData data, VertexSnapStateMachine stateMachine)
    {
        this.logger = logger;
        this.data = data;
        this.stateMachine = stateMachine;
        cursorManager = new Utils.CursorManager(logger, data);
        targetSelector = new TargetSelector(logger, data);
        vertexCalculator = new VertexCalculator(logger);
        raycastHelper = new RaycastHelper(logger);
    }

    public VertexSnapMode Mode => VertexSnapMode.Positioning;

    public void OnEnter()
    {
        logger.LogMethodEntry(nameof(OnEnter));
        logger.LogObjectCount("selectedItems", data.SelectedItems.Count);
        logger.LogVariableValue("currentTarget", data.CurrentTarget?.name ?? "null");
        logger.LogMethodExit(nameof(OnEnter));
    }

    public void OnExit()
    {
        logger.LogMethodEntry(nameof(OnExit));
        // Keep cursor and target for potential transition to snapping
        logger.LogMethodExit(nameof(OnExit));
    }

    public void Update()
    {
        // Update current target based on what we're pointing at
        targetSelector.UpdateCurrentTarget();
    }

    public void HandleInput(bool keyDown, bool leftMousePressed)
    {
        logger.LogMethodEntry(nameof(HandleInput), $"keyDown: {keyDown}, leftMouse: {leftMousePressed}");

        if (!keyDown)
        {
            logger.LogDebug("Key released, returning to inactive");
            stateMachine.TransitionTo(VertexSnapMode.Inactive, "Key released during positioning");
            logger.LogMethodExit(nameof(HandleInput));
            return;
        }

        // Validate we still have valid state
        if (data.Central == null || data.SelectedItems.Count == 0 || data.CurrentTarget?.transform == null)
        {
            logger.LogWarning("Lost valid selection during positioning");
            stateMachine.TransitionTo(VertexSnapMode.Inactive, "Lost valid selection during positioning");
            logger.LogMethodExit(nameof(HandleInput));
            return;
        }

        // Handle cursor positioning
        HandleCursorPositioning();

        // Check for transition to snapping mode
        if (leftMousePressed && data.Cursor != null && data.CurrentTarget != null)
        {
            logger.LogDebug("Left click detected, preparing for snapping mode");
            PrepareForSnapping();
            stateMachine.TransitionTo(VertexSnapMode.Snapping, $"Left click while positioning cursor on {data.CurrentTarget.name} ({data.StoredSelectedItems.Count} objects stored)");
        }

        logger.LogMethodExit(nameof(HandleInput));
    }

    private void HandleCursorPositioning()
    {
        logger.LogMethodEntry(nameof(HandleCursorPositioning));

        cursorManager.CreateCursor();

        Ray ray = data.Camera.ScreenPointToRay(Input.mousePosition);
        if (raycastHelper.TryGetWorldPoint(ray, data.CurrentTarget, data.Cursor, out Vector3 worldPoint))
        {
            Vector3 closestVertex = vertexCalculator.GetClosestVertex(data.MeshFilters, worldPoint);
            data.Cursor.position = closestVertex;

            // Calculate vertex offset
            if (data.CurrentTarget?.transform != null)
            {
                data.VertOffset = data.CurrentTarget.transform.position - data.Cursor.position;
                logger.LogVariableValue("vertOffset", data.VertOffset);
            }

            logger.LogVariableValue("cursorPosition", closestVertex);
        }
        else
        {
            logger.LogWarning("Could not get world point for cursor positioning");
        }

        logger.LogMethodExit(nameof(HandleCursorPositioning));
    }

    private void PrepareForSnapping()
    {
        logger.LogMethodEntry(nameof(PrepareForSnapping));

        // Store current state for snapping mode
        data.StoredVertexPosition = data.Cursor.position;
        data.StoredVertOffset = data.VertOffset;
        data.StoredPrimaryTarget = data.CurrentTarget;

        logger.LogVariableValue("storedVertexPosition", data.StoredVertexPosition);
        logger.LogVariableValue("storedPrimaryTarget", data.StoredPrimaryTarget?.name ?? "null");

        // Store selected items and their relative positions
        data.StoredSelectedItems.Clear();
        data.StoredSelectedItems.AddRange(data.SelectedItems);

        data.StoredRelativePositions.Clear();
        foreach (BlockProperties item in data.StoredSelectedItems)
        {
            if (item?.transform != null)
            {
                data.StoredRelativePositions.Add(item.transform.position);
            }
        }

        logger.LogObjectCount("storedSelectedItems", data.StoredSelectedItems.Count);
        logger.LogObjectCount("storedRelativePositions", data.StoredRelativePositions.Count);

        // Clear selection to allow free roaming
        if (data.Central != null)
        {
            data.Central.selection.DeselectAllBlocks(true, "");
            logger.LogDebug("Selection cleared for free roaming");
        }

        logger.LogMethodExit(nameof(PrepareForSnapping));
    }
}