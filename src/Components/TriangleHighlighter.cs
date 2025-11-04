using System.Collections.Generic;
using UnityEngine;

namespace VertexSnapper.Components;

public class TriangleHighlighter : MonoBehaviour
{
    private static TriangleHighlighter _instance;
    private MeshFilter _filter;

    private GameObject _highlightGO;
    private Mesh _highlightMesh;
    private MeshCollider _lastCollider;
    private int _lastTri = -1;
    private Material _mat;
    private MeshRenderer _renderer;

    public static void Highlight(RaycastHit hit)
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("TriangleHighlighter");
            _instance = go.AddComponent<TriangleHighlighter>();
            _instance.Init();
        }

        _instance.Show(hit);
    }

    public static void Clear()
    {
        if (_instance == null)
        {
            return;
        }

        _instance.Hide();
    }

    private void Init()
    {
        _highlightGO = new GameObject("HighlightTri");
        _filter = _highlightGO.AddComponent<MeshFilter>();
        _renderer = _highlightGO.AddComponent<MeshRenderer>();
        _highlightMesh = new Mesh { name = "TriangleHighlightMesh" };
        _highlightMesh.MarkDynamic();
        _filter.sharedMesh = _highlightMesh;

        // Emissive yellow material (Unlit is simplest; Standard with emission also works)
        _mat = new Material(Shader.Find("Unlit/Color"));
        _mat.color = new Color(1f, 0.9f, 0.1f, 1f);
        _renderer.sharedMaterial = _mat;

        // Render on top a bit (optional): move slightly toward camera using offset or z-bias in a custom shader if you see z-fighting
        _highlightGO.SetActive(false);
    }

    private void Show(RaycastHit hit)
    {
        MeshCollider mc = hit.collider as MeshCollider;
        if (mc == null || mc.sharedMesh == null)
        {
            Hide();
            return;
        }

        Mesh mesh = mc.sharedMesh;
        int tri = hit.triangleIndex;

        if (tri < 0 || (tri * 3) + 2 >= mesh.triangles.Length)
        {
            Hide();
            return;
        }

        // Read triangle vertices (mesh space)
        int[] tris = mesh.triangles;
        int i0 = tris[(tri * 3) + 0];
        int i1 = tris[(tri * 3) + 1];
        int i2 = tris[(tri * 3) + 2];

        Vector3[] verts = mesh.vertices;
        Vector3 v0 = verts[i0];
        Vector3 v1 = verts[i1];
        Vector3 v2 = verts[i2];

        // Parent highlight under the same object so local spaces match
        _highlightGO.transform.SetParent(mc.transform, false);

        // Build/refresh the 1-triangle mesh (in local space)
        _highlightMesh.Clear();
        _highlightMesh.SetVertices(new List<Vector3> { v0, v1, v2 });
        _highlightMesh.SetTriangles(new[] { 0, 1, 2 }, 0, true);

        // Flat normal so it catches light consistently (even though Unlit ignores it)
        Vector3 n = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v0));
        _highlightMesh.SetNormals(new List<Vector3> { n, n, n });

        // Optional: small outward offset to avoid z-fighting
        Vector3 offset = n * 0.001f;
        for (int i = 0; i < 3; i++)
        {
            _highlightMesh.vertices[i] += offset;
        }

        _highlightGO.SetActive(true);
        _lastCollider = mc;
        _lastTri = tri;
    }

    private void Hide()
    {
        _highlightGO.SetActive(false);
        _lastCollider = null;
        _lastTri = -1;
    }
}