using VertexSnapper.Config;
using VertexSnapper.Input;
using VertexSnapper.Interfaces;
using ZeepSDK.Messaging;

namespace VertexSnapper.States.ZeepkistStates.VertexSnapperStates;

public class StateIdle : IState
{
    public IStateMachine StateMachine { get; set; }

    public void Enter(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
        KeyInput.GetKey(VertexSnapperConfig.Instance.SnapperMode.Value).OnKeyDown += HandleSnapperModeKeyDown;
    }

    public void Exit()
    {
        KeyInput.GetKey(VertexSnapperConfig.Instance.SnapperMode.Value).OnKeyDown -= HandleSnapperModeKeyDown;
    }

    private void HandleSnapperModeKeyDown()
    {
        MessengerApi.Log("SnapperModeKeyDown");
        StateMachine.ChangeState(new StateSelectingOriginVertex());
    }
}