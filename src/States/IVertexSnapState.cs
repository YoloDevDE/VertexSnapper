namespace VertexSnapper.States;

public interface IVertexSnapState
{
    VertexSnapMode Mode { get; }
    void OnEnter();
    void OnExit();
    void Update();
    void HandleInput(bool keyDown, bool leftMousePressed);
}