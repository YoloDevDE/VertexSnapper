using System.Collections.Generic;
using UnityEngine;

namespace VertexSnapper.Snapping;

/// <summary>
///     One flat surface of a mesh, in local coordinates. A cube side is one face here even though
///     the mesh stores it as two triangles - which is what makes the highlight cover the whole side
///     and keeps the diagonal between those two triangles out of the outline.
/// </summary>
public class MeshFace
{
	public MeshFace(Vector3 normal, Vector3 center, List<Vector3[]> triangles, List<Vector3[]> outlineEdges)
	{
		Normal = normal;
		Center = center;
		Triangles = triangles;
		OutlineEdges = outlineEdges;
	}

	public Vector3 Normal { get; }
	public Vector3 Center { get; }
	public List<Vector3[]> Triangles { get; }
	public List<Vector3[]> OutlineEdges { get; }
}
