using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VertexSnapper.Snapping;

/// <summary>
///     Splits a mesh into flat faces once and remembers the result. Zeepkist reuses a handful of
///     block meshes across a whole level, so this runs a few times per session rather than once per
///     frame per block.
///
///     Two triangles that meet along an edge and face the same way are one flat surface here, so a
///     cube side comes back whole instead of as the two triangles it is stored as. The edge between
///     them lies inside that surface and is never offered as an outline.
/// </summary>
public abstract class MeshFaceCache
{
	private const float CoplanarTolerance = 0.9998f;
	private const float WeldGrid = 1000f;

	private static readonly Dictionary<Mesh, List<MeshFace>> Cache = new();

	private static Vector3[] _vertices;
	private static int[] _triangles;
	private static int[] _welded;
	private static Vector3[] _normals;
	private static Dictionary<long, List<int>> _trianglesPerEdge;
	private static int[] _faceOfTriangle;

	public static List<MeshFace> FacesOf(Mesh mesh)
	{
		if (Cache.TryGetValue(mesh, out List<MeshFace> cached))
		{
			return cached;
		}

		List<MeshFace> faces = Build(mesh);
		Cache[mesh] = faces;
		return faces;
	}

	private static List<MeshFace> Build(Mesh mesh)
	{
		_vertices = mesh.vertices;
		_triangles = mesh.triangles;
		_welded = WeldVertices();
		_normals = TriangleNormals();
		_trianglesPerEdge = MapEdges();
		_faceOfTriangle = GroupTriangles();

		return Enumerable.Range(0, _faceOfTriangle.Max() + 1)
			.Select(BuildFace)
			.Where(face => face != null)
			.ToList();
	}

	/// <summary>
	///     A corner where the texture or the shading changes is stored as several vertices at the same
	///     spot. Without merging them by position every triangle would look like an island and no face
	///     would ever grow past one triangle.
	/// </summary>
	private static int[] WeldVertices()
	{
		Dictionary<Vector3, int> canonical = new();
		int[] welded = new int[_vertices.Length];

		for (int i = 0; i < _vertices.Length; i++)
		{
			Vector3 key = new(
				Mathf.Round(_vertices[i].x * WeldGrid),
				Mathf.Round(_vertices[i].y * WeldGrid),
				Mathf.Round(_vertices[i].z * WeldGrid));

			if (!canonical.ContainsKey(key))
			{
				canonical[key] = i;
			}

			welded[i] = canonical[key];
		}

		return welded;
	}

	private static Vector3[] TriangleNormals()
	{
		Vector3[] normals = new Vector3[_triangles.Length / 3];

		for (int triangle = 0; triangle < normals.Length; triangle++)
		{
			Vector3 first = _vertices[_triangles[triangle * 3]];
			Vector3 second = _vertices[_triangles[triangle * 3 + 1]];
			Vector3 third = _vertices[_triangles[triangle * 3 + 2]];
			normals[triangle] = Vector3.Cross(second - first, third - first).normalized;
		}

		return normals;
	}

	private static Dictionary<long, List<int>> MapEdges()
	{
		Dictionary<long, List<int>> map = new();

		for (int triangle = 0; triangle < _normals.Length; triangle++)
		{
			foreach (long edge in EdgesOf(triangle))
			{
				if (!map.TryGetValue(edge, out List<int> neighbours))
				{
					neighbours = [];
					map[edge] = neighbours;
				}

				neighbours.Add(triangle);
			}
		}

		return map;
	}

	private static IEnumerable<long> EdgesOf(int triangle)
	{
		int first = _welded[_triangles[triangle * 3]];
		int second = _welded[_triangles[triangle * 3 + 1]];
		int third = _welded[_triangles[triangle * 3 + 2]];

		yield return EdgeKey(first, second);
		yield return EdgeKey(second, third);
		yield return EdgeKey(third, first);
	}

	private static long EdgeKey(int from, int to)
	{
		return Mathf.Min(from, to) * 1000000L + Mathf.Max(from, to);
	}

	/// <summary>
	///     Flood fills across shared edges for as long as the neighbour faces the same way, so every
	///     triangle ends up labelled with the flat surface it belongs to.
	/// </summary>
	private static int[] GroupTriangles()
	{
		int[] faceOfTriangle = Enumerable.Repeat(-1, _normals.Length).ToArray();
		int nextFace = 0;

		for (int triangle = 0; triangle < faceOfTriangle.Length; triangle++)
		{
			if (faceOfTriangle[triangle] != -1)
			{
				continue;
			}

			Spread(triangle, nextFace, faceOfTriangle);
			nextFace++;
		}

		return faceOfTriangle;
	}

	private static void Spread(int start, int face, int[] faceOfTriangle)
	{
		Queue<int> pending = new();
		pending.Enqueue(start);
		faceOfTriangle[start] = face;

		while (pending.Count > 0)
		{
			int triangle = pending.Dequeue();
			foreach (int neighbour in CoplanarNeighbours(triangle, faceOfTriangle))
			{
				faceOfTriangle[neighbour] = face;
				pending.Enqueue(neighbour);
			}
		}
	}

	private static IEnumerable<int> CoplanarNeighbours(int triangle, int[] faceOfTriangle)
	{
		return EdgesOf(triangle)
			.SelectMany(edge => _trianglesPerEdge[edge])
			.Where(neighbour => faceOfTriangle[neighbour] == -1)
			.Where(neighbour => Vector3.Dot(_normals[triangle], _normals[neighbour]) >= CoplanarTolerance)
			.Distinct()
			.ToList();
	}

	private static MeshFace BuildFace(int face)
	{
		List<int> members = Enumerable.Range(0, _faceOfTriangle.Length)
			.Where(triangle => _faceOfTriangle[triangle] == face)
			.ToList();

		if (members.Count == 0)
		{
			return null;
		}

		List<Vector3[]> corners = members.Select(CornersOf).ToList();

		return new MeshFace(
			_normals[members[0]],
			corners.SelectMany(triangle => triangle).Aggregate(Vector3.zero, (sum, corner) => sum + corner) /
			(corners.Count * 3),
			corners,
			OutlineOf(members));
	}

	private static Vector3[] CornersOf(int triangle)
	{
		return
		[
			_vertices[_triangles[triangle * 3]],
			_vertices[_triangles[triangle * 3 + 1]],
			_vertices[_triangles[triangle * 3 + 2]]
		];
	}

	/// <summary>
	///     An edge is on the outline when the triangle on its other side belongs to a different face,
	///     or when there is none at all. A face made of a single triangle gets no outline: its three
	///     edges qualify by that rule but mean nothing on curved geometry, where every triangle stands
	///     alone.
	/// </summary>
	private static List<Vector3[]> OutlineOf(List<int> members)
	{
		if (members.Count < 2)
		{
			return [];
		}

		int face = _faceOfTriangle[members[0]];

		return members
			.SelectMany(triangle => CornerPairsOf(triangle)
				.Where(pair => IsOutline(pair.Key, face))
				.Select(pair => pair.Value))
			.ToList();
	}

	private static bool IsOutline(long edge, int face)
	{
		List<int> neighbours = _trianglesPerEdge[edge];
		if (neighbours.Count < 2)
		{
			return true;
		}

		return neighbours.Any(triangle => _faceOfTriangle[triangle] != face);
	}

	private static IEnumerable<KeyValuePair<long, Vector3[]>> CornerPairsOf(int triangle)
	{
		Vector3[] corners = CornersOf(triangle);
		int first = _welded[_triangles[triangle * 3]];
		int second = _welded[_triangles[triangle * 3 + 1]];
		int third = _welded[_triangles[triangle * 3 + 2]];

		yield return new KeyValuePair<long, Vector3[]>(EdgeKey(first, second), [corners[0], corners[1]]);
		yield return new KeyValuePair<long, Vector3[]>(EdgeKey(second, third), [corners[1], corners[2]]);
		yield return new KeyValuePair<long, Vector3[]>(EdgeKey(third, first), [corners[2], corners[0]]);
	}
}
