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
	///     Makes the cube read as what it sits on: a bar along an edge, a plate across a face, a cube
	///     on a point. Without this all three modes look identical and nobody can tell which one is on.
	/// </summary>
	public static void ShapeCursor(GameObject cursor, SnapTarget target, SnapMode mode, float scale)
	{
		if (mode == SnapMode.Face && target.Corners != null)
		{
			ShapeAsTriangle(cursor, target);
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
		float length = (target.Corners[1] - target.Corners[0]).magnitude;
		cursor.transform.rotation = Quaternion.LookRotation(target.Direction);
		cursor.transform.localScale = new Vector3(scale * EdgeThickness, scale * EdgeThickness, length);
	}

	/// <summary>
	///     Replaces the cube with the hit triangle itself, so the highlight covers exactly the face
	///     that will be snapped and nothing next to it. The corners are lifted a hair along the normal
	///     to keep the block's own surface from fighting the highlight for the same pixels, and the
	///     triangle is wound both ways so it stays visible from behind.
	/// </summary>
	private static void ShapeAsTriangle(GameObject cursor, SnapTarget target)
	{
		cursor.transform.rotation = Quaternion.identity;
		cursor.transform.localScale = Vector3.one;

		Vector3 lift = target.Direction * FaceLift;
		Mesh mesh = cursor.GetComponent<MeshFilter>().mesh;
		mesh.Clear();
		mesh.vertices = target.Corners.Select(corner => corner - target.Position + lift).ToArray();
		mesh.triangles = [0, 1, 2, 0, 2, 1];
		mesh.RecalculateBounds();
	}
}
