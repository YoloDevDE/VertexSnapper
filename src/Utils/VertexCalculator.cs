using System.Collections.Generic;
using UnityEngine;
using VertexSnapper.Core;

namespace VertexSnapper.Utils;

public class VertexCalculator
{
    private readonly VertexSnapLogger logger;

    public VertexCalculator(VertexSnapLogger logger)
    {
        this.logger = logger;
    }

    public Vector3 GetClosestVertex(MeshFilter[] meshFilters, Vector3 worldPoint)
    {
        logger.LogMethodEntry(nameof(GetClosestVertex));
        logger.LogVariableValue("worldPoint", worldPoint);
        logger.LogObjectCount("meshFilters", meshFilters?.Length ?? 0);

        if (meshFilters == null || meshFilters.Length == 0)
        {
            logger.LogWarning("No mesh filters provided");
            logger.LogMethodExit(nameof(GetClosestVertex), "worldPoint (no meshes)");
            return worldPoint;
        }

        Vector3 closestVertex = worldPoint;
        float closestDistance = float.MaxValue;
        int totalVerticesChecked = 0;

        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter?.sharedMesh != null && meshFilter.transform != null)
            {
                Vector3[] vertices = meshFilter.sharedMesh.vertices;
                Transform transform = meshFilter.transform;

                foreach (Vector3 vertex in vertices)
                {
                    Vector3 worldVertex = transform.TransformPoint(vertex);
                    float distance = Vector3.Distance(worldPoint, worldVertex);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestVertex = worldVertex;
                    }

                    totalVerticesChecked++;
                }
            }
        }

        logger.LogVariableValue("vertices checked", totalVerticesChecked);
        logger.LogVariableValue("closest distance", closestDistance);
        logger.LogVariableValue("closestVertex", closestVertex);
        logger.LogMethodExit(nameof(GetClosestVertex));

        return closestVertex;
    }

    public Vector3[] GetAllVertices(MeshFilter[] meshFilters, Transform relativeTo = null)
    {
        logger.LogMethodEntry(nameof(GetAllVertices));
        logger.LogObjectCount("meshFilters", meshFilters?.Length ?? 0);

        if (meshFilters == null || meshFilters.Length == 0)
        {
            logger.LogWarning("No mesh filters provided");
            logger.LogMethodExit(nameof(GetAllVertices), "empty array");
            return new Vector3[0];
        }

        List<Vector3> allVertices = new List<Vector3>();

        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter?.sharedMesh != null && meshFilter.transform != null)
            {
                Vector3[] vertices = meshFilter.sharedMesh.vertices;
                Transform transform = meshFilter.transform;

                foreach (Vector3 vertex in vertices)
                {
                    Vector3 worldVertex = transform.TransformPoint(vertex);

                    if (relativeTo != null)
                    {
                        worldVertex = relativeTo.InverseTransformPoint(worldVertex);
                    }

                    allVertices.Add(worldVertex);
                }
            }
        }

        logger.LogVariableValue("total vertices collected", allVertices.Count);
        logger.LogMethodExit(nameof(GetAllVertices));

        return allVertices.ToArray();
    }

    public bool IsVertexOnSurface(Vector3 vertex, MeshFilter meshFilter, float tolerance = 0.01f)
    {
        logger.LogMethodEntry(nameof(IsVertexOnSurface));
        logger.LogVariableValue("vertex", vertex);
        logger.LogVariableValue("tolerance", tolerance);

        if (meshFilter?.sharedMesh == null || meshFilter.transform == null)
        {
            logger.LogWarning("Invalid mesh filter");
            logger.LogMethodExit(nameof(IsVertexOnSurface), "false (invalid mesh)");
            return false;
        }

        Mesh mesh = meshFilter.sharedMesh;
        Transform transform = meshFilter.transform;
        Vector3 localVertex = transform.InverseTransformPoint(vertex);

        // Check if vertex is close to any mesh vertex
        foreach (Vector3 meshVertex in mesh.vertices)
        {
            if (Vector3.Distance(localVertex, meshVertex) <= tolerance)
            {
                logger.LogDebug("Vertex found on surface");
                logger.LogMethodExit(nameof(IsVertexOnSurface), "true");
                return true;
            }
        }

        logger.LogDebug("Vertex not on surface");
        logger.LogMethodExit(nameof(IsVertexOnSurface), "false");
        return false;
    }
}