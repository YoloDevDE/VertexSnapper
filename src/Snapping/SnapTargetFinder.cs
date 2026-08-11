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
			.SelectMany(meshFilter => CandidatesOf(meshFilter, mode, hit.point));

		return Closest(candidates, hit.point);
	}

	/// <summary>
	///     The closest point on a segment, which is how both the finder and the reference pick decide
	///     what the mouse is nearest to. Comparing against midpoints instead would let a short edge
	///     beat the long one the cursor is actually sitting on.
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

	private static IEnumerable<SnapTarget> CandidatesOf(MeshFilter meshFilter, SnapMode mode, Vector3 hitPoint)
	{
		if (mode == SnapMode.Point)
		{
			return PointsOf(meshFilter);
		}

		if (mode == SnapMode.Edge)
		{
			return EdgesOf(meshFilter);
		}

		return FacesOf(meshFilter, hitPoint);
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
			.Select(vertex => new SnapTarget(vertex, Vector3.zero, null, null, vertex));
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
	///     One target per flat surface, not per triangle, so a cube side is offered whole and the seam
	///     running across it never shows up as an edge of its own.
	/// </summary>
	private static IEnumerable<SnapTarget> FacesOf(MeshFilter meshFilter, Vector3 hitPoint)
	{
		Transform meshTransform = meshFilter.transform;
		return MeshFaceCache.FacesOf(meshFilter.sharedMesh).Select(face => Face(face, meshTransform, hitPoint));
	}

	private static SnapTarget Face(MeshFace face, Transform meshTransform, Vector3 hitPoint)
	{
		Vector3[][] corners = face.Triangles
			.Select(triangle => triangle.Select(meshTransform.TransformPoint).ToArray())
			.ToArray();

		return new SnapTarget(
			meshTransform.TransformPoint(face.Center),
			meshTransform.TransformDirection(face.Normal).normalized,
			corners,
			ReferenceEdges(face, corners, meshTransform),
			NearestPointOn(corners, hitPoint));
	}

	/// <summary>
	///     A face has to be found from wherever the mouse touches it, not from its middle. Measuring
	///     against the middle would hand a large cube side to any small face that happens to sit
	///     closer to it.
	/// </summary>
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

	/// <summary>
	///     What the user can align the face along. A flat surface offers its outline, so the seam
	///     between two coplanar triangles is never on the list. A curved surface is a lone triangle
	///     with no outline worth the name, and there its own three edges are the honest choice.
	/// </summary>
	private static Vector3[][] ReferenceEdges(MeshFace face, Vector3[][] corners, Transform meshTransform)
	{
		if (face.OutlineEdges.Count > 0)
		{
			return face.OutlineEdges
				.Select(edge => edge.Select(meshTransform.TransformPoint).ToArray())
				.ToArray();
		}

		return
		[
			[corners[0][0], corners[0][1]],
			[corners[0][1], corners[0][2]],
			[corners[0][2], corners[0][0]]
		];
	}

	private static SnapTarget Edge(Vector3 from, Vector3 to)
	{
		Vector3 middle = (from + to) * 0.5f;
		return new SnapTarget(middle, (to - from).normalized, [[from, to]], null, middle);
	}
}
