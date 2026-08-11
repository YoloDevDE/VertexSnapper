using UnityEngine;

namespace VertexSnapper.Snapping;

/// <summary>
///     A place on a block that a cursor can sit on. A point has no direction of its own, an edge
///     points along itself and a face points away from itself.
///
///     Corners holds the triangles to draw - one for an edge, several for a face that grew across
///     coplanar neighbours.
/// </summary>
public class SnapTarget
{
	public SnapTarget(
		Vector3 position,
		Vector3 direction,
		Vector3[][] corners,
		Vector3 focus,
		MeshFilter source)
	{
		Position = position;
		Direction = direction;
		Corners = corners;
		Focus = focus;
		Source = source;
	}

	public Vector3 Position { get; }
	public Vector3 Direction { get; }
	public Vector3[][] Corners { get; }

	/// <summary>
	///     The mesh this came out of. An edge alone does not say which way a block folds away from it,
	///     so the faces meeting at that edge have to be looked up afterwards, and that needs the mesh
	///     back again.
	/// </summary>
	public MeshFilter Source { get; }

	/// <summary>
	///     Where this target competes from, which is not always where it sits. A cube side is anchored
	///     at its middle but has to be found from wherever the mouse touches it, otherwise a small
	///     face nearby would win against the large one directly under the cursor.
	/// </summary>
	public Vector3 Focus { get; }
}
