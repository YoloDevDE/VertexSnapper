using UnityEngine;
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
        if (VertexSnapper.LevelEditorCentral.selection.list.Count <= 0 || VertexSnapper.LevelEditorCentral.validation.amountOfBlocks < 2)
        {
            return;
        }

        VertexSnapper.ChangeState(new StateSetFirstCursor());
    }
}