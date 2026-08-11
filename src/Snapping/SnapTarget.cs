using UnityEngine;

namespace VertexSnapper.Snapping;

/// <summary>
///     A place on a block that a cursor can sit on. A point has no direction of its own, an edge
///     points along itself and a face points away from itself.
///
///     A face also carries a Reference: a direction lying in the face that fixes the turn around the
///     normal. Normal and reference together are a full coordinate system, which is what lets one
///     click align all three axes instead of two.
///
///     Corners holds the triangles to draw - one for an edge or a single triangle, several for a
///     face that grew across coplanar neighbours.
/// </summary>
public class SnapTarget
{
	public SnapTarget(Vector3 position, Vector3 direction, Vector3[][] corners, Vector3 reference, Vector3 focus)
	{
		Position = position;
		Direction = direction;
		Corners = corners;
		Reference = reference;
		Focus = focus;
	}

	public Vector3 Position { get; }

	/// <summary>
	///     Where this target is competing from, which is not always where it sits. A cube side is
	///     anchored at its middle but has to be found from wherever the mouse touches it, otherwise a
	///     small face nearby would win against the large one directly under the cursor.
	/// </summary>
	public Vector3 Focus { get; }
	public Vector3 Direction { get; }
	public Vector3[][] Corners { get; }
	public Vector3 Reference { get; }
}
