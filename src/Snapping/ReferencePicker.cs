using System.Linq;
using FMODSyntax;
using UnityEngine;
using VertexSnapper.Helper;

namespace VertexSnapper.Snapping;

/// <summary>
///     Highlights whichever edge of an already picked face the mouse is nearest, and hands that
///     direction back. Both reference steps work the same way and differ only in what they do with
///     the answer, so the hovering lives here rather than twice in the states.
/// </summary>
public abstract class ReferencePicker
{
	private const float BarThickness = 0.18f;

	public static Vector3[] Update(VertexSnapper vertexSnapper, SnapTarget target)
	{
		if (target?.Edges == null || target.Edges.Length == 0)
		{
			return null;
		}

		Vector3[] edge = NearestEdge(target, MouseRay(vertexSnapper));
		ShowBar(vertexSnapper, edge);
		return edge;
	}

	/// <summary>
	///     The mouse is a line into the scene, not a point in it, so the edge is measured against that
	///     line. Sampling the ray beats casting it: the face was already found, and a second cast
	///     would happily report a block in front of it.
	/// </summary>
	private static Vector3 MouseRay(VertexSnapper vertexSnapper)
	{
		Ray ray = vertexSnapper.MainCamera.ScreenPointToRay(Input.mousePosition);
		return ray.origin + ray.direction * Vector3.Distance(ray.origin, vertexSnapper.CurrentFacePosition());
	}

	private static Vector3[] NearestEdge(SnapTarget target, Vector3 aim)
	{
		return target.Edges
			.OrderBy(edge => (SnapTargetFinder.ClosestOnSegment(edge[0], edge[1], aim) - aim).sqrMagnitude)
			.First();
	}

	private static void ShowBar(VertexSnapper vertexSnapper, Vector3[] edge)
	{
		if (!vertexSnapper.ReferenceCursor)
		{
			vertexSnapper.ReferenceCursor = CursorFactory.CreateCursor(
				"ReferenceCursor",
				MaterialFactory.CreateUnlitMaterial(Color.white),
				vertexSnapper.gameObject);
		}

		Vector3 middle = (edge[0] + edge[1]) * 0.5f;
		if (vertexSnapper.ReferenceCursor.transform.position != middle)
		{
			AudioEvents.MenuHover1.PlayIfEnabled();
		}

		CursorFactory.ShapeAsEdge(vertexSnapper.ReferenceCursor, edge[0], edge[1], BarThickness);
	}
}
