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
    // Constants
    private const int MAX_HITS = 16;
    private const string BLOCK_TAG = "BuildingBlock";
    private const float PULSE_SPEED = 4f;
    private const float MIN_ALPHA = 0.1f;
    private const float MAX_ALPHA = 0.6f;

    // Static fields
    private static readonly RaycastHit[] hits = new RaycastHit[MAX_HITS];
    private static readonly List<string> before = new List<string>();
    private static readonly List<string> beforeSelection = new List<string>();
    private static readonly List<string> after = new List<string>();
    private static readonly List<string> afterSelection = new List<string>();

    // Collections
    private readonly HashSet<Renderer> hiddenRenderers = new HashSet<Renderer>();
    private readonly List<GameObject> holograms = new List<GameObject>();
    private readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private readonly List<Renderer> originRenderers = new List<Renderer>();
    private readonly List<BlockProperties> selectedItems = new List<BlockProperties>();
    private readonly List<Vector3> storedRelativePositions = new List<Vector3>();
    private readonly List<BlockProperties> storedSelectedItems = new List<BlockProperties>();

    // Core components
    private Camera cam;
    private LEV_LevelEditorCentral central;

    // State management
    private VertexMode currentMode = VertexMode.Inactive;
    private BlockProperties currentTarget;
    private Transform cursor;
    private ConfigEntry<string> hologramColor;
    private bool isInEditor;

    // Runtime data
    private MeshFilter[] meshFilters;
    private float pulseTime;
    private ConfigEntry<float> selectionRadius;
    private Transform snappingCursor; // New cursor for snapping mode
    private BlockProperties storedPrimaryTarget;
    private Vector3 storedVertexPosition;
    private Vector3 storedVertOffset;

    // Configuration
    private ConfigEntry<KeyCode> VertexActivationKey;
    private Vector3 vertOffset;
    private bool wasKeyDownLastFrame;

    // Properties
    private Transform Target => currentTarget?.transform;

    // Unity lifecycle methods
    private void Awake()
    {
        LevelEditorApi.EnteredLevelEditor += EnteredLevelEditor;
        LevelEditorApi.ExitedLevelEditor += ExitedLevelEditor;

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        // Configuration
        selectionRadius = Config.Bind("General", "Selection Radius", 0.5f);
        VertexActivationKey = Config.Bind("Keybinds", "Activation Key", KeyCode.LeftControl, "Holding down this 'VertexActivationKey' enables the vertex snapper");
        hologramColor = Config.Bind("Visual", "Hologram Color", "#00FFFF", "Color of the hologram preview in hex format (e.g., #00FFFF for cyan, #FF0000 for red)");
    }

    private void Update()
    {
        if (!isInEditor)
        {
            return;
        }

        bool keyDown = Input.GetKey(VertexActivationKey.Value);
        bool leftMousePressed = Input.GetMouseButtonDown(0);

        pulseTime += Time.deltaTime;
        UpdateAllPulseEffects();

        if (currentMode == VertexMode.Positioning)
        {
            UpdateCurrentTarget();
        }

        HandleModeTransitions(keyDown, leftMousePressed);
        HandleVisibilityAndInput(keyDown);
        HandleCurrentMode(keyDown, leftMousePressed);
    }

    private void OnDestroy()
    {
        LevelEditorApi.EnteredLevelEditor -= EnteredLevelEditor;
        LevelEditorApi.ExitedLevelEditor -= ExitedLevelEditor;
        CleanupAll();
    }

    // Event handlers
    private void EnteredLevelEditor()
    {
        TryFindCentral();
        isInEditor = true;
    }

    private void ExitedLevelEditor()
    {
        isInEditor = false;
        CleanupAll();
        SetMode(VertexMode.Inactive, "Exited level editor");
        currentTarget = null;
        wasKeyDownLastFrame = false;
    }

    private void ItemGotSelected()
    {
        ProcessItemSelection();
    }

    private void ItemGotDeselected()
    {
        ProcessItemSelection();
    }

    // Mode management
    private void SetMode(VertexMode newMode, string reason)
    {
        if (currentMode == newMode)
        {
            return;
        }

        VertexMode oldMode = currentMode;
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
                    InitializePositioningMode();
                }

                break;

            case VertexMode.Positioning:
                if (keyDown && leftMousePressed && cursor != null && currentTarget != null)
                {
                    InitializeSnappingMode();
                }
                else if (!keyDown)
                {
                    SetMode(VertexMode.Inactive, "Key released during positioning");
                }

                break;

            case VertexMode.Snapping:
                if (leftMousePressed && holograms.Count > 0)
                {
                    PerformSnap();
                    SetMode(VertexMode.Inactive, $"Snap confirmed with left click ({storedSelectedItems.Count} objects moved)");
                }
                else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(2))
                {
                    SetMode(VertexMode.Inactive, Input.GetKeyDown(KeyCode.Escape) ? "Escape VertexActivationKey pressed" : "Middle click to cancel");
                }

                break;
        }
    }

    private void InitializePositioningMode()
    {
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

    private void InitializeSnappingMode()
    {
        storedVertexPosition = cursor.position;
        storedVertOffset = vertOffset;
        storedPrimaryTarget = currentTarget;
        storedSelectedItems.Clear();
        storedSelectedItems.AddRange(selectedItems);

        storedRelativePositions.Clear();

        foreach (BlockProperties item in storedSelectedItems)
        {
            storedRelativePositions.Add(item.transform.position);
        }

        SetMode(VertexMode.Snapping, $"Left click while positioning cursor on {currentTarget.name} ({storedSelectedItems.Count} objects stored)");

        if (central != null)
        {
            central.selection.DeselectAllBlocks(true, "");
        }
    }

    private void HandleVisibilityAndInput(bool keyDown)
    {
        if (currentMode != VertexMode.Snapping && keyDown != wasKeyDownLastFrame)
        {
            if (!keyDown)
            {
                RestoreAllVisibility();
            }

            wasKeyDownLastFrame = keyDown;
        }

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
    }

    private void HandleCurrentMode(bool keyDown, bool leftMousePressed)
    {
        switch (currentMode)
        {
            case VertexMode.Inactive:
                if (!keyDown)
                {
                    CleanupAllCursorsAndHolograms();
                    currentTarget = null;
                }

                break;

            case VertexMode.Positioning:
                HandlePositioningMode(keyDown);
                break;

            case VertexMode.Snapping:
                HandleSnappingMode(keyDown);
                break;
        }
    }

    private void HandlePositioningMode(bool keyDown)
    {
        if (!keyDown)
        {
            CleanupAllCursorsAndHolograms();
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
        if (TryGetWorldPoint(ray, out Vector3 worldPoint))
        {
            cursor.position = GetClosestVert(meshFilters, worldPoint);
            CalculateVertOffset();
        }
    }

    private void HandleSnappingMode(bool keyDown)
    {
        // Keep the original cursor at the stored position
        UpdateOriginalCursorPosition();

        if (originRenderers.Count == 0 && storedSelectedItems.Count > 0)
        {
            ApplyOriginMaterials();
        }

        if (keyDown)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (TryFindTargetVertex(ray, out Vector3 targetVertex))
            {
                CreateSnappingCursor();
                UpdateSnappingCursor(targetVertex);
                CreateAllHolograms();
                UpdateHologramPositions(targetVertex);
            }
            else
            {
                DestroySnappingCursor();
                DestroyAllHolograms();
            }
        }
        else
        {
            DestroySnappingCursor();
            DestroyAllHolograms();
        }
    }

    private void UpdateOriginalCursorPosition()
    {
        if (cursor != null)
        {
            cursor.position = storedVertexPosition;
        }
        else
        {
            CreateCursor();
            cursor.position = storedVertexPosition;
        }
    }

    // Target and selection handling
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
            if (hit.transform == cursor?.transform || hit.transform == snappingCursor?.transform)
            {
                continue;
            }

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
            if (Target != null)
            {
                meshFilters = Target.GetComponentsInChildren<MeshFilter>();
                MessengerApi.Log($"[VERTEX] Target switched to: {currentTarget.name}");
            }
        }
    }

    private void ProcessItemSelection()
    {
        selectedItems.Clear();
        if (central?.selection.list.Count > 0)
        {
            selectedItems.AddRange(central.selection.list);
        }

        currentTarget = null;
        meshFilters = null;
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

    // Cursor and hologram management
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
        ConfigureTransparentMaterial(ren.material);
        cursor.GetComponent<Collider>().enabled = false;
    }

    private void CreateSnappingCursor()
    {
        if (snappingCursor != null)
        {
            return;
        }

        snappingCursor = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        snappingCursor.localScale = Vector3.one * selectionRadius.Value * 1.2f; // Slightly larger for distinction
        Renderer ren = snappingCursor.GetComponent<Renderer>();
        ren.material = CreateSnappingCursorMaterial();

        snappingCursor.GetComponent<Collider>().enabled = false;
    }

    private void UpdateSnappingCursor(Vector3 targetVertex)
    {
        if (snappingCursor != null)
        {
            snappingCursor.position = targetVertex;
        }
    }

    private void DestroySnappingCursor()
    {
        if (snappingCursor != null)
        {
            Destroy(snappingCursor.gameObject);
            snappingCursor = null;
        }
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
        GameObject hologram = new GameObject($"VertexSnapHologram_{item.name}");
        hologram.transform.position = item.transform.position;
        hologram.transform.rotation = item.transform.rotation;
        hologram.transform.localScale = item.transform.localScale;

        Renderer[] originalRenderers = item.GetComponentsInChildren<Renderer>();
        foreach (Renderer originalRenderer in originalRenderers)
        {
            CreateHologramRenderer(hologram, originalRenderer);
        }

        return hologram;
    }

    private void CreateHologramRenderer(GameObject hologram, Renderer originalRenderer)
    {
        GameObject hologramChild = new GameObject(originalRenderer.name + "_Hologram");
        hologramChild.transform.position = originalRenderer.transform.position;
        hologramChild.transform.rotation = originalRenderer.transform.rotation;
        hologramChild.transform.localScale = originalRenderer.transform.lossyScale;
        hologramChild.transform.SetParent(hologram.transform, true);

        MeshRenderer hologramRenderer = hologramChild.AddComponent<MeshRenderer>();
        MeshFilter hologramFilter = hologramChild.AddComponent<MeshFilter>();

        MeshFilter originalFilter = originalRenderer.GetComponent<MeshFilter>();
        if (originalFilter != null)
        {
            hologramFilter.sharedMesh = originalFilter.sharedMesh;
        }

        Material[] hologramMaterials = new Material[originalRenderer.materials.Length];
        for (int i = 0; i < hologramMaterials.Length; i++)
            hologramMaterials[i] = CreateHologramMaterial();

        hologramRenderer.materials = hologramMaterials;
    }

    private void UpdateHologramPositions(Vector3 targetVertex)
    {
        if (holograms.Count != storedSelectedItems.Count || storedPrimaryTarget == null)
        {
            return;
        }

        Vector3 movement = targetVertex - storedVertexPosition;

        for (int i = 0; i < holograms.Count; i++)
        {
            if (holograms[i] != null && i < storedRelativePositions.Count)
            {
                Vector3 newPosition = storedRelativePositions[i] + movement;
                holograms[i].transform.position = newPosition;
                holograms[i].transform.rotation = storedSelectedItems[i].transform.rotation;
            }
        }
    }

    private void CleanupAllCursorsAndHolograms()
    {
        if (cursor != null)
        {
            Destroy(cursor.gameObject);
            cursor = null;
        }

        DestroySnappingCursor();
        DestroyAllHolograms();
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

    // Material management
    private Material CreateHologramMaterial()
    {
        Material hologramMat = new Material(Shader.Find("Standard"));
        ConfigureTransparentMaterial(hologramMat);

        Color configColor = ParseHexColor(hologramColor.Value);
        configColor.a = (MIN_ALPHA + MAX_ALPHA) * 0.5f;
        hologramMat.color = configColor;

        hologramMat.EnableKeyword("_EMISSION");
        hologramMat.SetColor("_EmissionColor", configColor * 0.7f);

        return hologramMat;
    }

    private Material CreateSnappingCursorMaterial()
    {
        Material cursorMat = new Material(Shader.Find("Standard"));
        ConfigureTransparentMaterial(cursorMat);

        // Use a distinct color for the snapping cursor - bright yellow/orange
        Color cursorColor = new Color(1f, 0.8f, 0f, 0.9f); // Bright yellow-orange
        cursorMat.color = cursorColor;

        cursorMat.EnableKeyword("_EMISSION");
        cursorMat.SetColor("_EmissionColor", cursorColor * 1.5f); // Bright emission

        return cursorMat;
    }

    private Material CreateOriginMaterial()
    {
        Material originMat = new Material(Shader.Find("Standard"));
        ConfigureTransparentMaterial(originMat);

        float initialAlpha = (MIN_ALPHA + MAX_ALPHA) * 0.5f;
        originMat.color = new Color(0f, 1f, 0f, initialAlpha);

        originMat.EnableKeyword("_EMISSION");
        originMat.SetColor("_EmissionColor", new Color(0f, 0.8f, 0f, initialAlpha * 0.7f));

        return originMat;
    }

    private void ConfigureTransparentMaterial(Material material)
    {
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.renderQueue = (int)RenderQueue.Transparent;

        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Glossiness", 0.1f);
    }

    private void ApplyOriginMaterials()
    {
        if (storedSelectedItems.Count == 0)
        {
            return;
        }

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

                originalMaterials[renderer] = renderer.materials;
                originRenderers.Add(renderer);

                Material[] originMaterials = new Material[renderer.materials.Length];
                for (int i = 0; i < originMaterials.Length; i++)
                    originMaterials[i] = CreateOriginMaterial();

                renderer.materials = originMaterials;
            }
        }
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

    private void UpdateAllPulseEffects()
    {
        if (holograms.Count > 0)
        {
            UpdateHologramPulse();
        }

        if (originRenderers.Count > 0)
        {
            UpdateOriginMaterialPulse();
        }

        // Update snapping cursor pulse
        if (snappingCursor != null)
        {
            UpdateSnappingCursorPulse();
        }
    }

    private void UpdateHologramPulse()
    {
        float alpha = Mathf.Lerp(MIN_ALPHA, MAX_ALPHA, (Mathf.Sin(pulseTime * PULSE_SPEED) + 1f) * 0.5f);
        UpdateRenderersAlpha(holograms.SelectMany(h => h?.GetComponentsInChildren<Renderer>() ?? new Renderer[0]), alpha, 0.5f);
    }

    private void UpdateOriginMaterialPulse()
    {
        float originAlpha = Mathf.Lerp(MIN_ALPHA, MAX_ALPHA, (Mathf.Sin(pulseTime * PULSE_SPEED + Mathf.PI) + 1f) * 0.5f);
        UpdateRenderersAlpha(originRenderers, originAlpha, 0.7f);
    }

    private void UpdateSnappingCursorPulse()
    {
        Renderer cursorRenderer = snappingCursor.GetComponent<Renderer>();
        if (cursorRenderer?.material != null)
        {
            // Fast pulse for the snapping cursor
            float fastAlpha = Mathf.Lerp(0.5f, 1f, (Mathf.Sin(pulseTime * PULSE_SPEED * 2f) + 1f) * 0.5f);
            Color baseColor = new Color(1f, 0.8f, 0f, fastAlpha);
            cursorRenderer.material.color = baseColor;

            if (cursorRenderer.material.IsKeywordEnabled("_EMISSION"))
            {
                cursorRenderer.material.SetColor("_EmissionColor", baseColor * 2f);
            }
        }
    }

    private void UpdateRenderersAlpha(IEnumerable<Renderer> renderers, float alpha, float emissionMultiplier)
    {
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            foreach (Material material in renderer.materials)
            {
                if (material == null)
                {
                    continue;
                }

                Color color = material.color;
                color.a = alpha;
                material.color = color;

                if (material.IsKeywordEnabled("_EMISSION"))
                {
                    Color emissionColor = material.GetColor("_EmissionColor");
                    emissionColor.a = alpha * emissionMultiplier;
                    material.SetColor("_EmissionColor", emissionColor);
                }
            }
        }
    }

    // Utility and calculation methods
    private void CleanupAll()
    {
        RestoreAllVisibility();
        DestroyAllHolograms();
        if (cursor != null)
        {
            Destroy(cursor.gameObject);
            cursor = null;
        }

        DestroySnappingCursor();
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
            if (hit.transform == cursor?.transform || hit.transform == snappingCursor?.transform)
            {
                continue;
            }

            if (currentTarget != null && (hit.transform == currentTarget.transform || hit.transform.IsChildOf(currentTarget.transform)))
            {
                if (hit.transform.CompareTag(BLOCK_TAG))
                {
                    worldPoint = hit.point;
                    return true;
                }
            }
        }

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

    private bool TryFindTargetVertex(Ray ray, out Vector3 targetVertex)
    {
        targetVertex = Vector3.zero;
        int amountOfHits = Physics.RaycastNonAlloc(ray, hits);
        int min = Math.Min(MAX_HITS, amountOfHits);

        for (int i = 0; i < min; i++)
        {
            RaycastHit hit = hits[i];
            Transform hitTransform = hit.transform.root ?? hit.transform;

            if (hitTransform == cursor?.transform || hitTransform == snappingCursor?.transform)
            {
                continue;
            }

            if (holograms.Any(h => h != null && (hitTransform == h.transform || hitTransform.IsChildOf(h.transform))))
            {
                continue;
            }

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

        PrepareUndoData();
        CreateUndoEntry();
        DestroyAllHolograms();
        MessengerApi.Log($"[VERTEX] {storedSelectedItems.Count} object(s) snapped successfully!");
    }

    private void PrepareUndoData()
    {
        before.Clear();
        beforeSelection.Clear();
        after.Clear();
        afterSelection.Clear();

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
    }

    private void CreateUndoEntry()
    {
        if (before.Count > 0)
        {
            Change_Collection changeCollection = central.undoRedo.ConvertBeforeAndAfterListToCollection(
                before, after, storedSelectedItems, beforeSelection, afterSelection);
            central.validation.BreakLock(changeCollection, "Gizmo6");
        }
    }

    private Color ParseHexColor(string hex)
    {
        if (hex.StartsWith("#"))
        {
            hex = hex.Substring(1);
        }

        if (hex.Length != 6)
        {
            Logger.LogWarning($"Invalid hex color format: #{hex}. Using default cyan.");
            return new Color(0f, 1f, 1f, 1f);
        }

        try
        {
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Failed to parse hex color #{hex}: {ex.Message}. Using default cyan.");
            return new Color(0f, 1f, 1f, 1f);
        }
    }

    private enum VertexMode
    {
        Inactive,
        Positioning,
        Snapping
    }
}