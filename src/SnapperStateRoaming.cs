using ZeepSDK.LevelEditor;
using ZeepSDK.Messaging;

namespace VertexSnapper;

public class SnapperStateRoaming : IVertexSnapperState<VertexSnapper>
{
    public VertexSnapper VertexSnapper { get; set; }

    public void Enter()
    {
        KeyInputManager.OnKeyDown[VertexSnapperConfigManager.VertexKeyBind.Value] += ChangeStateToSnapping;
        KeyInputManager.OnMouseDown[2] += ChangeStateToIdle;
        VertexSnapper.UpdateOriginMaterialPulse();
        LevelEditorApi.BlockMouseInput(this);
        MessengerApi.Log("[Vertexsnapper] Snapping-Mode: <b><#00ff00>ACTIVE</color></b><br>Press <b><#00ffff>[MIDDLE_MOUSE_BUTTON]</color></b> or <b><#00ffff>[ESC]</color></b> to abort", 15f);
    }

    public void Exit()
    {
        KeyInputManager.OnKeyDown[VertexSnapperConfigManager.VertexKeyBind.Value] -= ChangeStateToSnapping;
        KeyInputManager.OnMouseDown[2] -= ChangeStateToIdle;
        VertexSnapper.RestoreDefaultState();
        LevelEditorApi.UnblockMouseInput(this);
    }

    public void Update()
    {
        VertexSnapper.UpdateOriginMaterialPulse();
    }

    private void ChangeStateToIdle()
    {
        MessengerApi.Log("[Vertexsnapper] Snapping-Mode: <b><#ff0000>INACTIVE</color></b><br>");
        VertexSnapper.ChangeState(new StateIdle());
    }

    private void ChangeStateToSnapping() { }
}