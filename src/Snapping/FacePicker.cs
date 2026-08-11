using System.Collections.Generic;
using System.Linq;
using FMODSyntax;
using UnityEngine;
using VertexSnapper.Helper;

namespace VertexSnapper.Snapping;

/// <summary>
///     Once an edge is picked, the block can still fold away from it in either direction. The faces
///     meeting at that edge are the two answers, and this highlights whichever one the mouse leans
///     towards.
/// </summary>
public abstract class FacePicker
{
	public static SnapTarget Update(VertexSnapper vertexSnapper, SnapTarget edge, GameObject cursor)
	{
		List<SnapTarget> faces = SnapTargetFinder.FacesAt(edge);
		if (faces.Count == 0)
		{
			return null;
		}

		SnapTarget chosen = Nearest(faces, MouseAt(vertexSnapper, edge));
		Show(cursor, chosen);
		return chosen;
	}

	/// <summary>
	///     The mouse is a line into the scene, not a point in it, so it is sampled at the depth of the
	///     edge that was just picked. Casting again would happily report a block standing in front.
	/// </summary>
	private static Vector3 MouseAt(VertexSnapper vertexSnapper, SnapTarget edge)
	{
		Ray ray = vertexSnapper.MainCamera.ScreenPointToRay(Input.mousePosition);
		return ray.origin + ray.direction * Vector3.Distance(ray.origin, edge.Position);
	}

	private static SnapTarget Nearest(List<SnapTarget> faces, Vector3 aim)
	{
		return faces.OrderBy(face => (face.Position - aim).sqrMagnitude).First();
	}

	private static void Show(GameObject cursor, SnapTarget face)
	{
		if (cursor.transform.position != face.Position)
		{
			AudioEvents.MenuHover1.PlayIfEnabled();
		}

		cursor.transform.position = face.Position;
		CursorFactory.ShapeCursor(cursor, face, 1f);
	}
}
