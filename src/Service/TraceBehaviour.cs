using UnityEngine;

namespace VertexSnapper.Service;

/// <summary>
///     Gives <see cref="TraceService" /> a frame tick, the same way TraceBehaviour does in Author
///     Time Hunting.
///     It runs in LateUpdate rather than Update so that the editor, the snapper states and the
///     gizmo have all had their say for this frame. Tracing in Update would report a gizmo origin
///     that something else overwrites a moment later - the very thing this is meant to catch.
/// </summary>
public class TraceBehaviour : MonoBehaviour
{
	private TraceService _owner;

	private void LateUpdate()
	{
		_owner?.Tick();
	}

	public void Bind(TraceService owner)
	{
		_owner = owner;
	}
}
