using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.Rendering;
using ZeepSDK.LevelEditor;
using ZeepSDK.Messaging;

namespace VertexSnapper;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const int MAX_HITS = 16;
    private const string BLOCK_TAG = "BuildingBlock";
    private const float VISIBILITY_UPDATE_INTERVAL = 0.1f;

    private static readonly RaycastHit[] hits = new RaycastHit[MAX_HITS];

    private static readonly List<string> before = new List<string> { string.Empty };
    private static readonly List<string> beforeSelection = new List<string> { string.Empty };
    private static readonly List<string> after = new List<string> { string.Empty };
    private static readonly List<string> afterSelection = new List<string> { string.Empty };
    private Camera cam;

    private LEV_LevelEditorCentral central;

    private VertexMode currentMode = VertexMode.Inactive;

    private Transform cursor;

    // Simplified visibility management
    private readonly HashSet<Renderer> hiddenRenderers = new HashSet<Renderer>();
    private GameObject hologram;
    private bool isDragging;

    private bool isInEditor;
    private ConfigEntry<KeyCode> key;
    private float lastVisibilityUpdateTime;
    private ConfigEntry<float> maxDistance;
    private MeshFilter[] meshFilters;
    private BlockProperties selectedItem;

    private ConfigEntry<float> selectionRadius;
    private BlockProperties storedSelectedItem;
    private Vector3 storedVertexPosition;
    private Vector3 storedVertOffset;
    private Vector3 vertOffset;
    private bool wasKeyDownLastFrame;

    private Transform Target => selectedItem == null ? null : selectedItem.transform;

    private void Awake()
    {
        LevelEditorApi.EnteredLevelEditor += EnteredLevelEditor;
        LevelEditorApi.ExitedLevelEditor += ExitedLevelEditor;

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        selectionRadius = Config.Bind("General",
            "Selection Radiuses",
            0.5f);

        maxDistance = Config.Bind("General",
            "Max Distance",
            300.0f,
            "Maximum Distance from camera to look for vertices"
        );

        key = Config.Bind("Keybinds",
            "Activation Key",
            KeyCode.LeftControl,
            "Holding down this key enables the vertex snapper");
    }

    private void Update()
    {
        if (!isInEditor)
        {
            return;
        }

        bool keyDown = Input.GetKey(key.Value);
        bool leftMousePressed = Input.GetMouseButtonDown(0);

        // Handle mode transitions
        HandleModeTransitions(keyDown, leftMousePressed);

        // Handle visibility based on key state (only when not in snapping mode)
        if (currentMode != VertexMode.Snapping)
        {
            if (keyDown != wasKeyDownLastFrame)
            {
                if (keyDown)
                {
                    lastVisibilityUpdateTime = 0; // Force immediate update
                }
                else
                {
                    RestoreAllVisibility();
                }

                wasKeyDownLastFrame = keyDown;
            }

            if (keyDown && Time.time - lastVisibilityUpdateTime > VISIBILITY_UPDATE_INTERVAL)
            {
             
                lastVisibilityUpdateTime = Time.time;
            }
        }

        // Handle mouse input blocking
        if (central != null)
        {
            if (keyDown || currentMode == VertexMode.Snapping)
            {
                LevelEditorApi.BlockMouseInput(this);
            }
            else
            {
                LevelEditorApi.UnblockMouseInput(this);
            }
        }

        // Handle different modes
        switch (currentMode)
        {
            case VertexMode.Inactive:
                HandleInactiveMode(keyDown);
                break;
            case VertexMode.Positioning:
                HandlePositioningMode(keyDown, leftMousePressed);
                break;
            case VertexMode.Snapping:
                HandleSnappingMode(keyDown, leftMousePressed);
                break;
        }
    }

    private void OnDestroy()
    {
        LevelEditorApi.EnteredLevelEditor -= EnteredLevelEditor;
        LevelEditorApi.ExitedLevelEditor -= ExitedLevelEditor;

        RestoreAllVisibility();
        DestroyHologram();
    }

    private void SetMode(VertexMode newMode, string reason)
    {
        if (currentMode == newMode)
        {
            return;
        }

        VertexMode oldMode = currentMode;
        currentMode = newMode;

        MessengerApi.Log($"[VERTEX] {oldMode} -> {newMode} ({reason})");
    }

    private void HandleModeTransitions(bool keyDown, bool leftMousePressed)
    {
        switch (currentMode)
        {
            case VertexMode.Inactive:
                if (keyDown && central != null && central.selection.list.Count == 1 && Target != null)
                {
                    SetMode(VertexMode.Positioning, "Key pressed with valid selection");
                }

                break;

            case VertexMode.Positioning:
                if (keyDown && leftMousePressed && cursor != null)
                {
                    // Store current state and enter snapping mode
                    storedVertexPosition = cursor.position;
                    storedVertOffset = vertOffset;
                    storedSelectedItem = selectedItem;
                    SetMode(VertexMode.Snapping, "Left click while positioning cursor");

                    // Clear selection to allow free roaming
                    if (central != null)
                    {
                        central.selection.DeselectAllBlocks(true, "");
                    }
                }
                else if (!keyDown)
                {
                    SetMode(VertexMode.Inactive, "Key released during positioning");
                }

                break;

            case VertexMode.Snapping:
                if (leftMousePressed && hologram != null)
                {
                    // Confirm the snap
                    PerformSnap();
                    SetMode(VertexMode.Inactive, "Snap confirmed with left click");
                }
                // Exit snapping mode with Escape or right click
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    SetMode(VertexMode.Inactive, "Escape key pressed");
                }

                break;
        }
    }

    private void HandleInactiveMode(bool keyDown)
    {
        if (!keyDown)
        {
            DestroyCursor();
            DestroyHologram();
        }
    }

    private void HandlePositioningMode(bool keyDown, bool leftMousePressed)
    {
        if (!keyDown)
        {
            DestroyCursor();
            return;
        }

        if (central == null || central.selection.list.Count != 1 || Target == null)
        {
            SetMode(VertexMode.Inactive, "Lost valid selection during positioning");
            return;
        }

        CreateCursor();

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!TryGetWorldPoint(ray, out Vector3 worldPoint))
        {
            return;
        }

        cursor.position = GetClosestVert(meshFilters, worldPoint);
        CalculateVertOffset();
    }

    private void HandleSnappingMode(bool keyDown, bool leftMousePressed)
    {
        // Keep cursor at stored position
        if (cursor != null)
        {
            cursor.position = storedVertexPosition;
        }
        else
        {
            CreateCursor();
            cursor.position = storedVertexPosition;
        }

        // Handle hologram
        if (keyDown)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (TryFindTargetVertex(ray, out Vector3 targetVertex))
            {
                CreateHologram();
                Vector3 targetPosition = targetVertex + storedVertOffset;
                hologram.transform.position = targetPosition;
                hologram.transform.rotation = storedSelectedItem.transform.rotation;
            }
            else
            {
                DestroyHologram();
            }
        }
        else
        {
            DestroyHologram();
        }
    }

    private bool TryFindTargetVertex(Ray ray, out Vector3 targetVertex)
    {
        targetVertex = Vector3.zero;

        int amountOfHits = Physics.RaycastNonAlloc(ray, hits, maxDistance.Value);
        int min = Math.Min(MAX_HITS, amountOfHits);

        for (int i = 0; i < min; i++)
        {
            RaycastHit hit = hits[i];

            Transform hitTransform = hit.transform.root;
            if (hitTransform == null)
            {
                hitTransform = hit.transform;
            }

            if (hitTransform == cursor.transform || hitTransform == hologram?.transform)
            {
                continue;
            }

            // Skip the original selected object
            if (storedSelectedItem != null && (hitTransform == storedSelectedItem.transform || hitTransform.IsChildOf(storedSelectedItem.transform)))
            {
                continue;
            }

            if (!hitTransform.CompareTag(BLOCK_TAG))
            {
                continue;
            }

            MeshFilter[] hitMeshFilters = hitTransform.GetComponentsInChildren<MeshFilter>();
            targetVertex = GetClosestVert(hitMeshFilters, hit.point);
            return true;
        }

        return false;
    }

    private void PerformSnap()
    {
        if (hologram == null || storedSelectedItem == null)
        {
            return;
        }

        Vector3 targetPosition = hologram.transform.position;
        float distance = Vector3.Distance(targetPosition, storedSelectedItem.transform.position);

        if (distance < 0.01f)
        {
            DestroyHologram();
            MessengerApi.Log("[VERTEX] Snap cancelled (too close to current position)");
            return;
        }

        List<BlockProperties> selection = new List<BlockProperties> { storedSelectedItem };

        before[0] = storedSelectedItem.ConvertBlockToJSON_v15_string(true);
        beforeSelection[0] = selection[0].UID;

        storedSelectedItem.transform.position = targetPosition;

        after[0] = storedSelectedItem.ConvertBlockToJSON_v15_string(true);
        afterSelection[0] = selection[0].UID;

        Change_Collection changeCollection = central.undoRedo.ConvertBeforeAndAfterListToCollection(
            before,
            after,
            selection,
            beforeSelection,
            afterSelection);

        central.validation.BreakLock(changeCollection, "Gizmo6");

        DestroyHologram();
        MessengerApi.Log("[VERTEX] Object snapped successfully!");
    }

    private void CreateHologram()
    {
        if (hologram != null || storedSelectedItem == null)
        {
            return;
        }

        // Create a visual copy of the selected item
        hologram = new GameObject("VertexSnapHologram");

        // Copy all renderers from the original object
        Renderer[] originalRenderers = storedSelectedItem.GetComponentsInChildren<Renderer>();
        foreach (Renderer originalRenderer in originalRenderers)
        {
            GameObject hologramChild = new GameObject(originalRenderer.name + "_Hologram");
            hologramChild.transform.SetParent(hologram.transform);
            hologramChild.transform.localPosition = storedSelectedItem.transform.InverseTransformPoint(originalRenderer.transform.position);
            hologramChild.transform.localRotation = Quaternion.Inverse(storedSelectedItem.transform.rotation) * originalRenderer.transform.rotation;
            hologramChild.transform.localScale = originalRenderer.transform.lossyScale;

            MeshRenderer hologramRenderer = hologramChild.AddComponent<MeshRenderer>();
            MeshFilter hologramFilter = hologramChild.AddComponent<MeshFilter>();

            // Copy mesh
            MeshFilter originalFilter = originalRenderer.GetComponent<MeshFilter>();
            if (originalFilter != null)
            {
                hologramFilter.sharedMesh = originalFilter.sharedMesh;
            }

            // Create hologram materials
            Material[] hologramMaterials = new Material[originalRenderer.materials.Length];
            for (int i = 0; i < originalRenderer.materials.Length; i++)
            {
                Material hologramMat = new Material(originalRenderer.materials[i]);
                SetMaterialToTransparent(hologramMat);
                Color color = hologramMat.color;
                color.a = 0.5f;
                hologramMat.color = color;
                hologramMaterials[i] = hologramMat;
            }

            hologramRenderer.materials = hologramMaterials;
        }
    }

    private void DestroyHologram()
    {
        if (hologram != null)
        {
            Destroy(hologram);
            hologram = null;
        }
    }

    private void EnteredLevelEditor()
    {
        TryFindCentral();
        isInEditor = true;
        MessengerApi.Log("[VERTEX] Entered level editor");
    }

    private void ExitedLevelEditor()
    {
        isInEditor = false;
        RestoreAllVisibility();
        DestroyHologram();
        SetMode(VertexMode.Inactive, "Exited level editor");
        wasKeyDownLastFrame = false;
    }


    private void RestoreAllVisibility()
    {
        foreach (Renderer renderer in hiddenRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }

        hiddenRenderers.Clear();
    }

    private void SetMaterialToTransparent(Material material)
    {
        material.SetOverrideTag("RenderType", "Transparent");
        // material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        // material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        // material.SetInt("_ZWrite", 0);
        // material.DisableKeyword("_ALPHATEST_ON");
        // material.EnableKeyword("_ALPHABLEND_ON");
        // material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
 
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private void DestroyCursor()
    {
        if (cursor != null)
        {
            Destroy(cursor.gameObject);
        }
    }

    private void CreateCursor()
    {
        if (cursor != null)
        {
            return;
        }

        cursor = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        cursor.localScale = Vector3.one * selectionRadius.Value;
        Renderer ren = cursor.GetComponent<Renderer>();
        ren.material = new Material(Shader.Find("Standard"));
        ren.material.color = new Color(1, 1, 1, 0.75f);
        SetMaterialToTransparent(ren.material);
        Collider col = cursor.GetComponent<Collider>();
        col.enabled = false;
    }

    private void TryFindCentral()
    {
        central = FindObjectOfType<LEV_LevelEditorCentral>();

        if (central == null)
        {
            return;
        }

        central.selection.ThingsJustGotSelected.AddListener(ItemGotSelected);
        central.selection.ThingsJustGotDeselected.AddListener(ItemGotDeselected);
        cam = central.cam.cameraCamera;
    }

    private void ItemGotSelected()
    {
        ProcessItemSelection();
    }

    private void ItemGotDeselected()
    {
        ProcessItemSelection();
    }

    private void ProcessItemSelection()
    {
        if (central.selection.list.Count == 1)
        {
            selectedItem = central.selection.list[0];
            meshFilters = Target.GetComponentsInChildren<MeshFilter>();
        }
        else
        {
            selectedItem = null;
            meshFilters = null;
        }
    }

    private void CalculateVertOffset()
    {
        vertOffset = Target.position - cursor.position;
    }

    private Vector3 GetClosestVert(IEnumerable<MeshFilter> meshFilters, Vector3 target)
    {
        float distance = float.MaxValue;
        Vector3 destination = target;

        foreach (MeshFilter meshFilter in meshFilters)
        {
            foreach (Vector3 vertex in meshFilter.sharedMesh.vertices)
            {
                Vector3 transformedVertex = meshFilter.transform.TransformPoint(vertex);
                float dist = Vector3.Distance(transformedVertex, target);
                if (dist < distance)
                {
                    distance = dist;
                    destination = transformedVertex;
                }
            }
        }

        return destination;
    }

    private bool TryGetWorldPoint(Ray ray, out Vector3 worldPoint)
    {
        int amountOfHits = Physics.RaycastNonAlloc(ray, hits);
        int min = Math.Min(MAX_HITS, amountOfHits);
        for (int i = 0; i < min; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.transform == cursor.transform)
            {
                continue;
            }

            if (hit.transform != Target && !hit.transform.IsChildOf(Target))
            {
                continue;
            }

            if (!hit.transform.CompareTag(BLOCK_TAG))
            {
                continue;
            }

            worldPoint = hit.point;
            return true;
        }

        Vector3 normal = (cam.transform.position - Target.transform.position).normalized;
        Plane p = new Plane(normal, Target.transform.position);
        if (p.Raycast(ray, out float enter))
        {
            worldPoint = ray.GetPoint(enter);
            return true;
        }

        Logger.LogWarning("Unable to get world point");
        worldPoint = Vector3.zero;
        return false;
    }

    // New vertex mode system
    private enum VertexMode
    {
        Inactive,
        Positioning, // Phase 1: Positioning the cursor on selected object
        Snapping // Phase 2: Roaming around to find target location
    }
}