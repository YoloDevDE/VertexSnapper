using UnityEngine;

namespace VertexSnapper.Snapping;

/// <summary>
///     A place on a block that a cursor can sit on. A point has no direction of its own, an edge
///     points along itself and a face points away from itself - which is all the snap needs to know
///     to line two blocks up.
/// </summary>
public class SnapTarget
{
	public SnapTarget(Vector3 position, Vector3 direction)
	{
		Position = position;
		Direction = direction;
	}

	public Vector3 Position { get; }
	public Vector3 Direction { get; }
}
