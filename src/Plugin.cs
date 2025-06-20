using System;
using System.Collections.Generic;
using System.Linq;
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
    private const float PULSE_SPEED = 6f; // Speed of the pulse
    private const float MIN_ALPHA = 0.2f; // Minimum opacity
    private const float MAX_ALPHA = 0.8f; // Maximum opacity

    private static readonly RaycastHit[] hits = new RaycastHit[MAX_HITS];

    private static readonly List<string> before = new List<string>();
    private static readonly List<string> beforeSelection = new List<string>();
    private static readonly List<string> after = new List<string>();
    private static readonly List<string> afterSelection = new List<string>();

    // Simplified visibility management
    private readonly HashSet<Renderer> hiddenRenderers = new HashSet<Renderer>();
    private readonly List<GameObject> holograms = new List<GameObject>();

    // Add these fields with your other private fields
    private readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private readonly List<Renderer> originRenderers = new List<Renderer>();
    private readonly List<BlockProperties> selectedItems = new List<BlockProperties>();
    private readonly List<Vector3> storedRelativePositions = new List<Vector3>();
    private readonly List<BlockProperties> storedSelectedItems = new List<BlockProperties>();
    private Camera cam;

    private LEV_LevelEditorCentral central;

    private VertexMode currentMode = VertexMode.Inactive;
    private BlockProperties currentTarget; // The object we're currently pointing at

    private Transform cursor;
    private bool isDragging;

    private bool isInEditor;
    private ConfigEntry<KeyCode> key;
    private float lastVisibilityUpdateTime;
    private MeshFilter[] meshFilters;

    // Add these fields at the top with your other private fields
    private float pulseTime;

    private ConfigEntry<float> selectionRadius;
    private BlockProperties storedPrimaryTarget; // The object that was used as anchor
    private Vector3 storedVertexPosition;
    private Vector3 storedVertOffset;
    private Vector3 vertOffset;
    private bool wasKeyDownLastFrame;

    private Transform Target => currentTarget?.transform;

    private void Awake()
    {
        LevelEditorApi.EnteredLevelEditor += EnteredLevelEditor;
        LevelEditorApi.ExitedLevelEditor += ExitedLevelEditor;

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        selectionRadius = Config.Bind("General",
            "Selection Radiuses",
            0.5f);


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

        // Update pulse time for hologram materials
        pulseTime += Time.deltaTime;
        UpdateHologramPulse();

        // Update current target based on what we're pointing at
        if (currentMode == VertexMode.Positioning)
        {
            UpdateCurrentTarget();
        }

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
        DestroyAllHolograms();
    }

    private void UpdateHologramPulse()
    {
        if (holograms.Count == 0)
        {
            return;
        }

        // Calculate pulsing alpha value using sine wave
        float alpha = Mathf.Lerp(MIN_ALPHA, MAX_ALPHA, (Mathf.Sin(pulseTime * PULSE_SPEED) + 1f) * 0.5f);

        // Update all hologram materials
        foreach (GameObject hologram in holograms)
        {
            if (hologram == null)
            {
                continue;
            }

            Renderer[] renderers = hologram.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.materials)
                {
                    if (material != null)
                    {
                        Color color = material.color;
                        color.a = alpha;
                        material.color = color;

                        // Also update emission color alpha for better effect
                        if (material.IsKeywordEnabled("_EMISSION"))
                        {
                            Color emissionColor = material.GetColor("_EmissionColor");
                            emissionColor.a = alpha * 0.5f; // Softer emission pulse
                            material.SetColor("_EmissionColor", emissionColor);
                        }
                    }
                }
            }
        }
    }

    private void UpdateCurrentTarget()
    {
        if (selectedItems.Count == 0)
        {
            return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        int amountOfHits = Physics.RaycastNonAlloc(ray, hits);
        int min = Math.Min(MAX_HITS, amountOfHits);

        BlockProperties newTarget = null;

        for (int i = 0; i < min; i++)
        {
            RaycastHit hit = hits[i];

            if (hit.transform == cursor?.transform)
            {
                continue;
            }


            // Check if this hit belongs to any of our selected items
            foreach (BlockProperties item in selectedItems)
            {
                if (item != null && (hit.transform == item.transform || hit.transform.IsChildOf(item.transform)))
                {
                    newTarget = item;
                    break;
                }
            }

            if (newTarget != null)
            {
                break;
            }
        }

        // If we didn't hit any selected object, keep the current target or use the first one
        if (newTarget == null)
        {
            if (currentTarget == null || !selectedItems.Contains(currentTarget))
            {
                currentTarget = selectedItems.FirstOrDefault();
            }
        }
        else if (newTarget != currentTarget)
        {
            currentTarget = newTarget;
            // Update mesh filters when target changes
            if (Target != null)
            {
                meshFilters = Target.GetComponentsInChildren<MeshFilter>();
                MessengerApi.Log($"[VERTEX] Target switched to: {currentTarget.name}");
            }
        }
    }

    private void SetMode(VertexMode newMode, string reason)
    {
        if (currentMode == newMode)
        {
            return;
        }

        VertexMode oldMode = currentMode;

        // Handle mode exit cleanup
        if (oldMode == VertexMode.Snapping && newMode != VertexMode.Snapping)
        {
            RestoreOriginMaterials();
        }

        currentMode = newMode;

        MessengerApi.Log($"[VERTEX] {oldMode} -> {newMode} ({reason})");
    }

    private void HandleModeTransitions(bool keyDown, bool leftMousePressed)
    {
        switch (currentMode)
        {
            case VertexMode.Inactive:
                if (keyDown && central != null && selectedItems.Count > 0)
                {
                    // Set initial target to first selected item if none is set
                    if (currentTarget == null || !selectedItems.Contains(currentTarget))
                    {
                        currentTarget = selectedItems.FirstOrDefault();
                        if (Target != null)
                        {
                            meshFilters = Target.GetComponentsInChildren<MeshFilter>();
                        }
                    }

                    SetMode(VertexMode.Positioning, $"Key pressed with {selectedItems.Count} object(s) selected");
                }

                break;

            case VertexMode.Positioning:
                if (keyDown && leftMousePressed && cursor != null && currentTarget != null)
                {
                    // Store current state and enter snapping mode
                    storedVertexPosition = cursor.position;
                    storedVertOffset = vertOffset;
                    storedPrimaryTarget = currentTarget;
                    storedSelectedItems.Clear();
                    storedSelectedItems.AddRange(selectedItems);

                    // Calculate relative positions of all selected objects to the current target
                    storedRelativePositions.Clear();
                    Vector3 primaryPosition = storedPrimaryTarget.transform.position;
                    foreach (BlockProperties item in storedSelectedItems)
                    {
                        storedRelativePositions.Add(item.transform.position - primaryPosition);
                    }

                    SetMode(VertexMode.Snapping, $"Left click while positioning cursor on {currentTarget.name} ({storedSelectedItems.Count} objects stored)");

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
                if (leftMousePressed && holograms.Count > 0)
                {
                    // Confirm the snap
                    PerformSnap();
                    SetMode(VertexMode.Inactive, $"Snap confirmed with left click ({storedSelectedItems.Count} objects moved)");
                }
                // Exit snapping mode with Escape or right click
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    SetMode(VertexMode.Inactive, "Escape key pressed");
                }
                else if (Input.GetMouseButtonDown(2))
                {
                    SetMode(VertexMode.Inactive, "Middle click to cancel");
                }

                break;
        }
    }

    private void HandleInactiveMode(bool keyDown)
    {
        if (!keyDown)
        {
            DestroyCursor();
            DestroyAllHolograms();
            currentTarget = null;
        }
    }

    private void HandlePositioningMode(bool keyDown, bool leftMousePressed)
    {
        if (!keyDown)
        {
            DestroyCursor();
            currentTarget = null;
            return;
        }

        if (central == null || selectedItems.Count == 0 || Target == null)
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

        // Apply green emission to original blocks when entering snapping mode
        if (originRenderers.Count == 0 && storedSelectedItems.Count > 0)
        {
            ApplyOriginMaterials();
        }

        // Update the pulse effect for origin materials
        UpdateOriginMaterialPulse();

        // Handle holograms
        if (keyDown)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (TryFindTargetVertex(ray, out Vector3 targetVertex))
            {
                CreateAllHolograms();
                UpdateHologramPositions(targetVertex);
            }
            else
            {
                DestroyAllHolograms();
            }
        }
        else
        {
            DestroyAllHolograms();
        }
    }

    private void UpdateHologramPositions(Vector3 targetVertex)
    {
        if (holograms.Count != storedSelectedItems.Count || storedPrimaryTarget == null)
        {
            return;
        }

        // Einfacher Ansatz: Berechne die Bewegung direkt zwischen den Vertices
        Vector3 movement = targetVertex - storedVertexPosition;

        // Update all holograms based on their stored relative positions
        for (int i = 0; i < holograms.Count; i++)
        {
            if (holograms[i] != null)
            {
                Vector3 newPosition = storedSelectedItems[i].transform.position + movement;
                holograms[i].transform.position = newPosition;
                holograms[i].transform.rotation = storedSelectedItems[i].transform.rotation;
            }
        }
    }

    private bool TryFindTargetVertex(Ray ray, out Vector3 targetVertex)
    {
        targetVertex = Vector3.zero;

        int amountOfHits = Physics.RaycastNonAlloc(ray, hits);
        int min = Math.Min(MAX_HITS, amountOfHits);

        for (int i = 0; i < min; i++)
        {
            RaycastHit hit = hits[i];

            Transform hitTransform = hit.transform.root;
            if (hitTransform == null)
            {
                hitTransform = hit.transform;
            }

            if (hitTransform == cursor.transform)
            {
                continue;
            }

            // Skip hologram objects
            if (holograms.Any(h => h != null && (hitTransform == h.transform || hitTransform.IsChildOf(h.transform))))
            {
                continue;
            }

            // Skip any of the original selected objects
            if (storedSelectedItems.Any(item => item != null && (hitTransform == item.transform || hitTransform.IsChildOf(item.transform))))
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
        if (holograms.Count == 0 || storedSelectedItems.Count == 0 || holograms.Count != storedSelectedItems.Count)
        {
            MessengerApi.Log("[VERTEX] Snap failed - hologram/selection mismatch");
            return;
        }

        // Check if any object would move a significant distance
        bool anySignificantMove = false;
        for (int i = 0; i < storedSelectedItems.Count; i++)
        {
            if (storedSelectedItems[i] == null || holograms[i] == null)
            {
                continue;
            }

            float distance = Vector3.Distance(holograms[i].transform.position, storedSelectedItems[i].transform.position);
            if (distance >= 0.01f)
            {
                anySignificantMove = true;
                break;
            }
        }

        if (!anySignificantMove)
        {
            DestroyAllHolograms();
            MessengerApi.Log("[VERTEX] Snap cancelled (too close to current positions)");
            return;
        }

        // Prepare undo/redo data
        before.Clear();
        beforeSelection.Clear();
        after.Clear();
        afterSelection.Clear();

        // Record before state and move objects
        for (int i = 0; i < storedSelectedItems.Count; i++)
        {
            if (storedSelectedItems[i] == null || holograms[i] == null)
            {
                continue;
            }

            before.Add(storedSelectedItems[i].ConvertBlockToJSON_v15_string(true));
            beforeSelection.Add(storedSelectedItems[i].UID);

            storedSelectedItems[i].transform.position = holograms[i].transform.position;

            after.Add(storedSelectedItems[i].ConvertBlockToJSON_v15_string(true));
            afterSelection.Add(storedSelectedItems[i].UID);
        }

        // Create undo/redo entry
        if (before.Count > 0)
        {
            Change_Collection changeCollection = central.undoRedo.ConvertBeforeAndAfterListToCollection(
                before,
                after,
                storedSelectedItems,
                beforeSelection,
                afterSelection);

            central.validation.BreakLock(changeCollection, "Gizmo6");
        }

        DestroyAllHolograms();
        MessengerApi.Log($"[VERTEX] {storedSelectedItems.Count} object(s) snapped successfully!");
    }

    private void CreateAllHolograms()
    {
        if (holograms.Count > 0 || storedSelectedItems.Count == 0)
        {
            return;
        }

        foreach (BlockProperties selectedItem in storedSelectedItems)
        {
            if (selectedItem == null)
            {
                continue;
            }

            GameObject hologram = CreateHologramForItem(selectedItem);
            holograms.Add(hologram);
        }
    }

    private GameObject CreateHologramForItem(BlockProperties item)
    {
        // Create a visual copy of the selected item
        GameObject hologram = new GameObject($"VertexSnapHologram_{item.name}");

        // Copy all renderers from the original object
        Renderer[] originalRenderers = item.GetComponentsInChildren<Renderer>();
        foreach (Renderer originalRenderer in originalRenderers)
        {
            GameObject hologramChild = new GameObject(originalRenderer.name + "_Hologram");
            hologramChild.transform.SetParent(hologram.transform);

            // Correct way to handle scaled objects:
            // Use world position and rotation directly, then convert to local space of hologram
            hologramChild.transform.position = originalRenderer.transform.position;
            hologramChild.transform.rotation = originalRenderer.transform.rotation;
            hologramChild.transform.localScale = originalRenderer.transform.localScale;

            // Now convert to local space relative to the hologram parent
            hologramChild.transform.SetParent(hologram.transform, true);

            MeshRenderer hologramRenderer = hologramChild.AddComponent<MeshRenderer>();
            MeshFilter hologramFilter = hologramChild.AddComponent<MeshFilter>();

            // Copy mesh
            MeshFilter originalFilter = originalRenderer.GetComponent<MeshFilter>();
            if (originalFilter != null)
            {
                hologramFilter.sharedMesh = originalFilter.sharedMesh;
            }

            // Create materials array to match the original renderer's material count
            Material[] hologramMaterials = new Material[originalRenderer.materials.Length];
            for (int i = 0; i < hologramMaterials.Length; i++)
            {
                hologramMaterials[i] = CreateWireframeMaterial();
            }

            // Assign all materials to the hologram renderer
            hologramRenderer.materials = hologramMaterials;
        }

        return hologram;
    }

    private Material CreateWireframeMaterial()
    {
        Material wireframeMat = new Material(Shader.Find("Standard"));

        // Set up as transparent wireframe
        ConfigureHologramMaterial(wireframeMat);

        // Make it a bright, semi-transparent color - start with mid-range alpha
        float initialAlpha = (MIN_ALPHA + MAX_ALPHA) * 0.5f;
        wireframeMat.color = new Color(0f, 1f, 0f, initialAlpha); // Cyan with initial alpha

        // Optional: Add emission for better visibility
        wireframeMat.EnableKeyword("_EMISSION");
        wireframeMat.SetColor("_EmissionColor", new Color(0f, 0.5f, 0f, initialAlpha * 0.5f));

        return wireframeMat;
    }

    private void DestroyAllHolograms()
    {
        foreach (GameObject hologram in holograms)
        {
            if (hologram != null)
            {
                Destroy(hologram);
            }
        }

        holograms.Clear();
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
        DestroyAllHolograms();
        SetMode(VertexMode.Inactive, "Exited level editor");
        currentTarget = null;
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
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
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
        selectedItems.Clear();

        if (central != null && central.selection.list.Count > 0)
        {
            selectedItems.AddRange(central.selection.list);
        }

        // Reset current target when selection changes
        currentTarget = null;
        meshFilters = null;
    }

    private void CalculateVertOffset()
    {
        if (Target != null)
        {
            vertOffset = Target.position - cursor.position;
        }
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

            // Check if hit belongs to the current target
            if (currentTarget != null && (hit.transform == currentTarget.transform || hit.transform.IsChildOf(currentTarget.transform)))
            {
                if (hit.transform.CompareTag(BLOCK_TAG))
                {
                    worldPoint = hit.point;
                    return true;
                }
            }
        }

        // Fallback to plane intersection with the current target
        if (Target != null)
        {
            Vector3 normal = (cam.transform.position - Target.transform.position).normalized;
            Plane p = new Plane(normal, Target.transform.position);
            if (p.Raycast(ray, out float enter))
            {
                worldPoint = ray.GetPoint(enter);
                return true;
            }
        }

        Logger.LogWarning("Unable to get world point");
        worldPoint = Vector3.zero;
        return false;
    }

    private void ConfigureHologramMaterial(Material material)
    {
        // Configure for transparent rendering
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.renderQueue = (int)RenderQueue.Transparent;

        // Setup alpha blending
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        // Optimize for hologram appearance
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Glossiness", 0.1f); // Slight shine for visibility
    }

    private void ApplyOriginMaterials()
    {
        if (storedSelectedItems.Count == 0)
        {
            return;
        }

        // Apply materials to all stored selected items
        foreach (BlockProperties item in storedSelectedItems)
        {
            if (item == null)
            {
                continue;
            }

            Renderer[] renderers = item.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                // Store original materials
                originalMaterials[renderer] = renderer.materials;
                originRenderers.Add(renderer);

                // Create new pulsing materials
                Material[] originMaterials = new Material[renderer.materials.Length];
                for (int i = 0; i < originMaterials.Length; i++)
                {
                    originMaterials[i] = CreateOriginMaterial();
                }

                // Apply the new materials
                renderer.materials = originMaterials;
            }
        }
    }

    private Material CreateOriginMaterial()
    {
        Material originMat = new Material(Shader.Find("Standard"));

        // Set up as transparent
        ConfigureHologramMaterial(originMat);

        // Green color for origin objects
        float initialAlpha = (MIN_ALPHA + MAX_ALPHA) * 0.5f;
        originMat.color = new Color(0f, 1f, 0f, initialAlpha); // Bright green

        originMat.EnableKeyword("_EMISSION");
        originMat.SetColor("_EmissionColor", new Color(0f, 0.8f, 0f, initialAlpha * 0.7f)); // Green emission

        return originMat;
    }

    private void RestoreOriginMaterials()
    {
        foreach (KeyValuePair<Renderer, Material[]> kvp in originalMaterials)
        {
            if (kvp.Key != null)
            {
                kvp.Key.materials = kvp.Value;
            }
        }

        originalMaterials.Clear();
        originRenderers.Clear();
    }

    private void UpdateOriginMaterialPulse()
    {
        if (originRenderers.Count == 0)
        {
            return;
        }

        // Use a different phase for origin materials so they pulse differently
        float originAlpha = Mathf.Lerp(MIN_ALPHA, MAX_ALPHA, (Mathf.Sin(pulseTime * PULSE_SPEED + Mathf.PI) + 1f) * 0.5f);

        foreach (Renderer renderer in originRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            foreach (Material material in renderer.materials)
            {
                if (material != null)
                {
                    Color color = material.color;
                    color.a = originAlpha;
                    material.color = color;

                    if (material.IsKeywordEnabled("_EMISSION"))
                    {
                        Color emissionColor = material.GetColor("_EmissionColor");
                        emissionColor.a = originAlpha * 0.7f;
                        material.SetColor("_EmissionColor", emissionColor);
                    }
                }
            }
        }
    }

    // New vertex mode system
    private enum VertexMode
    {
        Inactive,
        Positioning, // Phase 1: Positioning the cursor on selected object
        Snapping // Phase 2: Roaming around to find target location
    }
}