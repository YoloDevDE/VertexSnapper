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
	private const float SamePointTolerance = 0.0001f;

	public static SnapTarget Find(RaycastHit hit, SnapMode mode)
	{
		BlockProperties block = hit.collider.gameObject.GetComponentInParent<BlockProperties>();
		if (!block)
		{
			return null;
		}

		IEnumerable<SnapTarget> candidates = MeshFiltersOf(block)
			.SelectMany(meshFilter => CandidatesOf(meshFilter, mode));

		return Closest(candidates, hit.point);
	}

	/// <summary>
	///     The flat surfaces that meet at an edge - two on a solid block, one at an open border. An
	///     edge on its own leaves open which way the block folds away from it, and this is the choice
	///     the user is offered to settle that.
	/// </summary>
	public static List<SnapTarget> FacesAt(SnapTarget edge)
	{
		if (edge?.Source == null)
		{
			return [];
		}

		Transform meshTransform = edge.Source.transform;

		return MeshFaceCache.FacesOf(edge.Source.sharedMesh)
			.Select(face => Face(face, edge.Source, meshTransform, edge.Position))
			.Where(face => Touches(face, edge))
			.ToList();
	}

	/// <summary>
	///     The closest point on a segment, which is how the finder decides what the mouse is nearest
	///     to. Comparing against midpoints instead would let a short edge beat the long one the cursor
	///     is actually sitting on.
	/// </summary>
	public static Vector3 ClosestOnSegment(Vector3 from, Vector3 to, Vector3 point)
	{
		Vector3 along = to - from;
		if (along.sqrMagnitude < Mathf.Epsilon)
		{
			return from;
		}

		return from + along * Mathf.Clamp01(Vector3.Dot(point - from, along) / along.sqrMagnitude);
	}

	private static IEnumerable<MeshFilter> MeshFiltersOf(BlockProperties block)
	{
		return block.GetComponentsInChildren<MeshFilter>().Where(meshFilter => meshFilter && meshFilter.sharedMesh);
	}

	private static bool Touches(SnapTarget face, SnapTarget edge)
	{
		return face.Corners
			.SelectMany(triangle => triangle)
			.Count(corner => (corner - edge.Corners[0][0]).sqrMagnitude < SamePointTolerance ||
			                 (corner - edge.Corners[0][1]).sqrMagnitude < SamePointTolerance) >= 2;
	}

	private static IEnumerable<SnapTarget> CandidatesOf(MeshFilter meshFilter, SnapMode mode)
	{
		if (mode == SnapMode.Point)
		{
			return PointsOf(meshFilter);
		}

		return EdgesOf(meshFilter);
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
			.Select(meshTransform.TransformPoint)
			.Select(vertex => new SnapTarget(vertex, Vector3.zero, null, vertex, meshFilter));
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

			yield return Edge(first, second, meshFilter);
			yield return Edge(second, third, meshFilter);
			yield return Edge(third, first, meshFilter);
		}
	}

	private static SnapTarget Face(MeshFace face, MeshFilter source, Transform meshTransform, Vector3 hitPoint)
	{
		Vector3[][] corners = face.Triangles
			.Select(triangle => triangle.Select(meshTransform.TransformPoint).ToArray())
			.ToArray();

		return new SnapTarget(
			meshTransform.TransformPoint(face.Center),
			meshTransform.TransformDirection(face.Normal).normalized,
			corners,
			NearestPointOn(corners, hitPoint),
			source);
	}

	private static Vector3 NearestPointOn(Vector3[][] corners, Vector3 hitPoint)
	{
		return corners
			.SelectMany(triangle => new[]
			{
				ClosestOnSegment(triangle[0], triangle[1], hitPoint),
				ClosestOnSegment(triangle[1], triangle[2], hitPoint),
				ClosestOnSegment(triangle[2], triangle[0], hitPoint)
			})
			.OrderBy(point => (point - hitPoint).sqrMagnitude)
			.First();
	}

	private static SnapTarget Edge(Vector3 from, Vector3 to, MeshFilter source)
	{
		Vector3 middle = (from + to) * 0.5f;
		return new SnapTarget(middle, (to - from).normalized, [[from, to]], middle, source);
	}
}
