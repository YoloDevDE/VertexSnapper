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
        logger.LogInfo($"VertexSnapper v{MyPluginInfo.PLUGIN_VERSION} starting up");

        // Initialize core components
        data = new VertexSnapData();
        stateMachine = new VertexSnapStateMachine(logger, data);

        // Subscribe to level editor events
        LevelEditorApi.EnteredLevelEditor += OnEnteredLevelEditor;
        LevelEditorApi.ExitedLevelEditor += OnExitedLevelEditor;
        LevelEditorApi.ItemGotSelected += OnItemSelected;
        LevelEditorApi.ItemGotDeselected += OnItemDeselected;

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
        LevelEditorApi.ItemGotSelected -= OnItemSelected;
        LevelEditorApi.ItemGotDeselected -= OnItemDeselected;
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

    private void OnItemSelected(BlockProperties item)
    {
        if (item != null && !data.SelectedItems.Contains(item))
        {
            data.SelectedItems.Add(item);
            logger.LogDebug($"Item selected: {item.name} (total: {data.SelectedItems.Count})");

            // If this is the first selected item, make it the current target
            if (data.SelectedItems.Count == 1)
            {
                data.CurrentTarget = item;
                if (item.transform != null)
                {
                    data.MeshFilters = item.transform.GetComponentsInChildren<MeshFilter>();
                }
            }
        }
    }

    private void OnItemDeselected(BlockProperties item)
    {
        if (item != null && data.SelectedItems.Contains(item))
        {
            data.SelectedItems.Remove(item);
            logger.LogDebug($"Item deselected: {item.name} (total: {data.SelectedItems.Count})");

            // If we deselected the current target, pick a new one
            if (data.CurrentTarget == item)
            {
                if (data.SelectedItems.Count > 0)
                {
                    data.CurrentTarget = data.SelectedItems[0];
                    if (data.CurrentTarget?.transform != null)
                    {
                        data.MeshFilters = data.CurrentTarget.transform.GetComponentsInChildren<MeshFilter>();
                    }
                }
                else
                {
                    data.CurrentTarget = null;
                    data.MeshFilters = null;
                }
            }

            // If no items are selected and we're active, return to inactive
            if (data.SelectedItems.Count == 0 && stateMachine.IsActive)
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