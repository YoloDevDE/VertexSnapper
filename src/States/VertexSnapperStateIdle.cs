using VertexSnapper.Config;
using VertexSnapper.Input;
using VertexSnapper.Interfaces;
using VertexSnapper.Util;

namespace VertexSnapper.States;

public class VertexSnapperStateIdle : IState
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
        Logger.LogInfo("Snap vertex from idle state!");
        StateMachine.ChangeState(new VertexSnapperStateSelectionOriginVertex());
    }
}