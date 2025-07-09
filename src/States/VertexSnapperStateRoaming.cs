using UnityEngine;
using VertexSnapper.Interfaces;
using VertexSnapper.Util;
using ZeepSDK.LevelEditor;

namespace VertexSnapper.States;

public class VertexSnapperStateRoaming : IState
{
    public VertexHologramManager VertexHologramManager { get; set; }
    public IStateMachine StateMachine { get; set; }

    public void Enter(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
        LevelEditorApi.BlockMouseInput(this);
        GameObject managerObject = new GameObject("VertexHologramManager");
        VertexHologramManager = managerObject.AddComponent<VertexHologramManager>();
        VertexHologramManager.Initialize(StateMachine as GameStateMachine);
    }

    public void Exit()
    {
    }
}