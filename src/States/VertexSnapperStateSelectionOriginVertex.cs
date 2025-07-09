using UnityEngine;
using VertexSnapper.Config;
using VertexSnapper.Input;
using VertexSnapper.Interfaces;
using VertexSnapper.Util;
using ZeepSDK.LevelEditor;
using Logger = VertexSnapper.Util.Logger;

namespace VertexSnapper.States;

public class VertexSnapperStateSelectionOriginVertex : IState
{
    private VertexSelectionManager _vertexSelectionManager;
    public IStateMachine StateMachine { get; set; }

    public void Enter(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
        LevelEditorApi.BlockMouseInput(this);
        Logger.LogInfo("Entering VertexSnapperStateSelectionOriginVertex");

        // Cast to GameStateMachine to access the specific properties
        GameStateMachine gameStateMachine = stateMachine as GameStateMachine;
        if (gameStateMachine == null)
        {
            Logger.LogError("StateMachine is not a GameStateMachine!");
            return;
        }

        // Create vertex selection manager
        GameObject managerObject = new GameObject("VertexSelectionManager");
        _vertexSelectionManager = managerObject.AddComponent<VertexSelectionManager>();
        _vertexSelectionManager.Initialize(gameStateMachine);

        // Subscribe to input
        KeyInput.GetKey(VertexSnapperConfig.Instance.SnapperMode.Value).OnKeyUp += HandleSnapperModeKeyUp;
    }

    public void Exit()
    {
        LevelEditorApi.UnblockMouseInput(this);
        Logger.LogInfo("Exiting VertexSnapperStateSelectionOriginVertex");

        // Cleanup vertex selection manager
        if (_vertexSelectionManager != null)
        {
            _vertexSelectionManager.Cleanup();
            Object.Destroy(_vertexSelectionManager.gameObject);
        }

        // Unsubscribe from input
        KeyInput.GetKey(VertexSnapperConfig.Instance.SnapperMode.Value).OnKeyUp -= HandleSnapperModeKeyUp;
    }

    private void HandleSnapperModeKeyUp()
    {
        StateMachine.ChangeState(new VertexSnapperStateIdle());
    }
}