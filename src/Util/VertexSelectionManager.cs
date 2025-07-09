using System.Collections.Generic;
using UnityEngine;
using VertexSnapper.Input;
using VertexSnapper.States;

namespace VertexSnapper.Util;

public class VertexSelectionManager : MonoBehaviour
{
    private Camera _camera;
    private GameObject _hoveredSphere;
    private Material _hoverMaterial;
    private Material _normalMaterial;
    private GameStateMachine _stateMachine;
    private readonly List<GameObject> _vertexSpheres = new List<GameObject>();

    private void Awake()
    {
        _camera = Camera.main;
        CreateMaterials();
    }

    private void Update()
    {
        // Simple raycasting for hover detection as backup
        if (_hoveredSphere == null)
        {
            CheckMouseHover();
        }
    }

    public void Initialize(GameStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
        stateMachine.BlockSelection = FindObjectOfType<LEV_Selection>().list;
        CreateVertexSpheres();
        SubscribeToInput();
    }

    public void Cleanup()
    {
        UnsubscribeFromInput();
        DestroyVertexSpheres();
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

    private void CreateVertexSpheres()
    {
        // Use the BlockSelection from the StateMachine
        if (_stateMachine.BlockSelection == null || _stateMachine.BlockSelection.Count == 0)
        {
            Logger.LogWarning("No block selection found in StateMachine");
            return;
        }

        List<BlockProperties> selectedBlocks = _stateMachine.BlockSelection;
        Logger.LogInfo($"Creating vertex spheres for {selectedBlocks.Count} selected blocks");

        HashSet<Vector3> uniqueVertices = new HashSet<Vector3>();

        // Collect all unique vertices from all selected blocks
        foreach (BlockProperties block in selectedBlocks)
        {
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
            }
        }

        Logger.LogInfo($"Found {uniqueVertices.Count} unique vertices");

        // Create spheres for each unique vertex
        foreach (Vector3 vertexPosition in uniqueVertices)
        {
            GameObject sphere = CreateVertexSphere(vertexPosition);
            _vertexSpheres.Add(sphere);
        }
    }

    private GameObject CreateVertexSphere(Vector3 position)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "VertexSnapper_VertexSphere";
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * 0.1f; // Small spheres

        // Set material
        Renderer renderer = sphere.GetComponent<Renderer>();
        renderer.material = _normalMaterial;

        // Remove collider and add a custom trigger collider for mouse detection
        Destroy(sphere.GetComponent<Collider>());
        SphereCollider triggerCollider = sphere.AddComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = 2f; // Larger trigger area for easier selection

        // Add vertex sphere component for mouse interaction
        VertexSphereInteraction interaction = sphere.AddComponent<VertexSphereInteraction>();
        interaction.Initialize(this);

        return sphere;
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
        if (_hoveredSphere != null)
        {
            Vector3 selectedVertex = _hoveredSphere.transform.position;
            Logger.LogInfo($"Vertex selected at position: {selectedVertex}");

            // Store selected vertex in the StateMachine and change state
            _stateMachine.VertexOrigin = selectedVertex;
            _stateMachine.ChangeState(new VertexSnapperStateRoaming());
        }
    }

    public void OnVertexHover(GameObject sphere)
    {
        if (_hoveredSphere != null && _hoveredSphere != sphere)
        {
            // Remove hover from previous sphere
            _hoveredSphere.GetComponent<Renderer>().material = _normalMaterial;
        }

        _hoveredSphere = sphere;
        sphere.GetComponent<Renderer>().material = _hoverMaterial;
    }

    public void OnVertexExit(GameObject sphere)
    {
        if (_hoveredSphere == sphere)
        {
            _hoveredSphere = null;
            sphere.GetComponent<Renderer>().material = _normalMaterial;
        }
    }

    private void DestroyVertexSpheres()
    {
        foreach (GameObject sphere in _vertexSpheres)
        {
            if (sphere != null)
            {
                Destroy(sphere);
            }
        }

        _vertexSpheres.Clear();
        _hoveredSphere = null;
    }

    private void CheckMouseHover()
    {
        Ray ray = _camera.ScreenPointToRay(UnityEngine.Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            if (hitObject.name == "VertexSnapper_VertexSphere")
            {
                OnVertexHover(hitObject);
            }
        }
    }
}

// Helper component for vertex sphere mouse interaction
public class VertexSphereInteraction : MonoBehaviour
{
    private VertexSelectionManager _manager;

    private void OnMouseEnter()
    {
        _manager?.OnVertexHover(gameObject);
    }

    private void OnMouseExit()
    {
        _manager?.OnVertexExit(gameObject);
    }

    public void Initialize(VertexSelectionManager manager)
    {
        _manager = manager;
    }
}