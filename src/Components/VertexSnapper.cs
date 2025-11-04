using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using VertexSnapper.Helper;
using VertexSnapper.Managers;
using VertexSnapper.States;
using ZeepSDK.LevelEditor;

namespace VertexSnapper.Components;

public class VertexSnapper : MonoBehaviour
{
    // 1) Constants
    private const float PULSE_SPEED = 1f;
    private const float MIN_ALPHA = 0.2f;
    private const float MAX_ALPHA = 0.8f;

    // 2) Static fields
    private static readonly Color ORIGIN_BASE_COLOR = new Color(1f, 0f, 1f, 1f);
    private static readonly List<string> Before = new List<string>();
    private static readonly List<string> BeforeSelection = new List<string>();
    private static readonly List<string> After = new List<string>();
    private static readonly List<string> AfterSelection = new List<string>();

    // 3) Instance fields
    private readonly Dictionary<Renderer, Material[]> _wireframeReplaced = new Dictionary<Renderer, Material[]>();

    private readonly Color hologramColor = new Color(0f, 1f, 1f, 0.25f);
    private Camera _camera;
    private float _pulseTime;
    private BlockProperties _storedPrimaryTarget;
    private Vector3 _storedVertexPosition;
    private Transform _targetVertexPoint;

    // 4) Properties
    public List<BlockProperties> PreviouslySelectedBlocks { get; } = [];
    public List<BlockProperties> SelectedBlocks => LevelEditorCentral.selection.list;
    public LEV_LevelEditorCentral LevelEditorCentral { get; private set; }
    public Transform FirstVertex { get; set; }
    public Transform SecondVertex { get; set; }
    public Camera MainCamera => _camera = _camera ? _camera : Camera.main;
    private ManualLogSource Logger => Plugin.Instance.Logger;
    public IVertexSnapperState<VertexSnapper> CurrentState { get; set; }
    public GameObject VertexsnapperHologram { get; set; }

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
        CurrentState?.Update();
        _pulseTime += Time.deltaTime;
    }

    private void OnDestroy()
    {
        CurrentState?.Exit();
        CurrentState = null;
    }

    // 6) Public API
    public List<MeshFilter> GetAllMeshFilters(List<BlockProperties> blockProperties)
    {
        return blockProperties.SelectMany(x => x.GetComponentsInChildren<MeshFilter>()).ToList();
    }

    public void CreateAndMoveFirstCursorToClosestVertex()
    {
        if (PreviouslySelectedBlocks.Count == 0)
        {
            SafeDestroy(FirstVertex);
            return;
        }

        if (!FirstVertex)
        {
            FirstVertex = CreateVertexSphere().transform;
        }

        SpawnSphereAtVertex(FirstVertex, PreviouslySelectedBlocks);
    }

    public void CreateAndMoveSecondCursorToClosestVertex()
    {
        if (!SecondVertex)
        {
            SecondVertex = CreateVertexSphere().transform;
        }


        if (Physics.Raycast(MainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
        {
            BlockProperties block = hit.collider.GetComponentInParent<BlockProperties>();
            if (block)
            {
                SecondVertex.position = GetClosestWorldVertexInMeshes(GetAllMeshFilters([block]), hit.point);
            }
        }
    }

    public void SpawnSphereAtVertex(Transform targetTransform, List<BlockProperties> blockProperties)
    {
        if (Physics.Raycast(MainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
        {
            targetTransform.position = GetClosestWorldVertexInMeshes(GetAllMeshFilters(blockProperties), hit.point);
        }
    }

    public void ChangeState(IVertexSnapperState<VertexSnapper> newVertexSnapperState)
    {
        CurrentState?.Exit();
        CurrentState = newVertexSnapperState;
        CurrentState.VertexSnapper = this;
        CurrentState.Enter();
    }

    public void CacheAndRemoveBlockSelection()
    {
        PreviouslySelectedBlocks.Clear();
        PreviouslySelectedBlocks.CopyFrom(SelectedBlocks);
        // LevelEditorApi.ClearSelection();
        LevelEditorCentral.selection.list.Clear();
    }

    public void ReAddPreviousBlockSelection()
    {
        LevelEditorApi.ClearSelection();
        foreach (BlockProperties block in PreviouslySelectedBlocks)
        {
            LevelEditorApi.AddToSelection(block);
        }

        PreviouslySelectedBlocks.Clear();
    }

    public void CloneSelectedBlocks()
    {
        VertexsnapperHologram = new GameObject("VertexSnapper_Hologram");
        foreach (BlockProperties block in PreviouslySelectedBlocks)
        {
            if (block == null)
            {
                continue;
            }

            // Create a container per original block for hierarchy clarity
            GameObject blockCloneRoot = new GameObject(block.name + "_Hologram")
            {
                transform =
                {
                    position = block.transform.position,
                    rotation = block.transform.rotation,
                    localScale = block.transform.lossyScale
                }
            };
            blockCloneRoot.transform.SetParent(VertexsnapperHologram.transform, true);

            // Copy all renderers as dummy Mesh+Renderer only
            Renderer[] srcRenderers = block.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer srcRenderer in srcRenderers)
            {
                if (srcRenderer == null)
                {
                    continue;
                }

                MeshFilter srcMf = srcRenderer.GetComponent<MeshFilter>();
                SkinnedMeshRenderer srcSkinned = srcRenderer as SkinnedMeshRenderer;

                // Create dummy node
                GameObject go = new GameObject(srcRenderer.gameObject.name + "_Dummy");
                go.transform.SetPositionAndRotation(srcRenderer.transform.position, srcRenderer.transform.rotation);
                go.transform.localScale = srcRenderer.transform.lossyScale;
                go.transform.SetParent(blockCloneRoot.transform, true);

                // Add appropriate renderer + mesh holder without behavior
                if (srcSkinned && srcSkinned.sharedMesh)
                {
                    MeshFilter mf = go.AddComponent<MeshFilter>();
                    mf.sharedMesh = srcSkinned.sharedMesh;

                    MeshRenderer mr = go.AddComponent<MeshRenderer>();
                    Material[] srcMats = srcRenderer.sharedMaterials;
                    Material[] mats = new Material[Mathf.Max(1, srcMats?.Length ?? 1)];
                    for (int i = 0; i < mats.Length; i++)
                    {
                        mats[i] = WireframeBundleLoader.WireframeMaterial;
                    }

                    mr.sharedMaterials = mats;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                    mr.lightProbeUsage = LightProbeUsage.Off;
                    mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    continue;
                }

                if (srcMf && srcMf.sharedMesh)
                {
                    MeshFilter mf = go.AddComponent<MeshFilter>();
                    mf.sharedMesh = srcMf.sharedMesh;

                    MeshRenderer mr = go.AddComponent<MeshRenderer>();
                    Material[] srcMats = srcRenderer.sharedMaterials;
                    Material[] mats = new Material[Mathf.Max(1, srcMats?.Length ?? 1)];
                    for (int i = 0; i < mats.Length; i++)
                    {
                        mats[i] = WireframeBundleLoader.WireframeMaterial;
                    }

                    mr.sharedMaterials = mats;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                    mr.lightProbeUsage = LightProbeUsage.Off;
                    mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                }
            }
        }
    }

    public Material TransparentHologramMaterial(Color color, double intensity = 0)
    {
        // Create a transparent cyan hologram-style material
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                        ?? Shader.Find("HDRP/Lit")
                        ?? Shader.Find("Standard");
        Material mat = new Material(shader);

        // Base color: cyan with transparency
        Color baseCyan = color;

        // Configure Standard shader for transparency
        mat.SetFloat("_Mode", 3); // Transparent
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.color = baseCyan;
        mat.SetFloat("_Glossiness", 0.1f);
        mat.SetFloat("_Metallic", 0f);

        mat.SetColor("_BaseColor", baseCyan);

        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * (float)intensity);


        // Optional: slightly fresnel-like by reducing spec/metal
        return mat;
    }


    public void SafeDestroy(GameObject go)
    {
        if (go)
        {
            Destroy(go);
        }
    }

    public void SafeDestroy(Transform t)
    {
        if (t)
        {
            SafeDestroy(t.gameObject);
        }
    }

    public void ApplyWireframeMaterial()
    {
        _wireframeReplaced.Clear();

        foreach (BlockProperties block in PreviouslySelectedBlocks.Where(b => b))
        {
            foreach (Renderer r in block.GetComponentsInChildren<Renderer>(true).Where(r => r))
            {
                if (_wireframeReplaced.ContainsKey(r))
                {
                    continue;
                }

                _wireframeReplaced[r] = r.sharedMaterials;

                int slots = Math.Max(1, r.sharedMaterials?.Length ?? 1);
                Material[] mats = new Material[slots];
                for (int i = 0; i < slots; i++)
                {
                    mats[i] = WireframeBundleLoader.WireframeMaterial;
                }

                r.sharedMaterials = mats;
            }
        }
    }

    // Generic: apply a given material to all renderers of provided blocks.
    // Optionally preserve slot count; otherwise use a single-slot array.
    public void ApplyMaterialToBlocks(IEnumerable<BlockProperties> blocks, Material material, bool preserveSlotCount = true)
    {
        if (blocks == null || material == null)
        {
            return;
        }

        foreach (BlockProperties block in blocks.Where(b => b))
        {
            foreach (Renderer r in block.GetComponentsInChildren<Renderer>(true).Where(r => r))
            {
                // Backup originals once per renderer if not backed up yet
                if (!_wireframeReplaced.ContainsKey(r))
                {
                    _wireframeReplaced[r] = r.sharedMaterials;
                }

                int slots = preserveSlotCount ? Math.Max(1, r.sharedMaterials?.Length ?? 1) : 1;
                Material[] mats = new Material[slots];
                for (int i = 0; i < slots; i++)
                {
                    mats[i] = material;
                }

                r.sharedMaterials = mats;
            }
        }
    }

    public void RemoveAllWireframeMaterial()
    {
        if (_wireframeReplaced.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<Renderer, Material[]> kv in _wireframeReplaced.ToList())
        {
            Renderer r = kv.Key;
            if (!r)
            {
                continue;
            }

            r.sharedMaterials = kv.Value;
        }

        _wireframeReplaced.Clear();
    }


    public void MoveHologramToVertex(GameObject hologram)
    {
        // WIr wollen den Richtvektor herausfinden. Wir rechnen TargetVektor minus OriginVektor und dann tun wir jeden block um diesen Vektor verschieben.
        Plugin.Instance.Logger.LogInfo("Trying to MoveHologramToVertex");
        if (!FirstVertex || !SecondVertex)
        {
            return;
        }

        try
        {
            Plugin.Instance.Logger.LogInfo("MoveHologramToVertex");
            Vector3 directionVector = SecondVertex.position - FirstVertex.position;

            Vector3 newPosition = hologram.transform.position + directionVector;
            hologram.transform.position = newPosition;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public bool PerformSnap()
    {
        // WIr wollen den Richtvektor herausfinden. Wir rechnen TargetVektor minus OriginVektor und dann tun wir jeden block um diesen Vektor verschieben.
        if (!FirstVertex || !SecondVertex)
        {
            return false;
        }

        try
        {
            // Prepare undo (Before/After) snapshots like in the commented example
            Before.Clear();
            BeforeSelection.Clear();
            After.Clear();
            AfterSelection.Clear();

            // Take BEFORE snapshots
            foreach (BlockProperties block in PreviouslySelectedBlocks.Where(b => b))
            {
                Before.Add(block.ConvertBlockToJSON_v15_string(true));
                BeforeSelection.Add(block.UID);
            }

            // Apply movement
            Vector3 directionVector = SecondVertex.position - FirstVertex.position;
            foreach (BlockProperties block in PreviouslySelectedBlocks.Where(b => b))
            {
                Vector3 oldPosition = block.transform.position;
                Vector3 newPosition = oldPosition + directionVector;
                Logger.LogInfo($"Moving block {block.name} from {oldPosition} to {newPosition}");
                block.transform.position = newPosition;
            }

            // Take AFTER snapshots
            foreach (BlockProperties block in PreviouslySelectedBlocks.Where(b => b))
            {
                After.Add(block.ConvertBlockToJSON_v15_string(true));
                AfterSelection.Add(block.UID);
            }

            // Register undo step if we actually changed something
            if (Before.Count > 0)
            {
                LevelEditorCentral.validation.BreakLock(
                    LevelEditorCentral.undoRedo.ConvertBeforeAndAfterListToCollection(
                        Before, After, PreviouslySelectedBlocks, BeforeSelection, AfterSelection
                    ),
                    "Gizmo6"
                );
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    public void RestoreDefaultState()
    {
        RemoveAllWireframeMaterial();
        SafeDestroy(FirstVertex);
        FirstVertex = null;
        TriangleHighlighter.Clear();
        ReAddPreviousBlockSelection();
    }

    public void UpdateHologramPulse()
    {
        float phase = _pulseTime * PULSE_SPEED + Mathf.PI;
        float a = Mathf.Lerp(MIN_ALPHA, MAX_ALPHA, (Mathf.Sin(phase) + 1f) * 0.5f);

        // Apply pulse to the hologram GO instead of the old _holograms list
        if (!VertexsnapperHologram)
        {
            return;
        }

        IEnumerable<Material> hologramMaterials = VertexsnapperHologram
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
        material.SetFloat("_FillAlpha", a);
    }

    private Vector3 GetClosestWorldVertexInMeshes(IEnumerable<MeshFilter> targetMeshes, Vector3 target)
    {
        float minDist = float.MaxValue;
        Vector3 closest = Vector3.zero;

        foreach (MeshFilter meshFilter in targetMeshes)
        {
            if (!meshFilter || !meshFilter.sharedMesh || meshFilter.sharedMesh.vertexCount == 0)
            {
                continue;
            }

            Transform t = meshFilter.transform;
            Vector3[] verts = meshFilter.sharedMesh.vertices;
            foreach (Vector3 v in verts)
            {
                Vector3 world = t.TransformPoint(v);
                float dist = (target - world).sqrMagnitude;
                if (dist > minDist)
                {
                    continue;
                }

                minDist = dist;
                closest = world;
            }
        }

        return closest;
    }

    private GameObject CreateVertexSphere()
    {
        GameObject sphereGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sphereGameObject.transform.localScale = Vector3.one * (float)VertexSnapperConfigManager.VertexSnapperSphereRadius.Value;

        Renderer renderer = sphereGameObject.GetComponent<Renderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sharedMaterial = TransparentHologramMaterial(ORIGIN_BASE_COLOR);

        return sphereGameObject;
    }

    private Color HexToColor(string hex)
    {
        if (hex.StartsWith("#"))
        {
            hex = hex.Substring(1);
        }

        if (hex.Length != 6 && hex.Length != 8)
        {
            return Color.white;
        }

        try
        {
            byte r = Convert.ToByte(hex[..2], 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);

            byte a = hex.Length == 8 ? Convert.ToByte(hex.Substring(6, 2), 16) : (byte)255;
            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }
        catch (Exception)
        {
            return Color.white;
        }
    }
}