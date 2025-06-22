using System.Collections.Generic;
using VertexSnapper.States;

namespace VertexSnapper.Core;

public class VertexSnapStateMachine
{
    private readonly VertexSnapData data;
    private readonly VertexSnapLogger logger;
    private readonly Dictionary<VertexSnapMode, IVertexSnapState> states;

    private IVertexSnapState currentState;

    public VertexSnapStateMachine(VertexSnapLogger logger, VertexSnapData data)
    {
        this.logger = logger;
        this.data = data;
        states = new Dictionary<VertexSnapMode, IVertexSnapState>();

        InitializeStates();
    }

    public VertexSnapMode CurrentMode => currentState?.Mode ?? VertexSnapMode.Inactive;
    public bool IsActive => CurrentMode != VertexSnapMode.Inactive;

    public void TransitionTo(VertexSnapMode mode, string reason)
    {
        logger.LogMethodEntry(nameof(TransitionTo), $"to {mode}, reason: {reason}");

        if (!states.ContainsKey(mode))
        {
            logger.LogError($"State {mode} not found!");
            return;
        }

        VertexSnapMode oldMode = currentState?.Mode ?? VertexSnapMode.Inactive;

        if (oldMode == mode)
        {
            logger.LogDebug($"Already in {mode} state, ignoring transition");
            return;
        }

        logger.LogStateTransition(oldMode, mode, reason);

        currentState?.OnExit();
        currentState = states[mode];
        currentState.OnEnter();

        logger.LogMethodExit(nameof(TransitionTo));
    }

    public void Update()
    {
        currentState?.Update();
    }

    public void HandleInput(bool keyDown, bool leftMousePressed)
    {
        currentState?.HandleInput(keyDown, leftMousePressed);
    }

    private void InitializeStates()
    {
        logger.LogMethodEntry(nameof(InitializeStates));

        states[VertexSnapMode.Inactive] = new InactiveState(logger, data, this);
        states[VertexSnapMode.Positioning] = new PositioningState(logger, data, this);
        states[VertexSnapMode.Snapping] = new SnappingState(logger, data, this);

        currentState = states[VertexSnapMode.Inactive];

        logger.LogObjectCount("states", states.Count);
        logger.LogMethodExit(nameof(InitializeStates));
    }
}