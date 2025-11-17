using UnityEngine;
using VertexSnapper.Helper;
using VertexSnapper.Managers;
using ZeepSDK.LevelEditor;

namespace VertexSnapper.States;

public class StateRoaming : IVertexSnapperState<VertexSnapper>
{
    public VertexSnapper VertexSnapper { get; set; }

    public void Enter()
    {
        KeyInputManager.OnKeyHeld[VertexSnapperConfigManager.VertexKeyBind.Value] += ChangeStateToSnapping;
        KeyInputManager.OnMouseDown[2] += ChangeStateToAbort;

        LevelEditorApi.BlockMouseInput(this);
        VertexSnapper.FirstCursor.GetComponentInChildren<Renderer>().material = MaterialFactory.CreateUnlitMaterial(new Color(0f, 1f, 0f, 1f));
    }

    public void Exit()
    {
        KeyInputManager.OnKeyHeld[VertexSnapperConfigManager.VertexKeyBind.Value] -= ChangeStateToSnapping;
        KeyInputManager.OnMouseDown[2] -= ChangeStateToAbort;
        LevelEditorApi.UnblockMouseInput(this);
    }

    public void Update() { }

    private void ChangeStateToAbort()
    {
        VertexSnapper.ChangeState(new StateAbort());
    }

    private void ChangeStateToSnapping()
    {
        VertexSnapper.ChangeState(new StateSetSecondCursor());
    }
}