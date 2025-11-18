namespace VertexSnapper.States;

public class StateCleanUp : IVertexSnapperState<VertexSnapper>
{
    public VertexSnapper VertexSnapper { get; set; }


    public void Enter()
    {
        VertexSnapper.RestoreDefaultState();
        ChangeStateToIdle();
    }

    public void Exit() { }

    public void Update() { }

    private void ChangeStateToIdle()
    {
        VertexSnapper.ChangeState(new StateIdle());
    }
}