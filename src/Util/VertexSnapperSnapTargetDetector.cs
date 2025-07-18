using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VertexSnapper.Config;
using VertexSnapper.Input;
using Logger = VertexSnapper.Util.Logger;

namespace VertexSnapper.States;

public class SnapTargetDetector : MonoBehaviour
{
    private const float SPHERE_SIZE = 0.3f;
    private const float MOUSE_RADIUS = 3.0f;
    private const float MAX_VERTEX_SCREEN_DISTANCE = 100f; // Max Pixel-Distanz zur Maus
    private readonly List<GameObject> _targetSpheres = new List<GameObject>();

    private Camera _camera;
    private BlockProperties _currentTargetBlock;
    private GameObject _hoveredTargetSphere;
    private bool _isVertexModeActive;
    private GameObject _previewHologram;
    private GameStateMachine _stateMachine;
    private Material _targetSphereMaterial;
    private Vector3 _targetVertex;
    private List<Vector3> _targetVertices = new List<Vector3>();

    private void Awake()
    {
        _camera = Camera.main;
        CreateMaterials();
    }

    private void Update()
    {
        if (_isVertexModeActive)
        {
            HandleVertexMode();
        }
        else
        {
            CleanupVertexMode();
        }
    }

    private void OnDestroy()
    {
        CleanupVertexMode();
        DestroyHologram();
    }

    public void Initialize(GameStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
        KeyInput.GetKey(KeyCode.Mouse0).OnKeyDown += HandleMouseClick;

        // Subscribe to VertexMode key events
        KeyInput.GetKey(VertexSnapperConfig.Instance.VertexMode.Value).OnKeyDown += OnVertexModeKeyDown;
        KeyInput.GetKey(VertexSnapperConfig.Instance.VertexMode.Value).OnKeyUp += OnVertexModeKeyUp;
    }

    public void Cleanup()
    {
        KeyInput.GetKey(KeyCode.Mouse0).OnKeyDown -= HandleMouseClick;

        // Unsubscribe from VertexMode key events
        KeyInput.GetKey(VertexSnapperConfig.Instance.VertexMode.Value).OnKeyDown -= OnVertexModeKeyDown;
        KeyInput.GetKey(VertexSnapperConfig.Instance.VertexMode.Value).OnKeyUp -= OnVertexModeKeyUp;

        CleanupVertexMode();
        DestroyHologram();
    }

    private void OnVertexModeKeyDown()
    {
        _isVertexModeActive = true;
        Logger.LogInfo("VertexMode activated");
    }

    private void OnVertexModeKeyUp()
    {
        _isVertexModeActive = false;
        Logger.LogInfo("VertexMode deactivated");
        CleanupVertexMode();
    }

    private void CreateMaterials()
    {
        _targetSphereMaterial = new Material(Shader.Find("Standard"));
        _targetSphereMaterial.color = Color.green;
    }

    private void HandleVertexMode()
    {
        // Finde das nächstgelegene Vertex zu der Maus (über alle Objekte in der Szene)
        Vector3? closestVertex = FindClosestVertexToMouse();

        if (closestVertex.HasValue)
        {
            // Aktualisiere oder erstelle Hologramm an der Vertex-Position
            UpdateHologramAtVertex(closestVertex.Value);
        }
        else
        {
            // Kein Vertex nahe genug - entferne Hologramm
            DestroyHologram();
        }

        // Separate Logik für Target-Sphären (nur für spezifische Blöcke)
        HandleTargetBlocks();
    }

    private Vector3? FindClosestVertexToMouse()
    {
        Vector2 mouseScreenPos = UnityEngine.Input.mousePosition;
        float minDistance = float.MaxValue;
        Vector3? closestVertex = null;

        // Durchsuche alle Objekte in der Szene nach Vertices
        BlockProperties[] allBlocks = FindObjectsOfType<BlockProperties>();

        foreach (BlockProperties block in allBlocks)
        {
            // Überspringe die eigene Selection (die wollen wir nicht als Ziel)
            if (_stateMachine.BlockSelection != null && _stateMachine.BlockSelection.Contains(block))
            {
                continue;
            }

            foreach (MeshFilter meshFilter in block.GetComponentsInChildren<MeshFilter>())
            {
                if (meshFilter.sharedMesh == null)
                {
                    continue;
                }

                HashSet<Vector3> vertices = FindCornerVertices(meshFilter);

                foreach (Vector3 vertex in vertices)
                {
                    // Konvertiere World-Position zu Screen-Position
                    Vector3 vertexScreen = _camera.WorldToScreenPoint(vertex);

                    // Überspringe Vertices hinter der Kamera
                    if (vertexScreen.z < 0)
                    {
                        continue;
                    }

                    // Berechne 2D-Distanz auf dem Bildschirm
                    Vector2 vertexScreen2D = new Vector2(vertexScreen.x, vertexScreen.y);
                    float distance = Vector2.Distance(mouseScreenPos, vertexScreen2D);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestVertex = vertex;
                    }
                }
            }
        }

        // Nur zurückgeben wenn die Distanz vernünftig ist
        return minDistance < MAX_VERTEX_SCREEN_DISTANCE ? closestVertex : null;
    }

    private void UpdateHologramAtVertex(Vector3 targetVertex)
    {
        if (_previewHologram == null)
        {
            CreateHologramAtVertex(targetVertex);
        }
        else
        {
            // Positioniere das Hologramm am Ziel-Vertex relativ zum ursprünglichen Vertex
            Vector3 offset = targetVertex - _stateMachine.VertexOrigin;
            _previewHologram.transform.position = _stateMachine.VertexOrigin + offset;
        }
    }

    private void CreateHologramAtVertex(Vector3 targetVertex)
    {
        if (_stateMachine.BlockSelection == null || _stateMachine.BlockSelection.Count == 0)
        {
            return;
        }

        _previewHologram = new GameObject("VertexSnapper_SelectionHologram");

        // Berechne die neue Position für das Hologramm
        Vector3 offset = targetVertex - _stateMachine.VertexOrigin;
        Vector3 hologramBasePosition = _stateMachine.VertexOrigin + offset;

        // Erstelle eine Kopie jedes ausgewählten Blocks
        foreach (BlockProperties block in _stateMachine.BlockSelection)
        {
            GameObject blockCopy = Instantiate(block.gameObject, _previewHologram.transform);

            // Berechne relative Position zum ursprünglichen VertexOrigin
            Vector3 relativePosition = block.transform.position - _stateMachine.VertexOrigin;
            blockCopy.transform.position = hologramBasePosition + relativePosition;

            // Mache es zu einem Hologramm
            MakeHologram(blockCopy);
        }
    }

    private void HandleTargetBlocks()
    {
        // Finde Ziel-Block per Raycast (für die grünen Sphären)
        Ray ray = _camera.ScreenPointToRay(UnityEngine.Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            ClearTargetSpheres();
            return;
        }

        // Ignoriere eigene Sphären und Hologramme
        if (hit.collider.gameObject.name.StartsWith("VertexSnapper_"))
        {
            return;
        }

        BlockProperties targetBlock = hit.collider.GetComponentInParent<BlockProperties>();
        if (targetBlock == null)
        {
            ClearTargetSpheres();
            return;
        }

        // Prüfe ob es sich um einen ausgewählten Block handelt (den wollen wir nicht als Ziel)
        if (_stateMachine.BlockSelection != null && _stateMachine.BlockSelection.Contains(targetBlock))
        {
            ClearTargetSpheres();
            return;
        }

        // Neuer Ziel-Block gefunden
        if (targetBlock != _currentTargetBlock)
        {
            _currentTargetBlock = targetBlock;
            CollectTargetVertices(targetBlock);
        }

        UpdateTargetSpheres();
        CheckTargetSphereHover();
    }

    private void ClearTargetSpheres()
    {
        _currentTargetBlock = null;
        _hoveredTargetSphere = null;
        _targetVertex = Vector3.zero;

        // Entferne alle Ziel-Sphären
        for (int i = _targetSpheres.Count - 1; i >= 0; i--)
        {
            if (_targetSpheres[i] != null)
            {
                Destroy(_targetSpheres[i]);
            }
        }

        _targetSpheres.Clear();
    }

    private void CollectTargetVertices(BlockProperties block)
    {
        _targetVertices.Clear();
        HashSet<Vector3> cornerVertices = new HashSet<Vector3>();

        foreach (MeshFilter meshFilter in block.GetComponentsInChildren<MeshFilter>())
        {
            if (meshFilter.sharedMesh == null)
            {
                continue;
            }

            HashSet<Vector3> corners = FindCornerVertices(meshFilter);
            foreach (Vector3 corner in corners)
            {
                cornerVertices.Add(corner);
            }
        }

        _targetVertices = new List<Vector3>(cornerVertices);
        Logger.LogInfo($"Found {_targetVertices.Count} target vertices");
    }

    private HashSet<Vector3> FindCornerVertices(MeshFilter meshFilter)
    {
        // Wiederverwendung der gleichen Logik wie in VertexSelectionManager
        Mesh mesh = meshFilter.sharedMesh;
        Transform transform = meshFilter.transform;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        Dictionary<int, int> vertexTriangleCount = new Dictionary<int, int>();
        Dictionary<int, HashSet<int>> vertexConnections = new Dictionary<int, HashSet<int>>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];

            IncrementVertexCount(vertexTriangleCount, v1);
            IncrementVertexCount(vertexTriangleCount, v2);
            IncrementVertexCount(vertexTriangleCount, v3);

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
        if (!connections.ContainsKey(vertexIndex) || !triangleCount.ContainsKey(vertexIndex))
        {
            return false;
        }

        int connectionCount = connections[vertexIndex].Count;
        int triangleUse = triangleCount[vertexIndex];

        return connectionCount <= 6 && triangleUse <= 4;
    }

    private void UpdateTargetSpheres()
    {
        if (_currentTargetBlock == null)
        {
            return;
        }

        Vector3 mousePos = GetMouseWorldPosition();
        List<Vector3> nearbyVertices = new List<Vector3>();

        // Finde Vertices nahe der Maus
        foreach (Vector3 vertex in _targetVertices)
        {
            if (Vector3.Distance(vertex, mousePos) <= MOUSE_RADIUS)
            {
                nearbyVertices.Add(vertex);
            }
        }

        // Entferne weit entfernte Sphären
        for (int i = _targetSpheres.Count - 1; i >= 0; i--)
        {
            GameObject sphere = _targetSpheres[i];
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
                _targetSpheres.RemoveAt(i);
                Destroy(sphere);
            }
        }

        // Füge neue Sphären hinzu
        foreach (Vector3 vertex in nearbyVertices)
        {
            bool sphereExists = false;
            foreach (GameObject sphere in _targetSpheres)
            {
                if (sphere != null && Vector3.Distance(sphere.transform.position, vertex) < 0.01f)
                {
                    sphereExists = true;
                    break;
                }
            }

            if (!sphereExists)
            {
                _targetSpheres.Add(CreateTargetSphere(vertex));
            }
        }
    }

    private GameObject CreateTargetSphere(Vector3 position)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "VertexSnapper_TargetSphere";
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * SPHERE_SIZE;
        sphere.GetComponent<Renderer>().material = _targetSphereMaterial;

        // Setup collider
        Destroy(sphere.GetComponent<Collider>());
        SphereCollider collider = sphere.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = SPHERE_SIZE * 2f;

        return sphere;
    }

    private void CheckTargetSphereHover()
    {
        Ray ray = _camera.ScreenPointToRay(UnityEngine.Input.mousePosition);
        GameObject hitSphere = null;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject.name == "VertexSnapper_TargetSphere")
            {
                hitSphere = hit.collider.gameObject;
            }
        }

        if (hitSphere != _hoveredTargetSphere)
        {
            if (_hoveredTargetSphere != null)
            {
                _hoveredTargetSphere.transform.localScale = Vector3.one * SPHERE_SIZE;
            }

            if (hitSphere != null)
            {
                hitSphere.transform.localScale = Vector3.one * (SPHERE_SIZE * 1.5f);
                _targetVertex = hitSphere.transform.position;
            }

            _hoveredTargetSphere = hitSphere;
        }
    }

    private void MakeHologram(GameObject obj)
    {
        // Entferne alle Collider
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            Destroy(col);
        }

        // Entferne alle Scripts
        MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            Destroy(script);
        }

        // Mache alle Renderer transparent
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material hologramMaterial = new Material(Shader.Find("Standard"));
                hologramMaterial.color = new Color(0f, 1f, 0f, 0.3f); // Grün transparent
                hologramMaterial.SetFloat("_Mode", 3); // Transparent mode
                hologramMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                hologramMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                hologramMaterial.SetInt("_ZWrite", 0);
                hologramMaterial.EnableKeyword("_ALPHABLEND_ON");
                hologramMaterial.renderQueue = 3000;
                materials[i] = hologramMaterial;
            }

            renderer.materials = materials;
        }
    }

    private void DestroyHologram()
    {
        if (_previewHologram != null)
        {
            Destroy(_previewHologram);
            _previewHologram = null;
        }
    }

    private void CleanupVertexMode()
    {
        ClearTargetSpheres();
        DestroyHologram();
    }

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = _camera.ScreenPointToRay(UnityEngine.Input.mousePosition);
        return Physics.Raycast(ray, out RaycastHit hit) ? hit.point : ray.GetPoint(10f);
    }

    private void HandleMouseClick()
    {
        if (_isVertexModeActive && _previewHologram != null)
        {
            Vector3? closestVertex = FindClosestVertexToMouse();
            if (closestVertex.HasValue)
            {
                Logger.LogInfo($"Snapping selection to vertex: {closestVertex.Value}");
                PerformSnap(closestVertex.Value);
            }
        }
    }

    private void PerformSnap(Vector3 targetPosition)
    {
        if (_stateMachine.BlockSelection == null || _stateMachine.BlockSelection.Count == 0)
        {
            return;
        }

        // Berechne den Offset zwischen dem ursprünglichen Vertex und der Zielposition
        Vector3 snapOffset = targetPosition - _stateMachine.VertexOrigin;

        // Bewege alle ausgewählten Blöcke um diesen Offset
        foreach (BlockProperties block in _stateMachine.BlockSelection)
        {
            block.transform.position += snapOffset;
        }

        Logger.LogInfo($"Snapped {_stateMachine.BlockSelection.Count} blocks by offset: {snapOffset}");

        // Cleanup und zurück zur Idle-State
        _stateMachine.ChangeState(new VertexSnapperStateIdle());
    }
}