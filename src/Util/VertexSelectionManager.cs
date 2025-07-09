using System.Collections.Generic;
using UnityEngine;
using VertexSnapper.Input;
using Logger = VertexSnapper.Util.Logger;

namespace VertexSnapper.States;

public class VertexSelectionManager : MonoBehaviour
{
    private const float SPHERE_SIZE = 0.5f;
    private const float MOUSE_RADIUS = 3.0f;
    private readonly List<GameObject> _spheres = new List<GameObject>();
    private List<Vector3> _allVertices = new List<Vector3>();

    private Camera _camera;

    private BlockProperties _currentBlock;
    private GameObject _hoveredSphere;
    private Material _hoverMaterial;
    private Material _normalMaterial;
    private GameStateMachine _stateMachine;

    private void Awake()
    {
        _camera = Camera.main;
        CreateMaterials();
    }

    private void Update()
    {
        CheckBlockHover();
        UpdateSpheres();
        CheckSphereHover();
    }

    private void OnDestroy()
    {
        DestroyAllSpheres();
    }

    public void Initialize(GameStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
        stateMachine.BlockSelection = FindObjectOfType<LEV_Selection>().list;
        KeyInput.GetKey(KeyCode.Mouse0).OnKeyDown += HandleMouseClick;
    }

    public void Cleanup()
    {
        KeyInput.GetKey(KeyCode.Mouse0).OnKeyDown -= HandleMouseClick;
        DestroyAllSpheres();
    }

    private void CreateMaterials()
    {
        _normalMaterial = new Material(Shader.Find("Standard"));
        _normalMaterial.color = Color.white;

        _hoverMaterial = new Material(Shader.Find("Standard"));
        _hoverMaterial.color = Color.cyan;
    }

    private void CheckBlockHover()
    {
        Ray ray = _camera.ScreenPointToRay(UnityEngine.Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            return;
        }

        if (hit.collider.gameObject.name == "VertexSnapper_VertexSphere")
        {
            return;
        }

        BlockProperties block = hit.collider.GetComponentInParent<BlockProperties>();
        if (block == null || !IsBlockSelected(block))
        {
            return;
        }

        if (block != _currentBlock)
        {
            _currentBlock = block;
            CollectVertices(block);
        }
    }

    private void CollectVertices(BlockProperties block)
    {
        _allVertices.Clear();
        HashSet<Vector3> cornerVertices = new HashSet<Vector3>();

        foreach (MeshFilter meshFilter in block.GetComponentsInChildren<MeshFilter>())
        {
            if (meshFilter.sharedMesh == null)
            {
                continue;
            }

            // Finde nur Corner-Vertices
            HashSet<Vector3> corners = FindCornerVertices(meshFilter);

            foreach (Vector3 corner in corners)
            {
                cornerVertices.Add(corner);
            }
        }

        _allVertices = new List<Vector3>(cornerVertices);
        Logger.LogInfo($"Found {_allVertices.Count} corner vertices");
    }

    private HashSet<Vector3> FindCornerVertices(MeshFilter meshFilter)
    {
        Mesh mesh = meshFilter.sharedMesh;
        Transform transform = meshFilter.transform;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // Zähle wie oft jeder Vertex in Dreiecken verwendet wird
        Dictionary<int, int> vertexTriangleCount = new Dictionary<int, int>();

        // Sammle alle Edges und zähle sie
        Dictionary<int, HashSet<int>> vertexConnections = new Dictionary<int, HashSet<int>>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];

            // Zähle Triangle-Verwendung
            IncrementVertexCount(vertexTriangleCount, v1);
            IncrementVertexCount(vertexTriangleCount, v2);
            IncrementVertexCount(vertexTriangleCount, v3);

            // Sammle Verbindungen
            AddConnection(vertexConnections, v1, v2);
            AddConnection(vertexConnections, v1, v3);
            AddConnection(vertexConnections, v2, v1);
            AddConnection(vertexConnections, v2, v3);
            AddConnection(vertexConnections, v3, v1);
            AddConnection(vertexConnections, v3, v2);
        }

        HashSet<Vector3> cornerVertices = new HashSet<Vector3>();

        for (int i = 0; i < vertices.Length; i++)
        {
            if (IsCornerVertex(i, vertexConnections, vertexTriangleCount))
            {
                Vector3 worldVertex = transform.TransformPoint(vertices[i]);
                cornerVertices.Add(worldVertex);
            }
        }

        return cornerVertices;
    }

    private void IncrementVertexCount(Dictionary<int, int> dict, int vertex)
    {
        if (!dict.ContainsKey(vertex))
        {
            dict[vertex] = 0;
        }

        dict[vertex]++;
    }

    private void AddConnection(Dictionary<int, HashSet<int>> connections, int from, int to)
    {
        if (!connections.ContainsKey(from))
        {
            connections[from] = new HashSet<int>();
        }

        connections[from].Add(to);
    }

    private bool IsCornerVertex(int vertexIndex, Dictionary<int, HashSet<int>> connections, Dictionary<int, int> triangleCount)
    {
        // Ein Corner-Vertex hat typischerweise:
        // 1. Weniger als 8 Verbindungen (nicht in der Mitte einer Fläche)
        // 2. Weniger als 6 Triangle-Verbindungen (an einer Kante/Ecke)

        if (!connections.ContainsKey(vertexIndex) || !triangleCount.ContainsKey(vertexIndex))
        {
            return false;
        }

        int connectionCount = connections[vertexIndex].Count;
        int triangleUse = triangleCount[vertexIndex];

        // Experimentelle Werte - du kannst diese anpassen
        return connectionCount <= 6 && triangleUse <= 4;
    }

    private void UpdateSpheres()
    {
        if (_currentBlock == null)
        {
            return;
        }

        Vector3 mousePos = GetMouseWorldPosition();
        List<Vector3> nearbyVertices = new List<Vector3>();

        // Find vertices near mouse
        foreach (Vector3 vertex in _allVertices)
        {
            if (Vector3.Distance(vertex, mousePos) <= MOUSE_RADIUS)
            {
                nearbyVertices.Add(vertex);
            }
        }

        // Remove spheres that are too far
        for (int i = _spheres.Count - 1; i >= 0; i--)
        {
            GameObject sphere = _spheres[i];
            if (sphere == null)
            {
                continue;
            }

            bool keepSphere = false;
            foreach (Vector3 vertex in nearbyVertices)
            {
                if (Vector3.Distance(sphere.transform.position, vertex) < 0.01f)
                {
                    keepSphere = true;
                    break;
                }
            }

            if (!keepSphere)
            {
                _spheres.RemoveAt(i);
                Destroy(sphere);
            }
        }

        // Add new spheres
        foreach (Vector3 vertex in nearbyVertices)
        {
            bool sphereExists = false;
            foreach (GameObject sphere in _spheres)
            {
                if (sphere != null && Vector3.Distance(sphere.transform.position, vertex) < 0.01f)
                {
                    sphereExists = true;
                    break;
                }
            }

            if (!sphereExists)
            {
                _spheres.Add(CreateSphere(vertex));
            }
        }
    }

    private GameObject CreateSphere(Vector3 position)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "VertexSnapper_VertexSphere";
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * SPHERE_SIZE;
        sphere.GetComponent<Renderer>().material = _normalMaterial;

        // Setup collider
        Destroy(sphere.GetComponent<Collider>());
        SphereCollider collider = sphere.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = SPHERE_SIZE * 2f;

        return sphere;
    }

    private void CheckSphereHover()
    {
        Ray ray = _camera.ScreenPointToRay(UnityEngine.Input.mousePosition);
        GameObject hitSphere = null;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject.name == "VertexSnapper_VertexSphere")
            {
                hitSphere = hit.collider.gameObject;
            }
        }

        if (hitSphere != _hoveredSphere)
        {
            if (_hoveredSphere != null)
            {
                _hoveredSphere.GetComponent<Renderer>().material = _normalMaterial;
                _hoveredSphere.transform.localScale = Vector3.one * SPHERE_SIZE;
            }

            if (hitSphere != null)
            {
                hitSphere.GetComponent<Renderer>().material = _hoverMaterial;
                hitSphere.transform.localScale = Vector3.one * (SPHERE_SIZE * 2);
            }

            _hoveredSphere = hitSphere;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = _camera.ScreenPointToRay(UnityEngine.Input.mousePosition);
        return Physics.Raycast(ray, out RaycastHit hit) ? hit.point : ray.GetPoint(10f);
    }

    private bool IsBlockSelected(BlockProperties block)
    {
        return _stateMachine.BlockSelection != null && _stateMachine.BlockSelection.Contains(block);
    }

    private void HandleMouseClick()
    {
        if (_hoveredSphere != null)
        {
            _stateMachine.VertexOrigin = _hoveredSphere.transform.position;
            Logger.LogInfo($"Vertex selected at: {_stateMachine.VertexOrigin}");
        }
    }

    private void DestroyAllSpheres()
    {
        foreach (GameObject sphere in _spheres)
        {
            if (sphere != null)
            {
                Destroy(sphere);
            }
        }

        _spheres.Clear();
        _hoveredSphere = null;
    }
}