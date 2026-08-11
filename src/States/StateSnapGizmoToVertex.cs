using FMODSyntax;
using UnityEngine;
using VertexSnapper.Helper;
using VertexSnapper.Managers;
using ZeepSDK.LevelEditor;
using ZeepSDK.Messaging;

namespace VertexSnapper.States;

public class StateSnapGizmoToVertex : IVertexSnapperState<VertexSnapper>
{
	public VertexSnapper VertexSnapper { get; set; }

	public void Enter()
	{
		KeyInputManager.OnKeyUp[VertexSnapperConfigManager.GizmoKeyBind.Value] += ChangeStateToIdle;
		KeyInputManager.OnMouseDown[0] += TrySnapGizmo;
		KeyInputManager.OnMouseDown[2] += ChangeStateToIdle;

		LevelEditorApi.BlockMouseInput(this);
		LevelEditorApi.BlockKeyboardInput(this);

		MessengerApi.Log("[Vertexsnapper] Gizmo snap active – hover over a vertex and click.", 0.8f);
	}

	public void Exit()
	{
		KeyInputManager.OnKeyUp[VertexSnapperConfigManager.GizmoKeyBind.Value] -= ChangeStateToIdle;
		KeyInputManager.OnMouseDown[0] -= TrySnapGizmo;
		KeyInputManager.OnMouseDown[2] -= ChangeStateToIdle;

		VertexSnapper.SafeDestroy(VertexSnapper.FirstCursor);

		LevelEditorApi.UnblockMouseInput(this);
		LevelEditorApi.UnblockKeyboardInput(this);
	}

	public void Update()
	{
		if (RaycastUtils.IsSphereCastOnBlockSuccessful(VertexSnapper.MainCamera, out RaycastHit hit))
		{
			Vector3 closestVertex = VertexSnapper.FindClosestVertexToHit(hit);

			if (!VertexSnapper.FirstCursor)
			{
				VertexSnapper.FirstCursor = CursorFactory.CreateCursor(
					"GizmoCursor",
					MaterialFactory.CreateUnlitMaterial(Color.cyan),
					VertexSnapper.gameObject,
					VertexSnapper.CubeScaleFactor
				);
			}

			if (VertexSnapper.FirstCursor.transform.position != closestVertex)
			{
				AudioEvents.MenuHover1.PlayIfEnabled();
				VertexSnapper.FirstCursor.transform.position = closestVertex;
			}
		}
		else
		{
			VertexSnapper.SafeDestroy(VertexSnapper.FirstCursor);
		}
	}

	private void TrySnapGizmo()
	{
		if (!VertexSnapper.FirstCursor)
		{
			MessengerApi.LogWarning("[Vertexsnapper] No vertex hovered – move your cursor over a block first.", 4f);
			AudioEvents.Blarghl.PlayIfEnabled();
			return;
		}

		Vector3 targetVertex = VertexSnapper.FirstCursor.transform.position;

		MessengerApi.LogSuccess("[Vertexsnapper] Gizmo snapped to vertex!", 0.8f);
		AudioEvents.BlockPlace.PlayIfEnabled();

		ChangeStateToGizmoLocked(targetVertex);
	}

	private void ChangeStateToGizmoLocked(Vector3 lockedPosition)
	{
		VertexSnapper.ChangeState(new StateGizmoLocked(lockedPosition));
	}

	private void ChangeStateToIdle()
	{
		VertexSnapper.ChangeState(new StateIdle());
	}
}
