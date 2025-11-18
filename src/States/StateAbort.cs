using FMODSyntax;
using ZeepSDK.Messaging;

namespace VertexSnapper.States;

public class StateAbort : IVertexSnapperState<VertexSnapper>
{
    public VertexSnapper VertexSnapper { get; set; }


    public void Enter()
    {
        MessengerApi.Log("[Vertexsnapper] Im not gonna snap <sprite=\"Zeepkist\" name=\"YannicSmile\">", 0.8f);
        AudioEvents.MenuClick.Play();
        ChangeStateToCleanUp();
    }

    public void Exit() { }


    public void Update() { }

    private void ChangeStateToCleanUp()
    {
        VertexSnapper.ChangeState(new StateCleanUp());
    }
}