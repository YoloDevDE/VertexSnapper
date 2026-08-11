using System.Linq;
using FMODSyntax;
using UnityEngine;
using VertexSnapper.Helper;
using VertexSnapper.Managers;
using VertexSnapper.Snapping;
using ZeepSDK.LevelEditor;
using ZeepSDK.Messaging;

namespace VertexSnapper.States;

/// <summary>
///     A face fixes two axes and leaves the turn around its own normal open. This is where the user
///     names the third: one of the edges of the face just picked. Guessing it from the mouse was
///     tried and felt arbitrary, so it is now a click of its own.
/// </summary>
public class StateSetFirstReference : IVertexSnapperState<VertexSnapper>
{
	public VertexSnapper VertexSnapper { get; set; }

	public void Enter()
	{
		KeyInputManager.OnKeyUp[VertexSnapperConfigManager.VertexKeyBind.Value] += ChangeStateToAbort;
		KeyInputManager.OnMouseDown[0] += Confirm;
		KeyInputManager.OnMouseDown[2] += ChangeStateToAbort;
		LevelEditorApi.BlockMouseInput(this);

		MessengerApi.Log("[Vertexsnapper] Now pick the edge to align along.", 2f);
	}

	public void Exit()
	{
		KeyInputManager.OnKeyUp[VertexSnapperConfigManager.VertexKeyBind.Value] -= ChangeStateToAbort;
		KeyInputManager.OnMouseDown[0] -= Confirm;
		KeyInputManager.OnMouseDown[2] -= ChangeStateToAbort;
		LevelEditorApi.UnblockMouseInput(this);
	}

	public void Update()
	{
		VertexSnapper.FirstReference = ReferencePicker.Update(VertexSnapper, VertexSnapper.FirstTarget);
	}

	private void Confirm()
	{
		if (VertexSnapper.FirstReference == Vector3.zero)
		{
			AudioEvents.Blarghl.PlayIfEnabled();
			return;
		}

		AudioEvents.MenuClick.PlayIfEnabled();
		VertexSnapper.ChangeState(new StateRoaming());
	}

	private void ChangeStateToAbort()
	{
		VertexSnapper.ChangeState(new StateAbort());
	}
}
