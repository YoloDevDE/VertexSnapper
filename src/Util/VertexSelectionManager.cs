using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VertexSnapper.Input;
using VertexSnapper.States;

namespace VertexSnapper.Util;

public class VertexSelectionManager : MonoBehaviour
{
    private const float SPHERE_SIZE = 0.5f;
    private const int VERTICES_PER_FRAME = 50; // How many spheres to create per frame
    private readonly List<GameObject> _currentVertexSpheres = new List<GameObject>();

    private Camera _camera;
    private BlockProperties _hoveredBlock;
    private GameObject _hoveredVertexSphere;
    private Material _hoverMaterial;
    private Material _normalMaterial;
    private GameStateMachine _stateMachine;
    private Coroutine _createSpheresCoroutine;

    private void Awake()
    {
        _camera = Camera.main;
        CreateMaterials();
    }

    private void Update()
    {
        CheckVertexSphereHover();
        CheckBlockHover();
    }

    public void Initialize(GameStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
        stateMachine.BlockSelection = FindObjectOfType<LEV_Selection>().list;
        SubscribeToInput();
    }

    public void Cleanup()
    {
        UnsubscribeFromInput();
        DestroyCurrentVertexSpheres();
    }

    private void CreateMaterials()
    {
        // Normal material - small white spheres
        _normalMaterial = new Material(Shader.Find("Standard"));
        _normalMaterial.color = Color.white;
        _normalMaterial.SetFloat("_Metallic", 0f);
        _normalMaterial.SetFloat("_Glossiness", 0.5f);

        // Hover material - highlighted (e.g. yellow)
        _hoverMaterial = new Material(Shader.Find("Standard"));
        _hoverMaterial.color = Color.yellow;
        _hoverMaterial.SetFloat("_Metallic", 0f);
        _hoverMaterial.SetFloat("_Glossiness", 0.8f);
    }

    private void CheckBlockHover()
    {
        Ray ray = _camera.ScreenPointToRay(UnityEngine.Input.mousePosition);
        RaycastHit hit;

        BlockProperties hitBlock = null;

        if (Physics.Raycast(ray, out hit))
        {
            // Ignore vertex spheres - keep the current hovered block
            if (hit.collider.gameObject.name == "VertexSnapper_VertexSphere")
            {
                hitBlock = _hoveredBlock; // Keep current block
            }
            else
            {
                // Check if we hit a selected block
                BlockProperties blockProperties = hit.collider.GetComponentInParent<BlockProperties>();
                if (blockProperties != null && IsBlockSelected(blockProperties))
                {
                    hitBlock = blockProperties;
                }
            }
        }

        // Block hover changed
        if (hitBlock == _hoveredBlock)
        {
            return;
        }

        if (_hoveredBlock != null)
        {
            OnBlockLeave(_hoveredBlock);
        }

        if (hitBlock != null)
        {
            OnBlockEnter(hitBlock);
        }

        _hoveredBlock = hitBlock;
    }

    private void CheckVertexSphereHover()
    {
        if (_currentVertexSpheres.Count == 0)
        {
            return;
        }

        Ray ray = _camera.ScreenPointToRay(UnityEngine.Input.mousePosition);
        RaycastHit hit;

        GameObject hitVertexSphere = null;
        if (Physics.Raycast(ray, out hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            if (hitObject.name == "VertexSnapper_VertexSphere")
            {
                hitVertexSphere = hitObject;
            }
        }

        if (_hoveredVertexSphere != null)
        {
            OnVertexSphereLeave(_hoveredVertexSphere);
        }

        if (hitVertexSphere != null)
        {
            OnVertexSphereEnter(hitVertexSphere);
        }

        _hoveredVertexSphere = hitVertexSphere;
    }

    private bool IsBlockSelected(BlockProperties block)
    {
        return _stateMachine.BlockSelection != null && _stateMachine.BlockSelection.Contains(block);
    }

    private void OnBlockEnter(BlockProperties block)
    {
        Logger.LogInfo($"Mouse entered block: {block.name}");

        // Stop any existing coroutine
        if (_createSpheresCoroutine != null)
        {
            StopCoroutine(_createSpheresCoroutine);
        }

        // Start creating spheres gradually
        _createSpheresCoroutine = StartCoroutine(CreateVertexSpheresCoroutine(block));
    }

    private void OnBlockLeave(BlockProperties block)
    {
        Logger.LogInfo($"Mouse left block: {block.name}");

        // Stop sphere creation if running
        if (_createSpheresCoroutine != null)
        {
            StopCoroutine(_createSpheresCoroutine);
            _createSpheresCoroutine = null;
        }

        DestroyCurrentVertexSpheres();
    }

    private IEnumerator CreateVertexSpheresCoroutine(BlockProperties block)
    {
        // First, collect all unique vertices
        HashSet<Vector3> uniqueVertices = new HashSet<Vector3>();
        MeshFilter[] meshFilters = block.GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh == null)
            {
                continue;
            }

            Mesh mesh = meshFilter.sharedMesh;
            Transform transform = meshFilter.transform;
            Vector3[] vertices = mesh.vertices;

            // Transform vertices to world space and add to set
            foreach (Vector3 vertex in vertices)
            {
                Vector3 worldVertex = transform.TransformPoint(vertex);
                uniqueVertices.Add(worldVertex);
            }

            // Yield after processing each mesh to prevent frame drops
            yield return null;
        }

        Logger.LogInfo($"Creating {uniqueVertices.Count} vertex spheres for block: {block.name}");

        // Convert to list for easier processing
        List<Vector3> vertexList = new List<Vector3>(uniqueVertices);

        // Create spheres in batches
        for (int i = 0; i < vertexList.Count; i += VERTICES_PER_FRAME)
        {
            // Create a batch of spheres
            int endIndex = Mathf.Min(i + VERTICES_PER_FRAME, vertexList.Count);

            for (int j = i; j < endIndex; j++)
            {
                GameObject sphere = CreateVertexSphere(vertexList[j]);
                _currentVertexSpheres.Add(sphere);
            }

            // Wait one frame before creating the next batch
            yield return null;
        }

        Logger.LogInfo($"Finished creating {_currentVertexSpheres.Count} vertex spheres");
        _createSpheresCoroutine = null;
    }


    private GameObject CreateVertexSphere(Vector3 position)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "VertexSnapper_VertexSphere";
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * SPHERE_SIZE; // Small spheres

        // Set material
        Renderer renderer = sphere.GetComponent<Renderer>();
        renderer.material = _normalMaterial;

        // Remove collider and add a custom trigger collider for mouse detection
        Destroy(sphere.GetComponent<Collider>());
        SphereCollider triggerCollider = sphere.AddComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius *= 1.1f;

        return sphere;
    }

    private void OnVertexSphereEnter(GameObject sphere)
    {
        sphere.GetComponent<Renderer>().material = _hoverMaterial;
        sphere.transform.localScale = Vector3.one * (SPHERE_SIZE * 2); // Slightly bigger when hovered
    }

    private void OnVertexSphereLeave(GameObject sphere)
    {
        if (sphere != null)
        {
            sphere.GetComponent<Renderer>().material = _normalMaterial;
            sphere.transform.localScale = Vector3.one * SPHERE_SIZE; // Back to normal size
        }
    }

    private void DestroyCurrentVertexSpheres()
    {
        foreach (GameObject sphere in _currentVertexSpheres)
        {
            if (sphere != null)
            {
                Destroy(sphere);
            }
        }

        _currentVertexSpheres.Clear();
        _hoveredVertexSphere = null;
    }

    private void SubscribeToInput()
    {
        KeyInput.GetKey(KeyCode.Mouse0).OnKeyDown += HandleMouseClick;
    }

    private void UnsubscribeFromInput()
    {
        KeyInput.GetKey(KeyCode.Mouse0).OnKeyDown -= HandleMouseClick;
    }

    private void HandleMouseClick()
    {
        if (_hoveredVertexSphere != null)
        {
            Vector3 selectedVertex = _hoveredVertexSphere.transform.position;
            Logger.LogInfo($"Vertex selected at position: {selectedVertex}");

            // Store selected vertex in the StateMachine and change state
            _stateMachine.VertexOrigin = selectedVertex;
            // _stateMachine.ChangeState(new VertexSnapperStateRoaming());
        }
    }
}