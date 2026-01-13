using FMODSyntax;
using UnityEngine;
using VertexSnapper.Helper;
using VertexSnapper.Managers;

namespace VertexSnapper.States;

public class StateIdle : IVertexSnapperState<VertexSnapper>
{
    private readonly KeyCode _vertexKey = VertexSnapperConfigManager.VertexKeyBind.Value;
    public VertexSnapper VertexSnapper { get; set; }

    public void Enter()
    {
        KeyInputManager.OnKeyDown[_vertexKey] += ChangeStateToSelectOriginVertex;
    }

    public void Exit()
    {
        KeyInputManager.OnKeyDown[_vertexKey] -= ChangeStateToSelectOriginVertex;
    }

    public void Update() { }

    private void ChangeStateToSelectOriginVertex()
    {
        // Respect the config toggle
        if (!VertexSnapperConfigManager.IsEnabled)
        {
            return;
        }

        if (!VertexSnapper.IsInEditingMode)
        {
            return;
        }

        if (VertexSnapper.LevelEditorCentral.selection.list.Count <= 0)
        {
            return;
        }

        if (VertexSnapper.LevelEditorCentral.validation.amountOfBlocks < 2)
        {
            return;
        }

        if (UiTypingDetector.IsTyping())
        {
            return;
        }

        AudioEvents.MenuClick.PlayIfEnabled();
        VertexSnapper.ChangeState(new StateSetFirstCursor());
    }
}