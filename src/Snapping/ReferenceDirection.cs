using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VertexSnapper.Snapping;

/// <summary>
///     A face fixes two axes and leaves the turn around its own normal open, which is why aligning
///     something used to take two snaps. This picks the third axis.
///
///     Where it comes from depends on what the geometry offers. A flat face - a cube side, a ramp,
///     a platform - has outline edges, and the one nearest the mouse wins. A curved face is a lone
///     triangle whose edges mean nothing, so the block's own axes are laid onto the face instead: a
///     loop still has a direction of travel, and that is what one wants to line up with.
/// </summary>
public abstract class ReferenceDirection
{
	private const float MinimumProjection = 0.1f;

	public static Vector3 For(MeshFace face, Transform meshTransform, Transform blockTransform, Vector3 hitPoint)
	{
		Vector3 normal = meshTransform.TransformDirection(face.Normal).normalized;

		if (face.OutlineEdges.Count > 0)
		{
			return NearestOutline(face, meshTransform, hitPoint);
		}

		return NearestBlockAxis(blockTransform, normal, hitPoint, meshTransform.TransformPoint(face.Center));
	}

	private static Vector3 NearestOutline(MeshFace face, Transform meshTransform, Vector3 hitPoint)
	{
		Vector3[] nearest = face.OutlineEdges
			.OrderBy(edge => DistanceToEdge(
				meshTransform.TransformPoint(edge[0]),
				meshTransform.TransformPoint(edge[1]),
				hitPoint))
			.First();

		return (meshTransform.TransformPoint(nearest[1]) - meshTransform.TransformPoint(nearest[0])).normalized;
	}

	private static float DistanceToEdge(Vector3 from, Vector3 to, Vector3 point)
	{
		Vector3 along = to - from;
		float travel = Mathf.Clamp01(Vector3.Dot(point - from, along) / along.sqrMagnitude);
		return (from + along * travel - point).sqrMagnitude;
	}

	private static Vector3 NearestBlockAxis(Transform blockTransform, Vector3 normal, Vector3 hitPoint, Vector3 center)
	{
		Vector3 aim = Vector3.ProjectOnPlane(hitPoint - center, normal);
		List<Vector3> axes = UsableAxes(blockTransform, normal);

		if (axes.Count == 0)
		{
			return Vector3.zero;
		}

		if (aim.sqrMagnitude < Mathf.Epsilon)
		{
			return axes[0];
		}

		return axes.OrderByDescending(axis => Mathf.Abs(Vector3.Dot(axis, aim.normalized))).First();
	}

	/// <summary>
	///     An axis that stands almost upright on the face leaves nothing behind when projected onto
	///     it, and normalizing that leftover would turn rounding noise into a direction.
	/// </summary>
	private static List<Vector3> UsableAxes(Transform blockTransform, Vector3 normal)
	{
		return new[] { blockTransform.forward, blockTransform.right, blockTransform.up }
			.Select(axis => Vector3.ProjectOnPlane(axis, normal))
			.Where(projected => projected.magnitude > MinimumProjection)
			.Select(projected => projected.normalized)
			.ToList();
	}
}
