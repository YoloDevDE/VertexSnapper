using UnityEngine;
using ZeepSDK.LevelEditor;
using ZeepSDK.Messaging;

namespace VertexSnapper;

public class SnapperStateRoaming : IVertexSnapperState<VertexSnapper>
{
    public VertexSnapper VertexSnapper { get; set; }

    public void Enter()
    {
        KeyInputManager.OnKeyDown[VertexSnapperConfigManager.VertexKeyBind.Value] += ChangeStateToSnapping;
        KeyInputManager.OnMouseDown[2] += ChangeStateToAbort;
        LevelEditorApi.BlockMouseInput(this);
        VertexSnapper.ApplyMaterialToBlocks(VertexSnapper.PreviouslySelectedBlocks, VertexSnapper.TransparentHologramMaterial(new Color(1f, 1f, 0f, 0.25f)));
        VertexSnapper.FirstVertex.GetComponentInChildren<Renderer>().material = VertexSnapper.TransparentHologramMaterial(new Color(1f, 1f, 0f, 1f), 2);
        KeyInputManager.OnKeyHeld[VertexSnapperConfigManager.VertexKeyBind.Value] += ChangeStateToSnapping;

        MessengerApi.Log($"[Vertexsnapper] Snapping-Mode: <b><#00ff00>ACTIVE</color></b><br>" +
                         $"<align-left>Press <b><#00ffff>[MIDDLE_MOUSE_BUTTON]</color></b> or <b><#00ffff>[ESC]</color></b> to abort<br>" +
                         $"Hold the <b><#00ffff>[{VertexSnapperConfigManager.VertexKeyBind.Value}]</color></b> Key while aiming on any block and confirm with <b><#00ffff>[LEFT_MOUSE_BUTTON]</color></b> to snap</align-left>", 10f);
    }

    public void Exit()
    {
        KeyInputManager.OnKeyDown[VertexSnapperConfigManager.VertexKeyBind.Value] -= ChangeStateToSnapping;
        KeyInputManager.OnMouseDown[2] -= ChangeStateToAbort;
        
        KeyInputManager.OnKeyHeld[VertexSnapperConfigManager.VertexKeyBind.Value] -= ChangeStateToSnapping;
        LevelEditorApi.UnblockMouseInput(this);
    }

    public void Update() { }

    private void ChangeStateToAbort()
    {
        VertexSnapper.ChangeState(new StateAbort());
    }

    private void ChangeStateToSnapping()
    {
        VertexSnapper.ChangeState(new StateSnapping());
    }
}