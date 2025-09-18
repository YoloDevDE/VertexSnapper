using VertexSnapper.Interfaces;

namespace VertexSnapper.States.ZeepkistStates.VertexSnapperStates;

public class StateSelectingTargetVertex : IState
{
    public IStateMachine StateMachine { get; set; }

    public void Enter(IStateMachine stateMachine)
    {
    }

    public void Exit()
    {
    }
}