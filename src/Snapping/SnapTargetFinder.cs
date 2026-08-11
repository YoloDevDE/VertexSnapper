using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VertexSnapper.Snapping;

/// <summary>
///     Reads the candidates straight out of the mesh rather than off the RaycastHit, because the
///     snapper also accepts sphere cast and box collider hits, where hit.triangleIndex is -1.
/// </summary>
public abstract class SnapTargetFinder
{
	public static SnapTarget Find(RaycastHit hit, SnapMode mode)
	{
		BlockProperties block = hit.collider.gameObject.GetComponentInParent<BlockProperties>();
		if (!block)
		{
			return null;
		}

		IEnumerable<SnapTarget> candidates = block.GetComponentsInChildren<MeshFilter>()
			.Where(meshFilter => meshFilter && meshFilter.sharedMesh)
			.SelectMany(meshFilter => CandidatesOf(meshFilter, mode));

		return Closest(candidates, hit.point);
	}

	private static IEnumerable<SnapTarget> CandidatesOf(MeshFilter meshFilter, SnapMode mode)
	{
		if (mode == SnapMode.Point)
		{
			return PointsOf(meshFilter);
		}

		if (mode == SnapMode.Edge)
		{
			return EdgesOf(meshFilter);
		}

		return FacesOf(meshFilter);
	}

	private static SnapTarget Closest(IEnumerable<SnapTarget> candidates, Vector3 hitPoint)
	{
		SnapTarget closest = null;
		float shortestDistance = float.MaxValue;

		foreach (SnapTarget candidate in candidates)
		{
			float currentDistance = (hitPoint - candidate.Position).sqrMagnitude;
			if (currentDistance > shortestDistance)
			{
				continue;
			}

			shortestDistance = currentDistance;
			closest = candidate;
		}

		return closest;
	}

	private static IEnumerable<SnapTarget> PointsOf(MeshFilter meshFilter)
	{
		Transform meshTransform = meshFilter.transform;
		return meshFilter.sharedMesh.vertices
			.Select(vertex => new SnapTarget(meshTransform.TransformPoint(vertex), Vector3.zero, null));
	}

	private static IEnumerable<SnapTarget> EdgesOf(MeshFilter meshFilter)
	{
		Vector3[] vertices = meshFilter.sharedMesh.vertices;
		int[] triangles = meshFilter.sharedMesh.triangles;
		Transform meshTransform = meshFilter.transform;

		for (int i = 0; i < triangles.Length; i += 3)
		{
			Vector3 first = meshTransform.TransformPoint(vertices[triangles[i]]);
			Vector3 second = meshTransform.TransformPoint(vertices[triangles[i + 1]]);
			Vector3 third = meshTransform.TransformPoint(vertices[triangles[i + 2]]);

			yield return Edge(first, second);
			yield return Edge(second, third);
			yield return Edge(third, first);
		}
	}

	private static IEnumerable<SnapTarget> FacesOf(MeshFilter meshFilter)
	{
		Vector3[] vertices = meshFilter.sharedMesh.vertices;
		int[] triangles = meshFilter.sharedMesh.triangles;
		Transform meshTransform = meshFilter.transform;

		for (int i = 0; i < triangles.Length; i += 3)
		{
			Vector3 first = meshTransform.TransformPoint(vertices[triangles[i]]);
			Vector3 second = meshTransform.TransformPoint(vertices[triangles[i + 1]]);
			Vector3 third = meshTransform.TransformPoint(vertices[triangles[i + 2]]);

			yield return new SnapTarget(
				(first + second + third) / 3f,
				Vector3.Cross(second - first, third - first).normalized,
				[first, second, third]);
		}
	}

	private static SnapTarget Edge(Vector3 from, Vector3 to)
	{
		return new SnapTarget((from + to) * 0.5f, (to - from).normalized, [from, to]);
	}
}
