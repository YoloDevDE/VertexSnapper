using System.Linq;
using TMPro;
using UnityEngine;

namespace VertexSnapper.Components;

public class DistanceIndicator : MonoBehaviour
{
    private static DistanceIndicator _instance;
    private bool _active;

    private LineRenderer _lineRenderer;
    private Vector3 _pointA;
    private Vector3 _pointB;
    private TextMeshPro _textMesh;

    private void Update()
    {
        if (_active)
        {
            UpdateIndicator();
        }
    }

    // --- Static API ---
    public static void Show(Vector3 a, Vector3 b)
    {
        if (!_instance)
        {
            GameObject go = new GameObject("DistanceIndicator");
            _instance = go.AddComponent<DistanceIndicator>();
            _instance.Init();
        }


        _instance.SetPoints(a, b);
    }

    public static void DestroyIndicator()
    {
        if (_instance == null)
        {
            return;
        }

        _instance.Cleanup();
        Destroy(_instance.gameObject);
        _instance = null;
    }

    // --- Instance setup ---
    private void Init()
    {
        // Line setup
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.startWidth = 0.4f;
        _lineRenderer.endWidth = 0.4f;
        _lineRenderer.startColor = Color.yellow;
        _lineRenderer.endColor = Color.yellow;
        _lineRenderer.enabled = false;

        // Text setup
        GameObject textGo = new GameObject("DistanceText");
        textGo.transform.SetParent(transform);
        _textMesh = textGo.AddComponent<TextMeshPro>();
        _textMesh.font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(f => f.name == "ComicHelvetic_Heavy Shadow");
        // _textMesh.fontSize = 28;
        // _textMesh.color = Color.white;
        // _textMesh.alignment = TextAlignmentOptions.Center;

        _textMesh.sortingOrder = 0;
        _textMesh.gameObject.SetActive(false);
    }

    private void SetPoints(Vector3 a, Vector3 b)
    {
        _pointA = a;
        _pointB = b;
        _active = true;

        _lineRenderer.enabled = true;
        _textMesh.gameObject.SetActive(true);
        UpdateIndicator();
    }

    private void UpdateIndicator()
    {
        _lineRenderer.SetPosition(0, _pointA);
        _lineRenderer.SetPosition(1, _pointB);

        float distance = Vector3.Distance(_pointA, _pointB);
        Vector3 midpoint = (_pointA + _pointB) / 2f;

        _textMesh.text = $"{distance:F2} m";
        _textMesh.transform.position = midpoint + Vector3.up * 0.05f;

        if (Camera.main)
        {
            _textMesh.transform.rotation = Quaternion.LookRotation(
                _textMesh.transform.position - Camera.main.transform.position
            );
        }
    }

    private void Cleanup()
    {
        _active = false;
        if (_lineRenderer)
        {
            Destroy(_lineRenderer);
        }

        if (_textMesh)
        {
            Destroy(_textMesh.gameObject);
        }
    }
}