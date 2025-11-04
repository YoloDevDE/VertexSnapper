using UnityEngine;
using VertexSnapper.Components;
using VertexSnapper.Managers;
using ZeepSDK.LevelEditor;

namespace VertexSnapper.States;

public class StateSetSecondVertex : IVertexSnapperState<Components.VertexSnapper>
{
    private Transform _prevSecondVertexTransform = new GameObject().transform;
    public Components.VertexSnapper VertexSnapper { get; set; }

    public void Enter()
    {
        KeyInputManager.OnKeyUp[VertexSnapperConfigManager.VertexKeyBind.Value] += ChangeStateToRoaming;
        KeyInputManager.OnMouseDown[0] += InvokeSnapProcess;
        KeyInputManager.OnMouseDown[2] += ChangeStateToAbort;
        LevelEditorApi.BlockMouseInput(this);
        VertexSnapper.CreateAndMoveSecondCursorToClosestVertex();
        VertexSnapper.CloneSelectedBlocks();
        DistanceIndicator.Show(VertexSnapper.FirstVertex.position, VertexSnapper.SecondVertex.position);
    }

    public void Exit()
    {
        DistanceIndicator.DestroyIndicator();
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
        if (!_prevSecondVertexTransform)
        {
            _prevSecondVertexTransform = new GameObject().transform;
        }

        if (_prevSecondVertexTransform?.position == VertexSnapper.SecondVertex.position)
        {
            return;
        }


        VertexSnapper.SafeDestroy(VertexSnapper.VertexsnapperHologram);
        VertexSnapper.CloneSelectedBlocks();
        _prevSecondVertexTransform!.position = VertexSnapper.SecondVertex.position;
        VertexSnapper.MoveHologramToVertex(VertexSnapper.VertexsnapperHologram);
        DistanceIndicator.Show(VertexSnapper.FirstVertex.position, VertexSnapper.SecondVertex.position);
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
        VertexSnapper.ChangeState(new StateRoaming());
    }
}