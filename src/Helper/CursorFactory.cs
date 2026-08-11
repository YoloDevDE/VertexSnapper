using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using VertexSnapper.Snapping;

namespace VertexSnapper.Helper;

public abstract class CursorFactory
{
	private const float EdgeThickness = 0.3f;
	private const float EdgeLength = 3f;
	private const float FaceWidth = 3f;
	private const float FaceThickness = 0.15f;

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
		if (mode == SnapMode.Point || target.Direction == Vector3.zero)
		{
			cursor.transform.rotation = Quaternion.identity;
			cursor.transform.localScale = Vector3.one * scale;
			return;
		}

		cursor.transform.rotation = Quaternion.LookRotation(target.Direction);
		cursor.transform.localScale = ScaleFor(mode, scale);
	}

	private static Vector3 ScaleFor(SnapMode mode, float scale)
	{
		if (mode == SnapMode.Edge)
		{
			return new Vector3(scale * EdgeThickness, scale * EdgeThickness, scale * EdgeLength);
		}

		return new Vector3(scale * FaceWidth, scale * FaceWidth, scale * FaceThickness);
	}
}
