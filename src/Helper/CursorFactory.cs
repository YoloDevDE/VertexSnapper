using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using VertexSnapper.Snapping;

namespace VertexSnapper.Helper;

public abstract class CursorFactory
{
	private const float EdgeThickness = 0.12f;
	private const float FaceLift = 0.01f;

	public static GameObject CreateCursor(
		string cursorName,
		[NotNull] Material material,
		GameObject parent = null,
		float scale = 0.5f)
	{
		GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
		Renderer renderer = go.GetComponent<Renderer>();

		go.transform.localScale = Vector3.one * scale;

		renderer.shadowCastingMode = ShadowCastingMode.Off;
		renderer.receiveShadows = false;
		renderer.sharedMaterial = material;

		go.name = cursorName;
		go.layer = 2;
		if (parent)
		{
			go.transform.SetParent(parent.transform);
		}

		return go;
	}

	/// <summary>
	///     Makes the cube read as what it sits on: a bar along an edge, the surface itself for a
	///     face, a cube on a point. Without this all three modes look identical and nobody can tell
	///     which one is on.
	/// </summary>
	public static void ShapeCursor(GameObject cursor, SnapTarget target, SnapMode mode, float scale)
	{
		if (mode == SnapMode.Face && target.Corners != null)
		{
			ShapeAsFace(cursor, target);
			return;
		}

		if (mode == SnapMode.Edge && target.Corners != null)
		{
			ShapeAsBar(cursor, target, scale);
			return;
		}

		cursor.transform.rotation = Quaternion.identity;
		cursor.transform.localScale = Vector3.one * scale;
	}

	/// <summary>
	///     Stretches the cube to cover the hit edge end to end, so the highlight shows exactly which
	///     edge was found rather than a bar of arbitrary length pointing the right way.
	/// </summary>
	private static void ShapeAsBar(GameObject cursor, SnapTarget target, float scale)
	{
		ShapeAsEdge(cursor, target.Corners[0][0], target.Corners[0][1], scale * EdgeThickness);
	}

	/// <summary>
	///     Lays the cube along a single edge, end to end. Used both for the edge mode and for the bar
	///     that marks the reference edge of a face.
	/// </summary>
	public static void ShapeAsEdge(GameObject cursor, Vector3 from, Vector3 to, float thickness)
	{
		Vector3 along = to - from;
		cursor.transform.position = (from + to) * 0.5f;
		cursor.transform.rotation = Quaternion.LookRotation(along);
		cursor.transform.localScale = new Vector3(thickness, thickness, along.magnitude);
	}

	/// <summary>
	///     Replaces the cube with the surface itself, so the highlight covers exactly what will be
	///     snapped - a whole cube side rather than the half of it that one triangle would cover. The
	///     corners are lifted a hair along the normal to keep the block's own surface from fighting
	///     the highlight for the same pixels, and every triangle is wound both ways so it stays
	///     visible from behind.
	/// </summary>
	private static void ShapeAsFace(GameObject cursor, SnapTarget target)
	{
		cursor.transform.rotation = Quaternion.identity;
		cursor.transform.localScale = Vector3.one;

		Vector3 lift = target.Direction * FaceLift;
		Mesh mesh = cursor.GetComponent<MeshFilter>().mesh;
		mesh.Clear();
		mesh.vertices = target.Corners
			.SelectMany(triangle => triangle)
			.Select(corner => corner - target.Position + lift)
			.ToArray();
		mesh.triangles = BothWindings(target.Corners.Length);
		mesh.RecalculateBounds();
	}

	private static int[] BothWindings(int triangleCount)
	{
		return Enumerable.Range(0, triangleCount)
			.SelectMany(triangle => new[]
			{
				triangle * 3, triangle * 3 + 1, triangle * 3 + 2,
				triangle * 3, triangle * 3 + 2, triangle * 3 + 1
			})
			.ToArray();
	}

}
