using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VertexSnapper.Components;

namespace VertexSnapper.Managers;

public class VertexSphereManager
{
    private readonly HashSet<GameObject> spheres = [];

    private VertexSphereManager()
    {
    }

    public static VertexSphereManager Instance { get; } = new VertexSphereManager();

    private float SphereRadius { get; } = 1f / 5f;
    private Color SphereColor { get; } = Color.cyan;

    public void CreateSphereAt(Vector3 position)
    {
        // Prüfen ob an dieser Position schon ein Sphere liegt
        if (spheres.Any(gameObject => gameObject && gameObject.transform.position == position))
        {
            return;
        }

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "VertexSnapper_VertexSphere";
        sphere.transform.position = position;

        VertexSphere comp = sphere.AddComponent<VertexSphere>();
        comp.Initialize(SphereRadius, SphereColor);

        spheres.Add(sphere);
    }

    public void DestroySphereAt(Vector3 position)
    {
        GameObject sphere = spheres.FirstOrDefault(s => s != null && s.transform.position == position);
        if (sphere == null)
        {
            return;
        }

        VertexSphere comp = sphere.GetComponent<VertexSphere>();
        if (comp != null)
        {
            comp.RequestDespawn(() => spheres.Remove(sphere));
        }
        else
        {
            Object.Destroy(sphere);
            spheres.Remove(sphere);
        }
    }

    public void DestroyAllSpheres()
    {
        List<GameObject> all = spheres.ToList();
        foreach (GameObject s in all)
        {
            DestroySphere(s);
        }

        spheres.Clear();
    }

    public void DestroyAllOtherSpheres(List<Vector3> keepPositions)
    {
        List<GameObject> toRemove = spheres
            .Where(s => s != null && !keepPositions.Contains(s.transform.position))
            .ToList();

        foreach (GameObject s in toRemove)
        {
            DestroySphere(s);
        }
    }

    private void DestroySphere(GameObject sphere)
    {
        VertexSphere comp = sphere.GetComponent<VertexSphere>();
        if (comp != null)
        {
            comp.RequestDespawn(() => spheres.Remove(sphere));
        }
        else
        {
            Object.Destroy(sphere);
            spheres.Remove(sphere);
        }
    }
}