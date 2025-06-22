using System.Collections.Generic;
using System.Linq;
using BepInEx;
using UnityEngine;
using VertexSnapper.Core;
using ZeepSDK.LevelEditor;

namespace VertexSnapper;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ZeepSDK")]
public class Plugin : BaseUnityPlugin
{
    private VertexSnapData data;

    // Core system components
    private VertexSnapLogger logger;
    private VertexSnapStateMachine stateMachine;

    // Input tracking
    private bool wasKeyDownLastFrame;

    private void Awake()
    {
        logger = new VertexSnapLogger();
        logger.LogInfo($"VertexSnapper {MyPluginInfo.PLUGIN_VERSION} starting up");

        // Initialize core components
        data = new VertexSnapData();
        stateMachine = new VertexSnapStateMachine(logger, data);

        // Subscribe to level editor events
        LevelEditorApi.EnteredLevelEditor += OnEnteredLevelEditor;
        LevelEditorApi.ExitedLevelEditor += OnExitedLevelEditor;
        LevelEditorApi.SelectionChanged += OnSelectionChanged;

        logger.LogInfo("VertexSnapper initialized successfully");
    }

    private void Update()
    {
        if (!data.IsInEditor || data.Camera == null)
        {
            return;
        }

        // Update camera reference
        UpdateCameraReference();

        // Get input state
        bool keyDown = Input.GetKey(KeyCode.V);
        bool leftMousePressed = Input.GetMouseButtonDown(0);

        // Update state machine
        stateMachine.Update();
        stateMachine.HandleInput(keyDown, leftMousePressed);

        // Track key state for next frame
        wasKeyDownLastFrame = keyDown;
    }

    private void OnDestroy()
    {
        logger?.LogInfo("VertexSnapper shutting down");

        // Force cleanup
        stateMachine?.TransitionTo(VertexSnapMode.Inactive, "Plugin destroyed");

        // Unsubscribe from events
        LevelEditorApi.EnteredLevelEditor -= OnEnteredLevelEditor;
        LevelEditorApi.ExitedLevelEditor -= OnExitedLevelEditor;
        LevelEditorApi.SelectionChanged -= OnSelectionChanged;
    }

    private void OnEnteredLevelEditor()
    {
        logger.LogInfo("Entered level editor");
        data.IsInEditor = true;

        // Find central reference
        data.Central = FindObjectOfType<LEV_LevelEditorCentral>();
        if (data.Central == null)
        {
            logger.LogWarning("Could not find LEV_LevelEditorCentral");
        }
        else
        {
            logger.LogDebug("Found LEV_LevelEditorCentral");
        }
    }

    private void OnExitedLevelEditor()
    {
        logger.LogInfo("Exited level editor");
        data.IsInEditor = false;

        // Force return to inactive state
        stateMachine.TransitionTo(VertexSnapMode.Inactive, "Exited level editor");

        // Clear references
        data.Central = null;
        data.Camera = null;
    }

    private void OnSelectionChanged(List<BlockProperties> selectedItems)
    {
        // Store previous selection for comparison
        List<BlockProperties> previousSelection = new List<BlockProperties>(data.SelectedItems);

        // Update current selection
        data.SelectedItems.Clear();
        if (selectedItems != null)
        {
            data.SelectedItems.AddRange(selectedItems);
        }

        logger.LogDebug($"Selection changed: {data.SelectedItems.Count} items selected");

        // Find newly selected items
        List<BlockProperties> newlySelected = data.SelectedItems.Except(previousSelection).ToList();
        foreach (BlockProperties item in newlySelected)
        {
            logger.LogDebug($"Item selected: {item.name}");
        }

        // Find deselected items
        List<BlockProperties> deselected = previousSelection.Except(data.SelectedItems).ToList();
        foreach (BlockProperties item in deselected)
        {
            logger.LogDebug($"Item deselected: {item.name}");
        }

        // Update current target
        if (data.SelectedItems.Count > 0)
        {
            // If current target is still selected, keep it
            if (data.CurrentTarget != null && data.SelectedItems.Contains(data.CurrentTarget))
            {
                // Keep current target
            }
            else
            {
                // Set new target to first selected item
                data.CurrentTarget = data.SelectedItems[0];
                if (data.CurrentTarget?.transform != null)
                {
                    data.MeshFilters = data.CurrentTarget.transform.GetComponentsInChildren<MeshFilter>();
                }

                logger.LogDebug($"New target set: {data.CurrentTarget.name}");
            }
        }
        else
        {
            // No items selected
            data.CurrentTarget = null;
            data.MeshFilters = null;

            // If we're active and no items are selected, return to inactive
            if (stateMachine.IsActive)
            {
                stateMachine.TransitionTo(VertexSnapMode.Inactive, "No items selected");
            }
        }
    }

    private void UpdateCameraReference()
    {
        if (data.Camera == null)
        {
            data.Camera = Camera.main;
            if (data.Camera != null)
            {
                logger.LogDebug("Camera reference found");
            }
        }
    }
}