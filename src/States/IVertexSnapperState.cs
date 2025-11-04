namespace VertexSnapper.States;

public interface IVertexSnapperState<T>
{
    T VertexSnapper { get; set; }
    void Enter();
    void Exit();
    void Update();
}