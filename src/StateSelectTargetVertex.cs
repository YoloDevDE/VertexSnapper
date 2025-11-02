namespace VertexSnapper;

public class StateSelectTargetVertex : IVertexSnapperState<VertexSnapper>
{
    public VertexSnapper VertexSnapper { get; set; }

    public void Enter()
    {
        KeyInputManager.OnKeyUp[VertexSnapperConfigManager.VertexKeyBind.Value] += ChangeStateToRoaming;
        KeyInputManager.OnMouseDown[2] += ChangeStateToIdle;
    }

    public void Exit()
    {
        KeyInputManager.OnKeyUp[VertexSnapperConfigManager.VertexKeyBind.Value] -= ChangeStateToRoaming;
        KeyInputManager.OnMouseDown[2] -= ChangeStateToIdle;
    }

    public void Update() { }

    private void ChangeStateToIdle()
    {
        VertexSnapper.ChangeState(new StateIdle());
    }

    private void ChangeStateToRoaming()
    {
        VertexSnapper.ChangeState(new SnapperStateRoaming());
    }
}