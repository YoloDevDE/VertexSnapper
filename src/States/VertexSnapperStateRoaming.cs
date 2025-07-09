using UnityEngine;
using VertexSnapper.Interfaces;
using VertexSnapper.Util;

namespace VertexSnapper.States;

public class VertexSnapperStateRoaming : IState
{
    public VertexHologramManager VertexHologramManager { get; set; }
    public IStateMachine StateMachine { get; set; }

    public void Enter(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
        GameObject managerObject = new GameObject("VertexSelectionManager");
        VertexHologramManager = managerObject.AddComponent<VertexHologramManager>();
        VertexHologramManager.Initialize(StateMachine as GameStateMachine);
    }

    public void Exit()
    {
    }
}