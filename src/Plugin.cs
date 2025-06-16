using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.Rendering;
using ZeepSDK.LevelEditor;

namespace VertexSnapper;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const int MAX_HITS = 16;
    private const string BLOCK_TAG = "BuildingBlock";

    private static readonly RaycastHit[] hits = new RaycastHit[MAX_HITS];

    private static readonly List<string> before = new List<string> { string.Empty };
    private static readonly List<string> beforeSelection = new List<string> { string.Empty };
    private static readonly List<string> after = new List<string> { string.Empty };
    private static readonly List<string> afterSelection = new List<string> { string.Empty };
    private Camera cam;

    private LEV_LevelEditorCentral central;

    private Transform cursor;
    private bool isDragging;

    private bool isInEditor;
    private ConfigEntry<KeyCode> key;
    private ConfigEntry<float> maxDistance;
    private MeshFilter[] meshFilters;
    private BlockProperties selectedItem;

    private ConfigEntry<float> selectionRadius;
    private Vector3 vertOffset;

    // New fields for transparency management
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private Dictionary<Renderer, Material[]> transparentMaterials = new Dictionary<Renderer, Material[]>();
    private HashSet<Renderer> outOfRangeRenderers = new HashSet<Renderer>();
    private float lastTransparencyUpdateTime;
    private const float TRANSPARENCY_UPDATE_INTERVAL = 0.1f; // Update every 100ms for performance

    private Transform Target => selectedItem == null ? null : selectedItem.transform;

    private void Awake()
    {
        LevelEditorApi.EnteredLevelEditor += EnteredLevelEditor;
        LevelEditorApi.ExitedLevelEditor += ExitedLevelEditor;
        // TODO: Have one for exiting level editor

        // Plugin startup logic
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

        // Update transparency for out-of-range objects periodically
        if (Time.time - lastTransparencyUpdateTime > TRANSPARENCY_UPDATE_INTERVAL)
        {
            UpdateObjectTransparency();
            lastTransparencyUpdateTime = Time.time;
        }

        bool keyDown = Input.GetKey(key.Value);

        if (central != null)
        {
            if (keyDown)
            {
                LevelEditorApi.BlockMouseInput(this);
            }
            else
            {
                LevelEditorApi.UnblockMouseInput(this);
            }
        }

        if (!keyDown)
        {
            DestroyCursor();
            return;
        }

        if (central == null)
        {
            return;
        }

        if (central.selection.list.Count != 1)
        {
            return;
        }

        if (Target == null)
        {
            return;
        }

        CreateCursor();

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (isDragging || ShouldHandleDragging(ray))
        {
            HandleDragging(ray);
            return;
        }

        if (!TryGetWorldPoint(ray, out Vector3 worldPoint))
        {
            return;
        }

        cursor.position = GetClosestVert(meshFilters, worldPoint);
        CalculateVertOffset();
    }

    private void OnDestroy()
    {
        LevelEditorApi.EnteredLevelEditor -= EnteredLevelEditor;
        LevelEditorApi.ExitedLevelEditor -= ExitedLevelEditor;
        
        // Clean up materials
        RestoreAllMaterials();
    }

    private void EnteredLevelEditor()
    {
        TryFindCentral();
        isInEditor = true;
    }

    private void ExitedLevelEditor()
    {
        isInEditor = false;
        RestoreAllMaterials();
    }

    private void UpdateObjectTransparency()
    {
        if (cam == null) return;

        // Find all building blocks in the scene
        GameObject[] buildingBlocks = GameObject.FindGameObjectsWithTag(BLOCK_TAG);
        
        foreach (GameObject block in buildingBlocks)
        {
            if (block == null) continue;

            // Skip the selected item
            if (Target != null && (block.transform == Target || block.transform.IsChildOf(Target))) 
                continue;

            float distance = Vector3.Distance(cam.transform.position, block.transform.position);
            Renderer[] renderers = block.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;

                bool shouldBeTransparent = distance > maxDistance.Value;
                bool currentlyTransparent = outOfRangeRenderers.Contains(renderer);

                if (shouldBeTransparent && !currentlyTransparent)
                {
                    MakeRendererTransparent(renderer);
                }
                else if (!shouldBeTransparent && currentlyTransparent)
                {
                    RestoreRendererMaterial(renderer);
                }
            }
        }
    }

    private void MakeRendererTransparent(Renderer renderer)
    {
        if (outOfRangeRenderers.Contains(renderer)) return;

        // Store original materials
        if (!originalMaterials.ContainsKey(renderer))
        {
            originalMaterials[renderer] = renderer.materials;
        }

        // Create transparent versions
        Material[] transparentMats = new Material[renderer.materials.Length];
        for (int i = 0;i < renderer.materials.Length; i++)
        {
            Material originalMat = renderer.materials[i];
            Material transparentMat = new Material(originalMat);
            
            // Make material transparent
            SetMaterialToTransparent(transparentMat);
            
            // Set alpha to 0.5 (half transparent)
            Color color = transparentMat.color;
            color.a = 0.5f;
            transparentMat.color = color;
            
            transparentMats[i] = transparentMat;
        }

        transparentMaterials[renderer] = transparentMats;
        renderer.materials = transparentMats;
        outOfRangeRenderers.Add(renderer);
    }

    private void RestoreRendererMaterial(Renderer renderer)
    {
        if (!outOfRangeRenderers.Contains(renderer)) return;

        if (originalMaterials.ContainsKey(renderer))
        {
            renderer.materials = originalMaterials[renderer];
        }

        // Clean up transparent materials
        if (transparentMaterials.ContainsKey(renderer))
        {
            foreach (Material mat in transparentMaterials[renderer])
            {
                if (mat != null) Destroy(mat);
            }
            transparentMaterials.Remove(renderer);
        }

        outOfRangeRenderers.Remove(renderer);
    }

    private void RestoreAllMaterials()
    {
        foreach (Renderer renderer in new HashSet<Renderer>(outOfRangeRenderers))
        {
            RestoreRendererMaterial(renderer);
        }

        // Clean up remaining materials
        foreach (var kvp in transparentMaterials)
        {
            foreach (Material mat in kvp.Value)
            {
                if (mat != null) Destroy(mat);
            }
        }

        originalMaterials.Clear();
        transparentMaterials.Clear();
        outOfRangeRenderers.Clear();
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

        void ToFadeMode(Material material)
        {
            SetMaterialToTransparent(material);
        }

        cursor = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        cursor.localScale = Vector3.one * selectionRadius.Value;
        Renderer ren = cursor.GetComponent<Renderer>();
        ren.material = new Material(Shader.Find("Standard"));
        ren.material.color = new Color(1, 1, 1, 0.75f);
        ToFadeMode(ren.material);
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

    private bool ShouldHandleDragging(Ray ray)
    {
        if (Target == null)
        {
            return false;
        }

        if (!Input.GetMouseButton(0))
        {
            return false;
        }

        Vector3 normal = (cam.transform.position - Target.transform.position).normalized;
        Plane plane = new Plane(normal, cursor.position);
        if (!plane.Raycast(ray, out float enter))
        {
            return false;
        }

        Vector3 pointOnPlane = ray.GetPoint(enter);
        float distance = Vector3.Distance(pointOnPlane, cursor.position);
        return distance <= selectionRadius.Value;
    }

    private void HandleDragging(Ray ray)
    {
        isDragging = true;

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

            if (hitTransform == cursor.transform)
            {
                continue;
            }

            if (hitTransform == Target || hitTransform.IsChildOf(Target))
            {
                continue;
            }

            if (!hitTransform.CompareTag(BLOCK_TAG))
            {
                continue;
            }

            MeshFilter[] hitMeshFilters = hitTransform.GetComponentsInChildren<MeshFilter>();
            Vector3 closestVert = GetClosestVert(hitMeshFilters, hit.point);

            Vector3 targetPosition = closestVert + vertOffset;

            float distance = Vector3.Distance(targetPosition, Target.position);
            if (distance < 0.01f)
            {
                continue;
            }

            List<BlockProperties> selection = new List<BlockProperties> { selectedItem };

            before[0] = selectedItem.ConvertBlockToJSON_v15_string(true);
            beforeSelection[0] = selection[0].UID;

            Target.position = targetPosition;

            after[0] = selectedItem.ConvertBlockToJSON_v15_string(true);
            afterSelection[0] = selection[0].UID;

            Change_Collection changeCollection = central.undoRedo.ConvertBeforeAndAfterListToCollection(
                before,
                after,
                selection,
                beforeSelection,
                afterSelection);

            central.validation.BreakLock(changeCollection, "Gizmo6");
        }

        if (!Input.GetMouseButton(0))
        {
            isDragging = false;
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
}