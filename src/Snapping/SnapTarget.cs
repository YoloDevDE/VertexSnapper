using UnityEngine;

namespace VertexSnapper.Snapping;

/// <summary>
///     A place on a block that a cursor can sit on. A point has no direction of its own, an edge
///     points along itself and a face points away from itself.
///
///     Corners holds the triangles to draw - one for an edge, several for a face that grew across
///     coplanar neighbours. Edges holds the directions that face can be turned along, which the user
///     picks from in a second step: a face alone leaves the turn around its own normal open.
/// </summary>
public class SnapTarget
{
	public SnapTarget(Vector3 position, Vector3 direction, Vector3[][] corners, Vector3[][] edges, Vector3 focus)
	{
		Position = position;
		Direction = direction;
		Corners = corners;
		Edges = edges;
		Focus = focus;
	}

	public Vector3 Position { get; }
	public Vector3 Direction { get; }
	public Vector3[][] Corners { get; }
	public Vector3[][] Edges { get; }

	/// <summary>
	///     Where this target competes from, which is not always where it sits. A cube side is anchored
	///     at its middle but has to be found from wherever the mouse touches it, otherwise a small
	///     face nearby would win against the large one directly under the cursor.
	/// </summary>
	public Vector3 Focus { get; }
}
