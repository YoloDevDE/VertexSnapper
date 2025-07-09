using VertexSnapper.Interfaces;

namespace VertexSnapper.States;

public class VertexState : IState
{
    private IState stateImplementation;

    public IStateMachine StateMachine
    {
        get => stateImplementation.StateMachine;
        set => stateImplementation.StateMachine = value;
    }

    public void Enter(IStateMachine stateMachine)
    {
        stateImplementation.Enter(stateMachine);
    }

    public void Exit()
    {
        stateImplementation.Exit();
    }
}