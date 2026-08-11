using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using VertexSnapper.Snapping;

namespace VertexSnapper.Helper;

public abstract class CursorFactory
{
	private const float EdgeThickness = 0.12f;
	private const float ReferenceThickness = 0.2f;
	private const float FaceLift = 0.01f;
	private const string ReferenceName = "ReferenceBar";

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
			ShapeAsFace(cursor, target, scale);
			return;
		}

		HideReference(cursor);

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
		float length = (target.Corners[0][1] - target.Corners[0][0]).magnitude;
		cursor.transform.rotation = Quaternion.LookRotation(target.Direction);
		cursor.transform.localScale = new Vector3(scale * EdgeThickness, scale * EdgeThickness, length);
	}

	/// <summary>
	///     Replaces the cube with the surface itself, so the highlight covers exactly what will be
	///     snapped - a whole cube side rather than the half of it that one triangle would cover. The
	///     corners are lifted a hair along the normal to keep the block's own surface from fighting
	///     the highlight for the same pixels, and every triangle is wound both ways so it stays
	///     visible from behind.
	/// </summary>
	private static void ShapeAsFace(GameObject cursor, SnapTarget target, float scale)
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

		ShowReference(cursor, target, scale);
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

	/// <summary>
	///     The bar marking which way the face will be turned. It is a child of the cursor so it is
	///     built once and thrown away with it, rather than created every frame the mouse moves.
	/// </summary>
	private static void ShowReference(GameObject cursor, SnapTarget target, float scale)
	{
		if (target.Reference == Vector3.zero)
		{
			HideReference(cursor);
			return;
		}

		Transform bar = ReferenceBar(cursor);
		bar.gameObject.SetActive(true);
		bar.position = target.Position + target.Direction * FaceLift;
		bar.rotation = Quaternion.LookRotation(target.Reference, target.Direction);
		bar.localScale = new Vector3(scale * ReferenceThickness, scale * ReferenceThickness, FaceReach(target));
	}

	private static float FaceReach(SnapTarget target)
	{
		return target.Corners
			.SelectMany(triangle => triangle)
			.Max(corner => Vector3.Project(corner - target.Position, target.Reference).magnitude) * 2f;
	}

	private static Transform ReferenceBar(GameObject cursor)
	{
		Transform existing = cursor.transform.Find(ReferenceName);
		if (existing)
		{
			return existing;
		}

		GameObject bar = CreateCursor(
			ReferenceName,
			MaterialFactory.CreateUnlitMaterial(Color.white),
			cursor);

		return bar.transform;
	}

	private static void HideReference(GameObject cursor)
	{
		Transform bar = cursor.transform.Find(ReferenceName);
		if (!bar)
		{
			return;
		}

		bar.gameObject.SetActive(false);
	}
}
