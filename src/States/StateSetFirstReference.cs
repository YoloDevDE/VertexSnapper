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
///     The edge is the hinge; this is where the user says at which angle the door hangs on it, by
///     picking one of the faces that meet at that edge.
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

		MessengerApi.Log("[Vertexsnapper] Now pick the face that hangs off this edge.", 2f);
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
		VertexSnapper.FirstFace = FacePicker.Update(VertexSnapper, VertexSnapper.FirstTarget, Cursor());
	}

	private GameObject Cursor()
	{
		if (!VertexSnapper.ReferenceCursor)
		{
			VertexSnapper.ReferenceCursor = CursorFactory.CreateCursor(
				"ReferenceCursor",
				MaterialFactory.CreateUnlitMaterial(Color.white),
				VertexSnapper.gameObject);
		}

		return VertexSnapper.ReferenceCursor;
	}

	private void Confirm()
	{
		if (VertexSnapper.FirstFace == null)
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
