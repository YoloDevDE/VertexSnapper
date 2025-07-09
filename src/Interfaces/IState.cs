namespace VertexSnapper.Interfaces;

public interface IState
{
    IStateMachine StateMachine { get; set; }
    void Enter(IStateMachine stateMachine);
    void Exit();
}