namespace VertexSnapper.Interfaces;

public interface IStateMachine
{
    IState CurrentState { get; set; }
    void ChangeState(IState state);
    void Stop();
}