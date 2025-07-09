using System;

namespace VertexSnapper.Input;

public class KeyEvents
{
    public event Action OnKeyDown;
    public event Action OnKeyUp;
    public event Action OnKeyHold;

    internal void TriggerKeyDown()
    {
        OnKeyDown?.Invoke();
    }

    internal void TriggerKeyUp()
    {
        OnKeyUp?.Invoke();
    }

    internal void TriggerKeyHold()
    {
        OnKeyHold?.Invoke();
    }
}