using UnityEngine;
using VertexSnapper.Interfaces;
using VertexSnapper.Managers;
using VertexSnapper.States.ZeepkistStates.VertexSnapperStates;
using ZeepSDK.LevelEditor;

namespace VertexSnapper.States.ZeepkistStates;

public class GameStateInEditor : IState
{
    private GameInputManager gameInputManager;
    public IStateMachine VertexSnapperStateMachine { get; set; }
    public IStateMachine StateMachine { get; set; }

    public void Enter(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
        LevelEditorApi.ExitedLevelEditor += HandleExitedLevelEditor;

        // Check if component already exists
        gameInputManager = Plugin.Instance.gameObject.GetComponent<GameInputManager>() ?? Plugin.Instance.gameObject.AddComponent<GameInputManager>();
        gameInputManager.enabled = true;


        VertexSnapperStateMachine = new GameStateMachine();
        VertexSnapperStateMachine.ChangeState(new StateIdle());
    }


    public void Exit()
    {
        VertexSnapperStateMachine.Stop();
        LevelEditorApi.ExitedLevelEditor -= HandleExitedLevelEditor;

        // Destroy the component when exiting the state
        if (!gameInputManager)
        {
            return;
        }

        Object.Destroy(gameInputManager);
        gameInputManager = null;
    }

    private void HandleExitedLevelEditor()
    {
        StateMachine.ChangeState(new GameStateNotInEditor());
    }
}