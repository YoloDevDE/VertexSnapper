using VertexSnapper.Interfaces;

namespace VertexSnapper.States;

public class VertexSnapperStateRoaming : IState
{
    public IStateMachine StateMachine { get; set; }

    public void Enter(IStateMachine stateMachine)
    {
    }

    public void Exit()
    {
    }
}