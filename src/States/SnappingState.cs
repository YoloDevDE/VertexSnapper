using UnityEngine;
using VertexSnapper.Core;
using VertexSnapper.Utils;

namespace VertexSnapper.States;

public class SnappingState : IVertexSnapState
{
    private readonly Utils.CursorManager cursorManager;
    private readonly VertexSnapData data;
    private readonly HologramManager hologramManager;

    private readonly VertexSnapLogger logger;
    private readonly Utils.MaterialManager materialManager;
    private readonly RaycastHelper raycastHelper;
    private readonly SnapExecutor snapExecutor;
    private readonly VertexSnapStateMachine stateMachine;

    public SnappingState(VertexSnapLogger logger, VertexSnapData data, VertexSnapStateMachine stateMachine)
    {
        this.logger = logger;
        this.data = data;
        this.stateMachine = stateMachine;
        cursorManager = new Utils.CursorManager(logger, data);
        hologramManager = new HologramManager(logger, data);
        materialManager = new Utils.MaterialManager(logger, data);
        raycastHelper = new RaycastHelper(logger);
        snapExecutor = new SnapExecutor(logger, data);
    }

    public VertexSnapMode Mode => VertexSnapMode.Snapping;

    public void OnEnter()
    {
        logger.LogMethodEntry(nameof(OnEnter));
        logger.LogObjectCount("storedSelectedItems", data.StoredSelectedItems.Count);
        logger.LogVariableValue("storedVertexPosition", data.StoredVertexPosition);

        // Ensure cursor is at stored position
        EnsureCursorAtStoredPosition();

        // Apply origin materials to show which objects are being moved
        ApplyOriginMaterials();

        logger.LogMethodExit(nameof(OnEnter));
    }

    public void OnExit()
    {
        logger.LogMethodEntry(nameof(OnExit));

        // Restore original materials
        materialManager.RestoreOriginMaterials();

        // Clean up holograms
        hologramManager.DestroyAllHolograms();

        logger.LogMethodExit(nameof(OnExit));
    }

    public void Update()
    {
        // Update material pulse effects
        materialManager.UpdateOriginMaterialPulse();
        materialManager.UpdateHologramPulse();
    }

    public void HandleInput(bool keyDown, bool leftMousePressed)
    {
        logger.LogMethodEntry(nameof(HandleInput), $"keyDown: {keyDown}, leftMouse: {leftMousePressed}");

        // Handle exit conditions first
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            logger.LogDebug("Escape key pressed");
            stateMachine.TransitionTo(VertexSnapMode.Inactive, "Escape key pressed");
            logger.LogMethodExit(nameof(HandleInput));
            return;
        }

        if (Input.GetMouseButtonDown(2))
        {
            logger.LogDebug("Middle mouse button pressed");
            stateMachine.TransitionTo(VertexSnapMode.Inactive, "Middle click to cancel");
            logger.LogMethodExit(nameof(HandleInput));
            return;
        }

        // Handle snap confirmation
        if (leftMousePressed && data.Holograms.Count > 0)
        {
            logger.LogDebug("Left click with holograms present - confirming snap");
            bool snapSuccess = snapExecutor.PerformSnap();

            if (snapSuccess)
            {
                stateMachine.TransitionTo(VertexSnapMode.Inactive, $"Snap confirmed with left click ({data.StoredSelectedItems.Count} objects moved)");
            }
            else
            {
                logger.LogWarning("Snap execution failed");
            }

            logger.LogMethodExit(nameof(HandleInput));
            return;
        }

        // Handle hologram updates
        if (keyDown)
        {
            UpdateHolograms();
        }
        else
        {
            logger.LogDebug("Key released, hiding holograms");
            hologramManager.DestroyAllHolograms();
        }

        logger.LogMethodExit(nameof(HandleInput));
    }

    private void EnsureCursorAtStoredPosition()
    {
        logger.LogMethodEntry(nameof(EnsureCursorAtStoredPosition));

        if (data.Cursor != null)
        {
            data.Cursor.position = data.StoredVertexPosition;
            logger.LogVariableValue("cursor moved to", data.StoredVertexPosition);
        }
        else
        {
            logger.LogDebug("Creating cursor at stored position");
            cursorManager.CreateCursor();
            if (data.Cursor != null)
            {
                data.Cursor.position = data.StoredVertexPosition;
            }
        }

        logger.LogMethodExit(nameof(EnsureCursorAtStoredPosition));
    }

    private void ApplyOriginMaterials()
    {
        logger.LogMethodEntry(nameof(ApplyOriginMaterials));

        if (data.OriginRenderers.Count == 0 && data.StoredSelectedItems.Count > 0)
        {
            materialManager.ApplyOriginMaterials();
            logger.LogObjectCount("origin renderers applied", data.OriginRenderers.Count);
        }
        else
        {
            logger.LogDebug("Origin materials already applied or no stored items");
        }

        logger.LogMethodExit(nameof(ApplyOriginMaterials));
    }

    private void UpdateHolograms()
    {
        logger.LogMethodEntry(nameof(UpdateHolograms));

        Ray ray = data.Camera.ScreenPointToRay(Input.mousePosition);
        if (raycastHelper.TryFindTargetVertex(ray, data.Cursor, data.Holograms, data.StoredSelectedItems, out Vector3 targetVertex))
        {
            logger.LogVariableValue("targetVertex found", targetVertex);

            hologramManager.CreateAllHolograms();
            hologramManager.UpdateHologramPositions(targetVertex, data.StoredVertexPosition);

            logger.LogObjectCount("active holograms", data.Holograms.Count);
        }
        else
        {
            logger.LogDebug("No target vertex found, destroying holograms");
            hologramManager.DestroyAllHolograms();
        }

        logger.LogMethodExit(nameof(UpdateHolograms));
    }
}