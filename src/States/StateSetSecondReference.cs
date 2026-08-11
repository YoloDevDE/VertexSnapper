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
///     The last of the four picks: which edge of the target face the selection lines up with. The
///     hologram turns as the mouse moves between the candidates, so the result is visible before the
///     click rather than after it.
/// </summary>
public class StateSetSecondReference : IVertexSnapperState<VertexSnapper>
{
	public VertexSnapper VertexSnapper { get; set; }

	public void Enter()
	{
		Rebuild();

		KeyInputManager.OnMouseDown[0] += Confirm;
		KeyInputManager.OnMouseDown[2] += ChangeStateToAbort;
		LevelEditorApi.BlockMouseInput(this);

		MessengerApi.Log("[Vertexsnapper] Pick the target edge to line up with.", 2f);
	}

	public void Exit()
	{
		KeyInputManager.OnMouseDown[0] -= Confirm;
		KeyInputManager.OnMouseDown[2] -= ChangeStateToAbort;
		LevelEditorApi.UnblockMouseInput(this);
	}

	/// <summary>
	///     The cursor and the hologram are torn down when the target face is confirmed, so this step
	///     puts back the two things it needs: something to snap towards, and a preview that can turn
	///     while the mouse moves between the candidate edges.
	/// </summary>
	private void Rebuild()
	{
		VertexSnapper.SecondCursor = CursorFactory.CreateCursor(
			"SecondCursor",
			MaterialFactory.CreateUnlitMaterial(Color.magenta),
			VertexSnapper.gameObject);
		VertexSnapper.SecondCursor.transform.position = VertexSnapper.SecondTarget.Position;
		CursorFactory.ShapeCursor(
			VertexSnapper.SecondCursor,
			VertexSnapper.SecondTarget,
			SnapMode.Face,
			VertexSnapper.CubeScaleFactor);

		if (!VertexSnapperConfigManager.MovingHologramEnabled.Value)
		{
			return;
		}

		VertexSnapper.Hologram = VertexSnapper.CreateHologram(
			VertexSnapper.BlockSelectionCache.Select(block => block.gameObject),
			WireframeBundleLoader.WireframeMaterial,
			VertexSnapperConfigManager.MovingHologramColor.Value);
		VertexSnapper.Hologram.layer = LayerMask.NameToLayer("Ignore Raycast");
		VertexSnapper.CreateAnchorPoint(
			VertexSnapper.Hologram,
			VertexSnapper.HologramOffsets,
			VertexSnapper.FirstCursor.transform);
	}

	public void Update()
	{
		VertexSnapper.SecondReferenceEdge = ReferencePicker.Update(VertexSnapper, VertexSnapper.SecondTarget);
		VertexSnapper.MoveHologramToCursor(VertexSnapper.SecondTarget.Position);
	}

	private void Confirm()
	{
		if (VertexSnapper.SecondReferenceEdge == null)
		{
			AudioEvents.Blarghl.PlayIfEnabled();
			return;
		}

		if (VertexSnapper.PerformSnap())
		{
			VertexSnapper.ChangeState(new StateCleanUp());
		}
	}

	private void ChangeStateToAbort()
	{
		VertexSnapper.ChangeState(new StateAbort());
	}
}
