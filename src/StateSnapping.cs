using UnityEngine;
using ZeepSDK.LevelEditor;

namespace VertexSnapper;

public class StateSnapping : IVertexSnapperState<VertexSnapper>
{
    private Transform _checkWhateverThing = new GameObject().transform;
    public VertexSnapper VertexSnapper { get; set; }

    public void Enter()
    {
        KeyInputManager.OnKeyUp[VertexSnapperConfigManager.VertexKeyBind.Value] += ChangeStateToRoaming;
        KeyInputManager.OnMouseDown[0] += InvokeSnapProcess;
        KeyInputManager.OnMouseDown[2] += ChangeStateToAbort;
        LevelEditorApi.BlockMouseInput(this);
        VertexSnapper.CreateAndMoveSecondCursorToClosestVertex();
        VertexSnapper.CloneSelectedBlocks();
    }

    public void Exit()
    {
        KeyInputManager.OnKeyUp[VertexSnapperConfigManager.VertexKeyBind.Value] -= ChangeStateToRoaming;
        KeyInputManager.OnMouseDown[2] -= ChangeStateToAbort;
        KeyInputManager.OnMouseDown[0] -= InvokeSnapProcess;
        LevelEditorApi.UnblockMouseInput(this);
        VertexSnapper.SafeDestroy(VertexSnapper.SecondVertex);
        VertexSnapper.SafeDestroy(VertexSnapper.VertexsnapperHologram);
    }

    public void Update()
    {
        VertexSnapper.CreateAndMoveSecondCursorToClosestVertex();
        VertexSnapper.UpdateHologramPulse();
        if (!_checkWhateverThing)
        {
            _checkWhateverThing = new GameObject().transform;
        }

        if (_checkWhateverThing?.position == VertexSnapper.SecondVertex.position)
        {
            return;
        }


        VertexSnapper.SafeDestroy(VertexSnapper.VertexsnapperHologram);
        VertexSnapper.CloneSelectedBlocks();
        _checkWhateverThing!.position = VertexSnapper.SecondVertex.position;
        VertexSnapper.MoveHologramToVertex(VertexSnapper.VertexsnapperHologram);
    }

    private void InvokeSnapProcess()
    {
        if (VertexSnapper.PerformSnap())
        {
            ChangeStateToAbort();
        }
    }

    private void ChangeStateToAbort()
    {
        VertexSnapper.ChangeState(new StateAbort());
    }

    private void ChangeStateToRoaming()
    {
        VertexSnapper.ChangeState(new SnapperStateRoaming());
    }
}