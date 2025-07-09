using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Logger = VertexSnapper.Util.Logger;

namespace VertexSnapper;

public class VertexSnapper : MonoBehaviour
{
    private List<BlockProperties> _blockSelection;
    private Camera _camera;
    private GameObject _previewSphere;

    private bool hasfoundthing = false;

    private void Awake()
    {
        Logger.LogInfo($"[{GetType().Name}] Awake");
        _camera = Camera.main;
    }

    private void Update()
    {
        // // Check if the "^" key is being held (typically the caret key, which is Shift+6)
        // bool isHoldingKey = Input.GetKeyDown(KeyCode.F13);
        // if (hasfoundthing)
        // {
        //     return;
        // }
        //
        // hasfoundthing =  (Input.GetMouseButton(0) && _previewSphere != null);
        // if (isHoldingKey  )
        // {
        //     HandlePreviewSphere();
        //     LevelEditorApi.BlockMouseInput(this);
        // }
        // else
        // {
        //     
        //     DestroyPreviewSphere();
        //     LevelEditorApi.UnblockMouseInput(this);
        // }
    }

    private void OnEnable()
    {
        Logger.LogInfo($"[{GetType().Name}] OnEnable");
        _camera = Camera.main;
    }

    private void OnDisable()
    {
        Logger.LogInfo($"[{GetType().Name}] OnDisable");
        DestroyPreviewSphere();
    }

    private void OnDestroy()
    {
        Logger.LogInfo($"[{GetType().Name}] Destroy");
        DestroyPreviewSphere();
    }

    private void HandlePreviewSphere()
    {
        // Get all block properties in the scene
        List<BlockProperties> allBlocks = FindObjectsOfType<BlockProperties>().ToList();
        allBlocks = FindObjectOfType<LEV_LevelEditorCentral>().selection.list;

        if (allBlocks.Count == 0)
        {
            DestroyPreviewSphere();
            return;
        }

        // Try to find the closest edge point first
        Vector3? closestPoint = GetClosestEdgeToMouse(allBlocks);

        // If no edge found, fallback to closest vertex
        if (!closestPoint.HasValue)
        {
            Logger.LogInfo("No edges found, falling back to vertices");
            closestPoint = GetClosestVertexToMouse(allBlocks);
        }

        if (closestPoint.HasValue)
        {
            // Create or update the preview sphere
            if (_previewSphere == null)
            {
                CreatePreviewSphere();
            }

            // Position the sphere at the closest point
            _previewSphere.transform.position = closestPoint.Value;
            Logger.LogInfo($"Sphere positioned at: {closestPoint.Value}");
        }
        else
        {
            Logger.LogInfo("No valid points found");
            DestroyPreviewSphere();
        }
    }

    private Vector3? GetClosestEdgeToMouse(List<BlockProperties> blocks)
    {
        Vector2 mouseScreenPos = UnityEngine.Input.mousePosition;
        float minDistance = float.MaxValue;
        Vector3? closestPoint = null;

        // Get all MeshFilters from blocks and their children
        List<MeshFilter> allMeshFilters = new List<MeshFilter>();
        List<MeshFilter[]> meshFilterRoot = blocks.Select(block => block.GetComponentsInChildren<MeshFilter>()).ToList();
        foreach (MeshFilter[] meshFilters in meshFilterRoot)
        {
            allMeshFilters.AddRange(meshFilters);
        }

        Logger.LogInfo($"Found {allMeshFilters.Count} total MeshFilters");

        foreach (MeshFilter meshFilter in allMeshFilters)
        {
            if (meshFilter.sharedMesh == null)
            {
                continue;
            }

            Mesh mesh = meshFilter.sharedMesh;
            Transform transform = meshFilter.transform;

            // Get mesh data
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            // Extract all unique edges from triangles
            HashSet<(int, int)> edges = new HashSet<(int, int)>();

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v1 = triangles[i];
                int v2 = triangles[i + 1];
                int v3 = triangles[i + 2];

                // Add edges (ensure consistent ordering with smaller index first)
                edges.Add((Mathf.Min(v1, v2), Mathf.Max(v1, v2)));
                edges.Add((Mathf.Min(v2, v3), Mathf.Max(v2, v3)));
                edges.Add((Mathf.Min(v3, v1), Mathf.Max(v3, v1)));
            }

            // Check each edge - but snap to the closest vertex (endpoint)
            foreach ((int, int) edge in edges)
            {
                Vector3 edgeStart = transform.TransformPoint(vertices[edge.Item1]);
                Vector3 edgeEnd = transform.TransformPoint(vertices[edge.Item2]);

                // Convert world positions to screen positions
                Vector3 edgeStartScreen = _camera.WorldToScreenPoint(edgeStart);
                Vector3 edgeEndScreen = _camera.WorldToScreenPoint(edgeEnd);

                // Skip if edge is behind camera
                if (edgeStartScreen.z < 0 || edgeEndScreen.z < 0)
                {
                    continue;
                }

                // Check distance to start vertex
                Vector2 edgeStartScreen2D = new Vector2(edgeStartScreen.x, edgeStartScreen.y);
                float distanceToStart = Vector2.Distance(mouseScreenPos, edgeStartScreen2D);

                if (distanceToStart < minDistance)
                {
                    minDistance = distanceToStart;
                    closestPoint = edgeStart;
                }

                // Check distance to end vertex
                Vector2 edgeEndScreen2D = new Vector2(edgeEndScreen.x, edgeEndScreen.y);
                float distanceToEnd = Vector2.Distance(mouseScreenPos, edgeEndScreen2D);

                if (distanceToEnd < minDistance)
                {
                    minDistance = distanceToEnd;
                    closestPoint = edgeEnd;
                }
            }
        }

        Logger.LogInfo($"Closest vertex distance: {minDistance} pixels");

        // Only return if distance is reasonable (within 100 pixels)
        return minDistance < 100f ? closestPoint : null;
    }


    private Vector3? GetClosestVertexToMouse(List<BlockProperties> blocks)
    {
        // Placeholder - du musst diese Methode noch implementieren
        // oder sie existiert bereits in deinem Code
        Logger.LogInfo("GetClosestVertexToMouse called - implement this method");
        return null;
    }

    private void CreatePreviewSphere()
    {
        _previewSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _previewSphere.name = "VertexSnapper_PreviewSphere";

        // Make it semi-transparent
        Renderer renderer = _previewSphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(1f, 0f, 0f, 0.5f); // Red with transparency
            material.SetFloat("_Mode", 3); // Transparent mode
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            renderer.material = material;
        }

        // Remove collider so it doesn't interfere with other objects
        Collider collider = _previewSphere.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        // Scale it down to make it more visible as a preview
        _previewSphere.transform.localScale = Vector3.one * 0.5f;
    }

    private void DestroyPreviewSphere()
    {
        if (_previewSphere != null)
        {
            Destroy(_previewSphere);
            _previewSphere = null;
        }
    }
}