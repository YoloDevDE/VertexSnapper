using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using FMODSyntax;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using VertexSnapper.Helper;
using VertexSnapper.States;
using ZeepSDK.LevelEditor;
using ZeepSDK.Messaging;

namespace VertexSnapper;

public class VertexSnapper : MonoBehaviour
{
    // 1) Constants
    private const float PULSE_SPEED = 6f;
    private const float MIN_ALPHA = 0.9f;
    private const float MAX_ALPHA = 0.1f;

    // 2) Static fields
    private static readonly List<string> Before = [];
    private static readonly List<string> BeforeSelection = [];
    private static readonly List<string> After = [];
    private static readonly List<string> AfterSelection = [];
    private static readonly int FillAlpha = Shader.PropertyToID("_FillAlpha");

    private Camera _camera;

    private bool _isStateChanging;
    private float _pulseTime;

    // 3) Instance fields
    public Dictionary<Renderer, Material[]> BlockSelectionMaterials { get; } = new Dictionary<Renderer, Material[]>();
    public Dictionary<Renderer, Material[]> OriginalBlockMaterials { get; } = new Dictionary<Renderer, Material[]>();

    public Dictionary<Renderer, Material[]> TargetBlockMaterials { get; } = new Dictionary<Renderer, Material[]>();

    public Dictionary<Transform, Vector3> HologramOffsets { get; } = new Dictionary<Transform, Vector3>();

    // 4) Properties
    public List<BlockProperties> BlockSelectionCache { get; } = [];
    public List<BlockProperties> SelectedBlocks => LevelEditorCentral.selection.list;
    public LEV_LevelEditorCentral LevelEditorCentral { get; private set; }

    public GameObject FirstCursor { get; set; }

    public GameObject SecondCursor { get; set; }


    public Camera MainCamera => _camera = _camera ? _camera : Camera.main;
    private ManualLogSource Logger => Plugin.Instance.Logger;
    public IVertexSnapperState<VertexSnapper> CurrentState { get; set; }
    public GameObject Hologram { get; set; }

    public Vector3 CubeSize { get; set; }
    public float CubeScaleFactor { get; set; } = 0.5f;
    public bool IsInEditingMode { get; set; }

    // 5) Unity lifecycle
    private void Awake()
    {
        string bundlePath = Path.Combine(Path.GetDirectoryName(Plugin.Instance.Info.Location) ?? ".", "assets", "wireframe");
        WireframeBundleLoader.InitWireframeMaterial(bundlePath);
        LevelEditorCentral = FindObjectOfType<LEV_LevelEditorCentral>();
    }

    private void Start()
    {
        ChangeState(new StateIdle());
    }

    private void Update()
    {
        _pulseTime += Time.deltaTime;
        if (IsInEditingMode)
        {
            if (LevelEditorCentral.tool.currentTool != 0 && CurrentState is not StateIdle)
            {
                IsInEditingMode = LevelEditorCentral.tool.currentTool == 0;
                BlockSelectionCache.Clear();
                ChangeState(new StateAbort());
                RestoreOriginalMaterials(OriginalBlockMaterials);
            }
        }
        else
        {
            IsInEditingMode = LevelEditorCentral.tool.currentTool == 0;
            return;
        }


        if (_isStateChanging)
        {
            return;
        }

        AnimateHologramPulse();

        CurrentState?.Update();
    }

    private void OnDestroy()
    {
        CurrentState?.Exit();
        CurrentState = null;
    }


    public void SnapCursorToVertex(Transform targetVertex, List<BlockProperties> filter)
    {
        if (RaycastUtils.IsSphereCastOnBlockSuccessful(MainCamera, out RaycastHit hit, filter))
        {
            targetVertex.position = FindClosestVertexToHit(hit);
        }
    }

    public void ChangeState(IVertexSnapperState<VertexSnapper> newVertexSnapperState)
    {
        _isStateChanging = true;
        CurrentState?.Exit();
        CurrentState = newVertexSnapperState;
        CurrentState.VertexSnapper = this;
        CurrentState.Enter();
        _isStateChanging = false;
    }

    public void CacheAndRemoveBlockSelection()
    {
        BlockSelectionCache.Clear();
        BlockSelectionCache.CopyFrom(SelectedBlocks);
        // LevelEditorCentral.selection.list.Clear();
        CacheOriginalMaterials(BlockSelectionCache, BlockSelectionMaterials);
        LevelEditorApi.ClearSelection();
        int validHistoryPosition = Math.Max(0, Math.Min(LevelEditorCentral.undoRedo.currentHistoryPosition - 1, LevelEditorCentral.undoRedo.historyList.Count - 1));
        LevelEditorCentral.undoRedo.historyList.RemoveAt(validHistoryPosition);
        LevelEditorCentral.undoRedo.currentHistoryPosition = validHistoryPosition;
        CacheOriginalMaterials(BlockSelectionCache, OriginalBlockMaterials);
    }


    public void ReAddPreviousBlockSelection()
    {
        if (!IsInEditingMode)
        {
            return;
        }

        LevelEditorApi.ClearSelection();
        foreach (BlockProperties block in BlockSelectionCache)
        {
            LevelEditorApi.AddToSelection(block);
        }

        BlockSelectionCache.Clear();
    }

    public GameObject CreateHologram(IEnumerable<GameObject> originals, Material holoMaterial, Color color = default)
    {
        GameObject root = new GameObject("MergedHologram")
        {
            layer = LayerMask.NameToLayer("Ignore Raycast")
        };

        foreach (GameObject obj in originals.Where(o => o))
        {
            GameObject clone = CreateHologram(obj, holoMaterial, color);
            clone.layer = 2;
            // Wichtig: Position nicht relativ resetten
            clone.transform.SetParent(root.transform, true);
            int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");

            foreach (Transform t in clone.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = ignoreLayer;

                if (t.TryGetComponent(out Collider col))
                {
                    col.enabled = false;
                }
            }
        }

        return root;
    }

    private static GameObject CreateHologram(GameObject original, Material holoMaterial, Color color = default)
    {
        GameObject clone = Instantiate(original);
        clone.name = original.name + "_Hologram";
        clone.layer = LayerMask.NameToLayer("Ignore Raycast");
        // foreach (Component comp in clone.GetComponentsInChildren<Component>(true).Where(c => c))
        // {
        //     if (comp is Transform or Renderer or MeshFilter)
        //     {
        //         continue;
        //     }
        //
        //     DestroyImmediate(comp);
        // }

        foreach (MeshRenderer renderer in clone.GetComponentsInChildren<MeshRenderer>().Where(r => r))
        {
            int count = renderer.sharedMaterials.Length;
            Material[] mats = new Material[count];

            for (int i = 0; i < count; i++)
            {
                mats[i] = new Material(holoMaterial);
                if (color != default)
                {
                    mats[i].SetColor("_BaseColor", color);
                }
            }

            renderer.sharedMaterials = mats;

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        return clone;
    }


    public void CreateAnchorPoint(GameObject hologram, Dictionary<Transform, Vector3> offsets, Transform anchorPoint)
    {
        if (offsets == null)
        {
            return;
        }

        offsets.Clear();
        foreach (Transform t in hologram.GetComponentsInChildren<Transform>())
        {
            offsets[t] = t.position - anchorPoint.transform.position;
        }
    }


    public void SafeDestroy(GameObject go)
    {
        // if (!go)
        // {
        //     return;
        // }
        //
        // go.transform.SetParent(null);
        // go.SetActive(false);
        Destroy(go);
    }

    public void SafeDestroy(Transform t)
    {
        if (t)
        {
            SafeDestroy(t.gameObject);
        }
    }


    public void RestoreOriginalMaterials(Dictionary<Renderer, Material[]> originalMaterialsCache)
    {
        if (originalMaterialsCache == null || originalMaterialsCache.Count == 0)
        {
            return;
        }


        foreach ((Renderer renderer, Material[] material) in originalMaterialsCache.ToList())
        {
            if (!renderer)
            {
                continue;
            }

            renderer.sharedMaterials = material;
        }

        originalMaterialsCache.Clear();
    }

    public void SetMaterialForBlocks([NotNull] IEnumerable<BlockProperties> blocks, [NotNull] Material material)
    {
        foreach (BlockProperties currentBlock in blocks.Where(b => b))
        {
            foreach (Renderer currentRenderer in currentBlock.GetComponentsInChildren<Renderer>().Where(r => r))
            {
                int materialSlots = Math.Max(1, currentRenderer.sharedMaterials?.Length ?? 1);
                Material[] sharedMaterials = new Material[materialSlots];
                for (int i = 0; i < materialSlots; i++)
                {
                    sharedMaterials[i] = material;
                }

                currentRenderer.sharedMaterials = sharedMaterials;
            }
        }
    }

    public void MoveHologramToCursor(Vector3 cursorPos)
    {
        if (!Hologram && HologramOffsets.Count == 0)
        {
            return;
        }

        foreach ((Transform savedTransform, Vector3 offset) in HologramOffsets)
        {
            savedTransform.position = cursorPos + offset;
        }
    }

    public bool PerformSnap()
    {
        if (!FirstCursor || !SecondCursor)
        {
            AudioEvents.Blarghl.Play();
            return false;
        }

        Before.Clear();
        BeforeSelection.Clear();
        After.Clear();
        AfterSelection.Clear();

        // Take BEFORE snapshots
        foreach (BlockProperties block in BlockSelectionCache.Where(b => b))
        {
            Before.Add(block.ConvertBlockToJSON_v15_string(true));
            BeforeSelection.Add(block.UID);
        }

        // Apply movement
        Vector3 directionVector = SecondCursor.transform.position - FirstCursor.transform.position;
        foreach (BlockProperties block in BlockSelectionCache.Where(b => b))
        {
            Vector3 oldPosition = block.transform.position;
            Vector3 newPosition = oldPosition + directionVector;
            block.transform.position = newPosition;
        }


        // Take AFTER snapshots
        foreach (BlockProperties block in BlockSelectionCache.Where(b => b))
        {
            After.Add(block.ConvertBlockToJSON_v15_string(true));
            AfterSelection.Add(block.UID);
        }

        // Register undo step if we actually changed something
        if (Before.Count > 0)
        {
            LevelEditorCentral.validation.BreakLock(
                LevelEditorCentral.undoRedo.ConvertBeforeAndAfterListToCollection(
                    Before,
                    After,
                    BlockSelectionCache,
                    BeforeSelection,
                    AfterSelection),
                "Gizmo6");
        }

        MessengerApi.LogSuccess("[Vertexsnapper] Snap successful!", 0.8f);
        AudioEvents.BlockPlace.Play();
        AudioEvents.ChangeWheelsGate.Play();
        return true;
    }

    public void RestoreDefaultState()
    {
        RestoreOriginalMaterials(BlockSelectionMaterials);
        RestoreOriginalMaterials(TargetBlockMaterials);
        SafeDestroy(FirstCursor);
        SafeDestroy(SecondCursor);
        SafeDestroy(Hologram);
        ReAddPreviousBlockSelection();
    }

    public void AnimateHologramPulse()
    {
        float phase = _pulseTime * PULSE_SPEED + Mathf.PI;
        float a = Mathf.Lerp(MIN_ALPHA, MAX_ALPHA, (Mathf.Sin(phase) + 1f) * 0.5f);

        // Apply pulse to the hologram GO instead of the old _holograms list
        if (!Hologram)
        {
            return;
        }

        IEnumerable<Material> hologramMaterials = Hologram
                                                  .GetComponentsInChildren<Renderer>(true)
                                                  .Where(r => r)
                                                  .Select(r => r.materials)
                                                  .SelectMany(m => m)
                                                  .Where(m => m != null);

        foreach (Material material in hologramMaterials)
        {
            SetMaterialOpacity(material, a);
        }
    }

    // 7) Private helpers
    private void SetMaterialOpacity(Material material, float a)
    {
        material.SetFloat(FillAlpha, a);
    }


    public Vector3 FindClosestVertexToHit(RaycastHit hit)
    {
        float shortestDistance = float.MaxValue;
        Vector3 closestVertexInMesh = Vector3.zero;
        MeshFilter[] selectedMeshFilters = hit.collider.gameObject.GetComponentInParent<BlockProperties>().GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter meshFilter in selectedMeshFilters.Where(m => m))
        {
            foreach (Vector3 vertexInMesh in meshFilter.sharedMesh.vertices)
            {
                Vector3 vertexWorldPos = meshFilter.transform.TransformPoint(vertexInMesh);
                float currentDistance = (hit.point - vertexWorldPos).sqrMagnitude;
                if (currentDistance > shortestDistance)
                {
                    continue;
                }

                shortestDistance = currentDistance;
                closestVertexInMesh = vertexWorldPos;
            }
        }

        return closestVertexInMesh;
    }

    public void CacheOriginalMaterials(List<BlockProperties> blocks, Dictionary<Renderer, Material[]> originalMaterialsCache)
    {
        originalMaterialsCache.Clear();

        foreach (BlockProperties block in blocks.Where(b => b))
        {
            foreach (Renderer r in block.GetComponentsInChildren<Renderer>(true).Where(r => r))
            {
                if (originalMaterialsCache.ContainsKey(r))
                {
                    continue;
                }

                originalMaterialsCache[r] = r.sharedMaterials;
            }
        }
    }

    public void ApplyWireframeMaterial(List<BlockProperties> blocks, Color color = default)
    {
        foreach (BlockProperties block in blocks.Where(b => b))
        {
            foreach (Renderer r in block.GetComponentsInChildren<Renderer>(true).Where(r => r))
            {
                int slots = Math.Max(1, r.sharedMaterials?.Length ?? 1);
                Material[] mats = new Material[slots];
                for (int i = 0; i < slots; i++)
                {
                    mats[i] = new Material(WireframeBundleLoader.WireframeMaterial);
                    if (color != default)
                    {
                        mats[i].SetColor("_BaseColor", color);
                    }
                }

                r.sharedMaterials = mats;
            }
        }
    }
}