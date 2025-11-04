using UnityEngine;
using VertexSnapper.Helper;
using VertexSnapper.Managers;
using ZeepSDK.LevelEditor;

namespace VertexSnapper.States;

public class StateRoaming : IVertexSnapperState<Components.VertexSnapper>
{
    public Components.VertexSnapper VertexSnapper { get; set; }

    public void Enter()
    {
        KeyInputManager.OnKeyDown[VertexSnapperConfigManager.VertexKeyBind.Value] += ChangeStateToSnapping;
        KeyInputManager.OnKeyHeld[VertexSnapperConfigManager.VertexKeyBind.Value] += ChangeStateToSnapping;
        KeyInputManager.OnMouseDown[2] += ChangeStateToAbort;

        LevelEditorApi.BlockMouseInput(this);
        VertexSnapper.ApplyMaterialToBlocks(VertexSnapper.PreviouslySelectedBlocks, WireframeBundleLoader.WireframeMaterial);
        VertexSnapper.FirstVertex.GetComponentInChildren<Renderer>().material = VertexSnapper.TransparentHologramMaterial(new Color(0f, 1f, 0f, 1f), 2);

        // MessengerApi.Log($"[Vertexsnapper] Snapping-Mode: <b><#00ff00>ACTIVE</color></b><br>" +
        //                  $"<align=left>Press <b><#00ffff>[MIDDLE_MOUSE_BUTTON]</color></b> or <b><#00ffff>[ESC]</color></b> to abort<br>" +
        //                  $"Hold the <b><#00ffff>[{VertexSnapperConfigManager.VertexKeyBind.Value}]</color></b> Key while aiming on any block and confirm with <b><#00ffff>[LEFT_MOUSE_BUTTON]</color></b> to snap</align>", 10f);
    }

    public void Exit()
    {
        KeyInputManager.OnKeyDown[VertexSnapperConfigManager.VertexKeyBind.Value] -= ChangeStateToSnapping;
        KeyInputManager.OnKeyHeld[VertexSnapperConfigManager.VertexKeyBind.Value] -= ChangeStateToSnapping;
        KeyInputManager.OnMouseDown[2] -= ChangeStateToAbort;
        LevelEditorApi.UnblockMouseInput(this);
    }

    public void Update() { }

    private void ChangeStateToAbort()
    {
        VertexSnapper.ChangeState(new StateAbort());
    }

    private void ChangeStateToSnapping()
    {
        if (!Physics.Raycast(VertexSnapper.MainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
        {
            return;
        }

        BlockProperties block = hit.collider.GetComponentInParent<BlockProperties>();
        if (!block)
        {
            return;
        }

        VertexSnapper.ChangeState(new StateSetSecondVertex());
    }
}