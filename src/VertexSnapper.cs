using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Rendering;
using ZeepSDK.LevelEditor;
using ZeepSDK.Messaging;

namespace VertexSnapper;

public class VertexSnapper : MonoBehaviour
{
    // Constants
    private const float PULSE_SPEED = 4f;
    private const float MIN_ALPHA = 0.1f;
    private const float MAX_ALPHA = 0.9f;

    // Origin material color constants
    private static readonly Color ORIGIN_BASE_COLOR = new Color(0f, 1f, 0.5f, 1f);

    // Static readonly fields
    private static readonly List<string> Before = new List<string>();
    private static readonly List<string> BeforeSelection = new List<string>();
    private static readonly List<string> After = new List<string>();
    private static readonly List<string> AfterSelection = new List<string>();

    // Private readonly collections
    private readonly List<GameObject> _holograms = new List<GameObject>();
    private readonly Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();
    private readonly List<Renderer> _originRenderers = new List<Renderer>();
    private readonly List<Vector3> _storedRelativePositions = new List<Vector3>();
    private Camera _camera;
    private float _pulseTime;
    private BlockProperties _storedPrimaryTarget;
    private Vector3 _storedVertexPosition;
    private Transform _targetVertexPoint;

    public List<BlockProperties> PreviouslySelectedBlocks { get; } = [];

    // State variables
    public List<BlockProperties> SelectedBlocks => LevelEditorCentral.selection.list;

    // Unity components/references
    public LEV_LevelEditorCentral LevelEditorCentral { get; private set; }
    public Transform OriginVertexPoint { get; set; }


    // Properties
    private Camera MainCamera => _camera = _camera ? _camera : Camera.main;
    private ManualLogSource Logger => Plugin.Instance.Logger;
    public IVertexSnapperState<VertexSnapper> CurrentState { get; set; }

    private void Awake()
    {
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
        // UpdateHologramPulse();
    }

    private void OnDestroy()
    {
        CurrentState?.Exit();
        CurrentState = null;
    }

    public List<MeshFilter> GetAllMeshFilters(List<BlockProperties> blockProperties)
    {
        return blockProperties.SelectMany(x => x.GetComponentsInChildren<MeshFilter>()).ToList();
    }

    public void CreateAndMoveOriginCursorToClosestVertex()
    {
        if (PreviouslySelectedBlocks.Count == 0)
        {
            SafeDestroy(OriginVertexPoint);
            return;
        }

        if (!OriginVertexPoint)
        {
            OriginVertexPoint = CreateVertexSphere().transform;
        }


        if (Physics.Raycast(MainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
        {
            OriginVertexPoint.position = GetClosestWorldVertexInMeshes(GetAllMeshFilters(PreviouslySelectedBlocks), hit.point);
        }
    }


    public void ChangeState(IVertexSnapperState<VertexSnapper> newVertexSnapperState)
    {
        CurrentState?.Exit();
        CurrentState = newVertexSnapperState;
        CurrentState.VertexSnapper = this;
        CurrentState.Enter();
    }

    private Color MessengerText()
    {
        return HexToColor("#ffffff");
    }

    private Color MessengerBackground()
    {
        return HexToColor("#652c70");
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
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);

            byte a = hex.Length == 8 ? Convert.ToByte(hex.Substring(6, 2), 16) : (byte)255;
            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }
        catch (Exception ex)
        {
            return Color.white;
        }
    }


    private GameObject CreateVertexSphere()
    {
        GameObject sphereGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sphereGameObject.transform.localScale = Vector3.one * (float)VertexSnapperConfigManager.VertexSnapperSphereRadius.Value;

        Renderer renderer = sphereGameObject.GetComponent<Renderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        Material material = new Material(Shader.Find("Unlit/Color"))
        {
            color = new Color(0f, 1f, 0.5f, 1f)
        };
        renderer.sharedMaterial = material;

        material.renderQueue = 3000;


        return sphereGameObject;
    }

    public void CacheAndRemoveBlockSelection()
    {
        PreviouslySelectedBlocks.Clear();
        PreviouslySelectedBlocks.AddRange(SelectedBlocks);
        LevelEditorApi.ClearSelection();
    }

    public void ReAddPreviousBlockSelection()
    {
        foreach (BlockProperties block in PreviouslySelectedBlocks)
        {
            LevelEditorApi.AddToSelection(block);
        }
    }

    public void SafeDestroy(GameObject obj)
    {
        if (obj)
        {
            Destroy(obj.gameObject);
        }
    }

    public void SafeDestroy(Transform t)
    {
        if (t)
        {
            Destroy(t.gameObject);
        }
    }

    public void CreateAllHolograms()
    {
        foreach (BlockProperties block in PreviouslySelectedBlocks.Where(block => block))
        {
            _holograms.Add(CreateHologramForItem(block));
        }
    }

    private GameObject CreateHologramForItem(BlockProperties item)
    {
        GameObject hologramForItem = new GameObject("VertexSnapHologram_" + item.name)
        {
            transform =
            {
                position = item.transform.position,
                rotation = item.transform.rotation,
                localScale = item.transform.localScale
            }
        };
        List<Renderer> componentsInChildren = item.GetComponentsInChildren<Renderer>().ToList();
        foreach (Renderer componentsInChild in componentsInChildren)
        {
            GameObject hologramRenderer = new GameObject(componentsInChild.name + "_Hologram")
            {
                transform =
                {
                    position = componentsInChild.transform.position,
                    rotation = componentsInChild.transform.rotation,
                    localScale = componentsInChild.transform.lossyScale
                }
            };
            hologramRenderer.transform.SetParent(hologramForItem.transform, true);
            MeshRenderer meshRenderer = hologramRenderer.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = hologramRenderer.AddComponent<MeshFilter>();
            MeshFilter meshFilterComponent = componentsInChild.GetComponent<MeshFilter>();
            if (meshFilterComponent)
            {
                meshFilter.sharedMesh = meshFilterComponent.sharedMesh;
            }

            Material[] wireframeMaterials = new Material[componentsInChild.materials.Length];

            for (int index = 1; index < wireframeMaterials.Length; index++)
            {
                wireframeMaterials[index] = CreateWireframeMaterial();
            }

            meshRenderer.materials = wireframeMaterials;
        }

        return hologramForItem;
    }

    private void DestroyAllHolograms()
    {
        foreach (GameObject hologram in _holograms)
        {
            if (!hologram)
            {
                continue;
            }

            Destroy(hologram);
        }

        _holograms.Clear();
    }

    // private void UpdateHologramPositions(Vector3 targetVertex)
    // {
    //
    //     Vector3 vector3_1 = targetVertex - _storedVertexPosition;
    //     for (int index = 0; index < _holograms.Count; ++index)
    //     {
    //         if (_holograms[index] != null && index < _storedRelativePositions.Count)
    //         {
    //             Vector3 vector3_2 = _storedRelativePositions[index] + vector3_1;
    //             _holograms[index].transform.position = vector3_2;
    //             _holograms[index].transform.rotation = SelectedBlocks[index].transform.rotation;
    //         }
    //     }
    // }

    public void UpdateHologramPulse()
    {
        if (_holograms.Count == 0)
        {
            return;
        }

        float num = Mathf.Lerp(0.1f, 0.7f, (float)((Mathf.Sin(_pulseTime * 4f) + 1.0) * 0.5));
        foreach (GameObject hologram in _holograms)
        {
            if (hologram == null)
            {
                continue;
            }

            foreach (Renderer componentsInChild in hologram.GetComponentsInChildren<Renderer>())
            foreach (Material material in componentsInChild.materials)
            {
                if (material == null)
                {
                    continue;
                }

                Color color1 = material.color with { a = num };
                material.color = color1;
                if (material.IsKeywordEnabled("_EMISSION"))
                {
                    Color color2 = material.GetColor("_EmissionColor") with
                    {
                        a = num * 0.5f
                    };
                    material.SetColor("_EmissionColor", color2);
                }
            }
        }
    }

    private Material CreateWireframeMaterial()
    {
        Material material = new Material(Shader.Find("Standard"));
        ConfigureHologramMaterial(material);
        Color hexColor = HexToColor("#00ffff");
        float num = 0.4f;
        hexColor.a = num;
        material.color = hexColor;
        material.EnableKeyword("_EMISSION");
        Color color = hexColor with { a = num * 0.5f };
        material.SetColor("_EmissionColor", color);
        return material;
    }

    private Material CreateOriginMaterial()
    {
        float baseAlpha = Mathf.Clamp((MIN_ALPHA + MAX_ALPHA) * 0.5f, MIN_ALPHA, MAX_ALPHA);

        Material material = new Material(Shader.Find("Standard"));
        ConfigureHologramMaterial(material);

        // Apply base color with alpha from constants
        Color baseColor = ORIGIN_BASE_COLOR;
        baseColor.a = baseAlpha;
        material.color = baseColor;

        return material;
    }

    private void ConfigureHologramMaterial(Material material)
    {
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", 5);
        material.SetInt("_DstBlend", 10);
        material.SetInt("_ZWrite", 0);
        material.renderQueue = 3000;
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    }


    public void ApplyHologramMaterialForOrigin()
    {
        foreach (BlockProperties block in PreviouslySelectedBlocks)
        {
            if (!block)
            {
                continue;
            }


            foreach (Renderer renderer in block.GetComponentsInChildren<Renderer>())
            {
                if (!renderer)
                {
                    continue;
                }

                if (!_originalMaterials.ContainsKey(renderer))
                {
                    _originalMaterials[renderer] = renderer.materials;
                    _originRenderers.Add(renderer);
                }

                Material[] originals = _originalMaterials[renderer];
                Material[] withOverlay = new Material[originals.Length + 1];
                Array.Copy(originals, withOverlay, originals.Length);
                withOverlay[^1] = CreateOriginMaterial();

                renderer.materials = withOverlay;
            }
        }
    }

    public void RestoreDefaultState()
    {
        if (_holograms.Count > 0)
        {
            DestroyAllHolograms();
        }

        if (_originRenderers.Count > 0)
        {
            RestoreOriginMaterials();
        }

        _storedVertexPosition = Vector3.zero;


        if (_storedPrimaryTarget != null) { }

        if (_storedRelativePositions.Count > 0)
        {
            _storedRelativePositions.Clear();
        }

        if (_holograms.Count > 0)
        {
            _holograms.Clear();
        }

        if (_originRenderers.Count > 0) { }

        SafeDestroy(OriginVertexPoint);
        OriginVertexPoint = null;
    }

    public void RestoreOriginMaterials()
    {
        foreach (KeyValuePair<Renderer, Material[]> originalMaterial in _originalMaterials)
        {
            if (originalMaterial.Key != null)
            {
                // Restore exactly the original set
                originalMaterial.Key.materials = originalMaterial.Value;
            }
        }

        _originalMaterials.Clear();
        _originRenderers.Clear();
    }

    public void UpdateOriginMaterialPulse()
    {
        if (_originRenderers.Count == 0)
        {
            return;
        }

        float phase = _pulseTime * PULSE_SPEED + Mathf.PI;
        float a = Mathf.Lerp(MIN_ALPHA, MAX_ALPHA, (Mathf.Sin(phase) + 1f) * 0.5f);


        IEnumerable<Material> originRendererMaterials = _originRenderers
                                                        .Where(r => r)
                                                        .Select(r => r.materials)
                                                        .SelectMany(m => m);
        foreach (Material material in originRendererMaterials)
        {
            SetMaterialOpacity(material, a);
        }
    }

    private static void SetMaterialOpacity(Material material, float a)
    {
        Color color = material.color;
        color.a = a;
        material.color = color;
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
                if (dist <= minDist)
                {
                    minDist = dist;
                    closest = world;
                }
            }
        }

        return closest;
    }


    private void PerformSnap()
    {
        if (_holograms.Count == 0 || PreviouslySelectedBlocks.Count == 0 || _holograms.Count != PreviouslySelectedBlocks.Count)
        {
            MessengerApi.LogError("[Vertexsnapper] Snap failed - hologram/selection mismatch");
        }
        else
        {
            bool flag = false;
            for (int index = 0; index < PreviouslySelectedBlocks.Count; ++index)
            {
                if (!(PreviouslySelectedBlocks[index] == null) &&
                    !(_holograms[index] == null) &&
                    Vector3.Distance(_holograms[index].transform.position, PreviouslySelectedBlocks[index].transform.position) >= 0.01)
                {
                    flag = true;
                    break;
                }
            }

            if (!flag)
            {
                DestroyAllHolograms();
                MessengerApi.LogError("[Vertexsnapper] Snap cancelled (too close to current positions)");
            }
            else
            {
                Before.Clear();
                BeforeSelection.Clear();
                After.Clear();
                AfterSelection.Clear();
                for (int index = 0; index < PreviouslySelectedBlocks.Count; ++index)
                {
                    if (!(PreviouslySelectedBlocks[index] == null) && !(_holograms[index] == null))
                    {
                        Before.Add(PreviouslySelectedBlocks[index].ConvertBlockToJSON_v15_string(true));
                        BeforeSelection.Add(PreviouslySelectedBlocks[index].UID);
                        PreviouslySelectedBlocks[index].transform.position = _holograms[index].transform.position;
                        After.Add(PreviouslySelectedBlocks[index].ConvertBlockToJSON_v15_string(true));
                        AfterSelection.Add(PreviouslySelectedBlocks[index].UID);
                    }
                }

                if (Before.Count > 0)
                {
                    LevelEditorCentral.validation.BreakLock(
                        LevelEditorCentral.undoRedo.ConvertBeforeAndAfterListToCollection(Before, After, PreviouslySelectedBlocks,
                            BeforeSelection, AfterSelection), "Gizmo6");
                }

                DestroyAllHolograms();
                MessengerApi.LogSuccess($"[Vertexsnapper] {PreviouslySelectedBlocks.Count} object(s) snapped successfully!");
            }
        }
    }
}