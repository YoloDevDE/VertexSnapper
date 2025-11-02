using ZeepSDK.LevelEditor;

namespace VertexSnapper;

public class SnapperStateRoaming : IVertexSnapperState<VertexSnapper>
{
    public VertexSnapper VertexSnapper { get; set; }

    public void Enter()
    {
        KeyInputManager.OnKeyDown[VertexSnapperConfigManager.VertexKeyBind.Value] += ChangeStateToSnapping;
        KeyInputManager.OnMouseDown[2] += ChangeStateToIdle;
        VertexSnapper.ApplyHologramMaterialForOrigin();
        VertexSnapper.UpdateOriginMaterialPulse();
        LevelEditorApi.BlockMouseInput(this);
    }

    public void Exit()
    {
        KeyInputManager.OnKeyDown[VertexSnapperConfigManager.VertexKeyBind.Value] -= ChangeStateToSnapping;
        KeyInputManager.OnMouseDown[2] -= ChangeStateToIdle;
        VertexSnapper.RestoreOriginMaterials();
        VertexSnapper.RestoreDefaultState();
        LevelEditorApi.UnblockMouseInput(this);
    }

    public void Update()
    {
        VertexSnapper.UpdateOriginMaterialPulse();
    }

    private void ChangeStateToIdle()
    {
        VertexSnapper.ChangeState(new StateIdle());
    }

    private void ChangeStateToSnapping() { }
}