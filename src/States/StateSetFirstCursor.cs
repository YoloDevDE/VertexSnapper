using FMODSyntax;
using UnityEngine;
using VertexSnapper.Helper;
using VertexSnapper.Managers;
using VertexSnapper.Snapping;
using ZeepSDK.LevelEditor;
using ZeepSDK.Messaging;

namespace VertexSnapper.States;

public class StateSetFirstCursor : IVertexSnapperState<VertexSnapper>
{
	public VertexSnapper VertexSnapper { get; set; }

	public void Enter()
	{
		KeyInputManager.OnKeyUp[VertexSnapperConfigManager.VertexKeyBind.Value] += ChangeStateToAbort;
		KeyInputManager.OnMouseDown[0] += TryChangeStateToRoaming;
		KeyInputManager.AnyScroll += OnAnyScroll;
		KeyInputManager.OnKeyDown[VertexSnapperConfigManager.SnapModeCycleKeyBind.Value] += CycleSnapMode;

		LevelEditorApi.BlockMouseInput(this);
		LevelEditorApi.BlockKeyboardInput(this);

		VertexSnapper.CacheAndRemoveBlockSelection();
		if (VertexSnapperConfigManager.OriginHologramEnabled.Value)
		{
			VertexSnapper.ApplyWireframeMaterial(
				VertexSnapper.BlockSelectionCache,
				VertexSnapperConfigManager.OriginHologramColor.Value
			);
		}


		MessengerApi.Log("[Vertexsnapper] Im gonna snap! <sprite=\"moremojis\" name=\"ZaagBladPadRood2\">", 0.6f);
	}


	public void Exit()
	{
		// Info: how to abort with middle mouse button (no warning)
		MessengerApi.Log(
			"[Vertexsnapper] You can abort vertex snapping anytime with the <#f00>middle mouse button</color> or <#f00>ESC</color>.",
			5f
		);

		KeyInputManager.OnKeyUp[VertexSnapperConfigManager.VertexKeyBind.Value] -= ChangeStateToAbort;
		KeyInputManager.OnMouseDown[0] -= TryChangeStateToRoaming;
		KeyInputManager.AnyScroll -= OnAnyScroll;
		KeyInputManager.OnKeyDown[VertexSnapperConfigManager.SnapModeCycleKeyBind.Value] -= CycleSnapMode;

		LevelEditorApi.UnblockMouseInput(this);
		LevelEditorApi.UnblockKeyboardInput(this);
	}

	public void Update()
	{
		if (RaycastUtils.IsSphereCastOnBlockSuccessful(VertexSnapper.MainCamera, out RaycastHit hit,
			    VertexSnapper.BlockSelectionCache))
		{
			if (!VertexSnapper.FirstCursor)
			{
				VertexSnapper.FirstCursor = CursorFactory.CreateCursor(
					"FirstCursor",
					MaterialFactory.CreateUnlitMaterial(Color.magenta),
					VertexSnapper.gameObject,
					VertexSnapper.CubeScaleFactor
				);
			}

			SnapTarget target = SnapTargetFinder.Find(hit, VertexSnapper.CurrentSnapMode);
			if (target == null)
			{
				return;
			}

			VertexSnapper.FirstTarget = target;
			CursorFactory.ShapeCursor(
				VertexSnapper.FirstCursor,
				target,
				VertexSnapper.CurrentSnapMode,
				VertexSnapper.CubeScaleFactor);

			if (VertexSnapper.FirstCursor.transform.position == target.Position)
			{
				return;
			}

			AudioEvents.MenuHover1.PlayIfEnabled();
			VertexSnapper.FirstCursor.transform.position = target.Position;

			return;
		}

		VertexSnapper.SafeDestroy(VertexSnapper.FirstCursor);
	}

	private void OnAnyScroll(float delta)
	{
		if (!VertexSnapper.FirstCursor)
		{
			return;
		}

		// Scale off the stored factor, not off localScale - an edge or face cursor is stretched,
		// so reading its scale back would make every scroll step grow it further out of shape.
		const float scaleSpeed = 0.2f;
		float factor = 1f + delta * scaleSpeed;

		VertexSnapper.CubeScaleFactor = Mathf.Clamp(VertexSnapper.CubeScaleFactor * factor, 0.05f, 5f);
		VertexSnapper.CubeSize = Vector3.one * VertexSnapper.CubeScaleFactor;
	}


	private void TryChangeStateToRoaming()
	{
		if (!OriginIsValid())
		{
			MessengerApi.LogWarning(
				$"[Vertexsnapper] No Vertex selected!<br><align=left><indent=15%>To select a vertex, hold down <#f00>[{VertexSnapperConfigManager.VertexKeyBind.Value}]</color> while hovering over the <b>block selection</b>.<br>To confirm, press the <#f00>left mouse button</color>.</align>",
				10f);
			AudioEvents.Blarghl.PlayIfEnabled();
			return;
		}

		AudioEvents.MenuClick.PlayIfEnabled();
		if (VertexSnapper.CurrentSnapMode == SnapMode.Face)
		{
			VertexSnapper.ChangeState(new StateSetFirstReference());
			return;
		}

		VertexSnapper.ChangeState(new StateRoaming());
	}

	private bool OriginIsValid()
	{
		return VertexSnapper && VertexSnapper.FirstCursor;
	}

	private void CycleSnapMode()
	{
		VertexSnapper.CycleSnapMode();
	}

	private void ChangeStateToAbort()
	{
		VertexSnapper.ChangeState(new StateAbort());
	}
}
