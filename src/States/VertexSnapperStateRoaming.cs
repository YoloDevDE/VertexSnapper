using UnityEngine;
using VertexSnapper.Input;
using VertexSnapper.Interfaces;
using VertexSnapper.Util;
using ZeepSDK.LevelEditor;

namespace VertexSnapper.States;

public class VertexSnapperStateRoaming : IState
{
    public VertexHologramManager VertexHologramManager { get; set; }
    public SnapTargetDetector SnapTargetDetector { get; set; }
    public IStateMachine StateMachine { get; set; }

    public void Enter(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
        KeyInput.GetKey(KeyCode.Mouse2).OnKeyDown += HandleAbortVertex;
        LevelEditorApi.BlockMouseInput(this);

        // Erstelle VertexHologramManager
        GameObject hologramManagerObject = new GameObject("VertexHologramManager");
        VertexHologramManager = hologramManagerObject.AddComponent<VertexHologramManager>();
        VertexHologramManager.Initialize(StateMachine as GameStateMachine);

        // Erstelle SnapTargetDetector
        GameObject snapDetectorObject = new GameObject("SnapTargetDetector");
        SnapTargetDetector = snapDetectorObject.AddComponent<SnapTargetDetector>();
        SnapTargetDetector.Initialize(StateMachine as GameStateMachine);
    }

    public void Exit()
    {
        KeyInput.GetKey(KeyCode.Mouse2).OnKeyDown -= HandleAbortVertex;
        LevelEditorApi.UnblockMouseInput(this);

        // Cleanup VertexHologramManager
        if (VertexHologramManager != null)
        {
            Object.Destroy(VertexHologramManager.gameObject);
        }

        // Cleanup SnapTargetDetector
        if (SnapTargetDetector != null)
        {
            SnapTargetDetector.Cleanup();
            Object.Destroy(SnapTargetDetector.gameObject);
        }
    }

    private void HandleAbortVertex()
    {
        StateMachine.ChangeState(new VertexSnapperStateIdle());
    }
}