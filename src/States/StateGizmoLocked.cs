using UnityEngine;
using VertexSnapper.Managers;
using ZeepSDK.Messaging;

namespace VertexSnapper.States;

public class StateGizmoLocked : IVertexSnapperState<VertexSnapper>
{
	private readonly Vector3 _lockedPosition;

	public StateGizmoLocked(Vector3 lockedPosition)
	{
		_lockedPosition = lockedPosition;
	}

	public VertexSnapper VertexSnapper { get; set; }

	public void Enter()
	{
		KeyInputManager.OnKeyDown[VertexSnapperConfigManager.GizmoKeyBind.Value] += ChangeStateToIdle;

		MessengerApi.Log(
			$"[Vertexsnapper] Gizmo locked to vertex. Press <#f00>[{VertexSnapperConfigManager.GizmoKeyBind.Value}]</color> to release.",
			3f
		);
	}

	public void Exit()
	{
		KeyInputManager.OnKeyDown[VertexSnapperConfigManager.GizmoKeyBind.Value] -= ChangeStateToIdle;
	}

	public void Update()
	{
		// Gizmo-Position jeden Frame erzwingen, egal was das Spiel macht
		VertexSnapper.LevelEditorCentral.gizmos.motherOrigin = _lockedPosition;
		VertexSnapper.LevelEditorCentral.gizmos.SetMotherPosition(_lockedPosition);
	}

	private void ChangeStateToIdle()
	{
		VertexSnapper.ChangeState(new StateIdle());
	}
}
