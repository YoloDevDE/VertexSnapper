using System.Collections.Generic;
using UnityEngine;
using VertexSnapper.Interfaces;
using Logger = VertexSnapper.Util.Logger;

namespace VertexSnapper.States;

public sealed class GameStateMachine : IStateMachine
{
    public List<BlockProperties> BlockSelection { get; set; }
    public Vector3 VertexOrigin { get; set; }
    public IState CurrentState { get; set; }

    public void ChangeState(IState state)
    {
        if (CurrentState != null)
        {
            Logger.LogInfo($"[{GetType().Name}] Exiting state: {CurrentState?.GetType().Name}");
            CurrentState?.Exit();
        }

        CurrentState = state;
        Logger.LogInfo($"[{GetType().Name}] Entering state: {CurrentState?.GetType().Name}");
        CurrentState?.Enter(this);
    }

    public void Stop()
    {
        Logger.LogInfo($"[{GetType().Name}] Stopping - Exiting state: {CurrentState?.GetType().Name}");
        CurrentState?.Exit();
        CurrentState = null;
    }
}