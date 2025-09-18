using VertexSnapper.Interfaces;
using ZeepSDK.LevelEditor;

namespace VertexSnapper.States.ZeepkistStates;

public class GameStateNotInEditor : IState
{
    public IStateMachine StateMachine { get; set; }

    public void Enter(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
        LevelEditorApi.EnteredLevelEditor += HandleEnteredLevelEditor;
    }

    public void Exit()
    {
        LevelEditorApi.EnteredLevelEditor -= HandleEnteredLevelEditor;
    }

    private void HandleEnteredLevelEditor()
    {
        StateMachine.ChangeState(new GameStateInEditor());
    }
}