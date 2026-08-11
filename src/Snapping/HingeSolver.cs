using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VertexSnapper.Snapping;

/// <summary>
///     Two edges laid together are a hinge, and the selection can still swing around it. Rather than
///     asking which face should come to rest against which, the angle is worked out: the faces
///     meeting at each edge are the only positions where the blocks lie flush, so every pairing of
///     them is a candidate, and the ones that would drive the selection into the target are dropped.
///
///     Of what remains the smallest turn wins - the block ends up where it was already closest to,
///     which is the answer that surprises nobody.
/// </summary>
public abstract class HingeSolver
{
	public static Quaternion? Solve(SnapTarget sourceEdge, SnapTarget targetEdge)
	{
		List<SnapTarget> sourceFaces = SnapTargetFinder.FacesAt(sourceEdge);
		List<SnapTarget> targetFaces = SnapTargetFinder.FacesAt(targetEdge);

		if (sourceFaces.Count == 0 || targetFaces.Count == 0)
		{
			return null;
		}

		List<Quaternion> candidates = sourceFaces
			.SelectMany(sourceFace => targetFaces.Select(targetFace => Pairing(sourceEdge, sourceFace, targetEdge, targetFace)))
			.Where(candidate => candidate.HasValue)
			.Select(candidate => candidate.Value)
			.Where(candidate => LeavesTheTargetAlone(candidate, sourceEdge, targetEdge, targetFaces))
			.ToList();

		if (candidates.Count == 0)
		{
			return null;
		}

		return candidates.OrderBy(candidate => Quaternion.Angle(Quaternion.identity, candidate)).First();
	}

	/// <summary>
	///     The turn that brings one particular pair of faces together: lying on each other, facing
	///     into each other, both reaching the same way from the hinge.
	/// </summary>
	private static Quaternion? Pairing(
		SnapTarget sourceEdge,
		SnapTarget sourceFace,
		SnapTarget targetEdge,
		SnapTarget targetFace)
	{
		Vector3 sourceOutward = AwayFromEdge(sourceEdge, sourceFace);
		Vector3 targetOutward = AwayFromEdge(targetEdge, targetFace);

		if (sourceOutward == Vector3.zero || targetOutward == Vector3.zero)
		{
			return null;
		}

		return Quaternion.LookRotation(-targetFace.Direction, targetOutward) *
		       Quaternion.Inverse(Quaternion.LookRotation(sourceFace.Direction, sourceOutward));
	}

	/// <summary>
	///     A pairing is only worth having if the selection comes to rest outside the target block. Two
	///     of the four pairings on a solid corner fold the wrong way and would bury one block in the
	///     other, and comparing which side of the target surface each block sits on catches exactly
	///     those.
	/// </summary>
	private static bool LeavesTheTargetAlone(
		Quaternion candidate,
		SnapTarget sourceEdge,
		SnapTarget targetEdge,
		List<SnapTarget> targetFaces)
	{
		Vector3 landed = targetEdge.Position + candidate * (CenterOf(sourceEdge) - sourceEdge.Position);
		return targetFaces.All(face => OnOppositeSides(face, landed, CenterOf(targetEdge)));
	}

	private static bool OnOppositeSides(SnapTarget face, Vector3 landed, Vector3 targetCenter)
	{
		float landedSide = Vector3.Dot(landed - face.Position, face.Direction);
		float targetSide = Vector3.Dot(targetCenter - face.Position, face.Direction);
		return landedSide * targetSide <= 0f;
	}

	private static Vector3 CenterOf(SnapTarget edge)
	{
		return edge.Source.transform.TransformPoint(edge.Source.sharedMesh.bounds.center);
	}

	private static Vector3 AwayFromEdge(SnapTarget edge, SnapTarget face)
	{
		Vector3 outward = Vector3.ProjectOnPlane(face.Position - edge.Position, face.Direction);
		if (outward.sqrMagnitude < Mathf.Epsilon)
		{
			return Vector3.zero;
		}

		return outward.normalized;
	}
}
