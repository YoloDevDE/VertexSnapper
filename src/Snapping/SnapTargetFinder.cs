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
			.SelectMany(meshFilter => CandidatesOf(meshFilter, block.transform, hit.point, mode));

		return Closest(candidates, hit.point);
	}

	private static IEnumerable<SnapTarget> CandidatesOf(
		MeshFilter meshFilter,
		Transform blockTransform,
		Vector3 hitPoint,
		SnapMode mode)
	{
		if (mode == SnapMode.Point)
		{
			return PointsOf(meshFilter);
		}

		if (mode == SnapMode.Edge)
		{
			return EdgesOf(meshFilter);
		}

		return FacesOf(meshFilter, blockTransform, hitPoint);
	}

	private static SnapTarget Closest(IEnumerable<SnapTarget> candidates, Vector3 hitPoint)
	{
		SnapTarget closest = null;
		float shortestDistance = float.MaxValue;

		foreach (SnapTarget candidate in candidates)
		{
			float currentDistance = (hitPoint - candidate.Focus).sqrMagnitude;
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
			.Select(vertex => meshTransform.TransformPoint(vertex))
			.Select(vertex => new SnapTarget(vertex, Vector3.zero, null, Vector3.zero, vertex));
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

	/// <summary>
	///     One target per flat surface, not per triangle, so a cube side is offered whole and the
	///     seam running across it never shows up as an edge of its own.
	/// </summary>
	private static IEnumerable<SnapTarget> FacesOf(MeshFilter meshFilter, Transform blockTransform, Vector3 hitPoint)
	{
		Transform meshTransform = meshFilter.transform;

		return MeshFaceCache.FacesOf(meshFilter.sharedMesh)
			.Select(face => Face(face, meshTransform, blockTransform, hitPoint));
	}

	private static SnapTarget Face(MeshFace face, Transform meshTransform, Transform blockTransform, Vector3 hitPoint)
	{
		Vector3[][] corners = face.Triangles
			.Select(triangle => triangle.Select(meshTransform.TransformPoint).ToArray())
			.ToArray();

		return new SnapTarget(
			meshTransform.TransformPoint(face.Center),
			meshTransform.TransformDirection(face.Normal).normalized,
			corners,
			ReferenceDirection.For(face, meshTransform, blockTransform, hitPoint),
			NearestCorner(corners, hitPoint));
	}

	private static Vector3 NearestCorner(Vector3[][] corners, Vector3 hitPoint)
	{
		return corners
			.SelectMany(triangle => triangle)
			.OrderBy(corner => (corner - hitPoint).sqrMagnitude)
			.First();
	}

	private static SnapTarget Edge(Vector3 from, Vector3 to)
	{
		Vector3 middle = (from + to) * 0.5f;
		return new SnapTarget(middle, (to - from).normalized, [[from, to]], Vector3.zero, middle);
	}
}
