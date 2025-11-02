using ZeepSDK.LevelEditor;
using ZeepSDK.Messaging;

namespace VertexSnapper;

public class StateSelectOriginVertex : IVertexSnapperState<VertexSnapper>
{
    public VertexSnapper VertexSnapper { get; set; }


    public void Enter()
    {
        KeyInputManager.OnKeyUp[VertexSnapperConfigManager.VertexKeyBind.Value] += ChangeStateToIdle;
        KeyInputManager.OnMouseDown[0] += TryChangeStateToRoaming;
        LevelEditorApi.BlockMouseInput(this);
        VertexSnapper.CacheAndRemoveBlockSelection();
    }

    public void Exit()
    {
        KeyInputManager.OnKeyUp[VertexSnapperConfigManager.VertexKeyBind.Value] -= ChangeStateToIdle;
        KeyInputManager.OnMouseDown[0] -= TryChangeStateToRoaming;
        LevelEditorApi.UnblockMouseInput(this);
        VertexSnapper.ReAddPreviousBlockSelection();
    }

    public void Update()
    {
        VertexSnapper.CreateAndMoveOriginCursorToClosestVertex();
    }

    private void TryChangeStateToRoaming()
    {
        if (OriginIsValid())
        {
            VertexSnapper.ChangeState(new SnapperStateRoaming());
        }
    }

    private bool OriginIsValid()
    {
        MessengerApi.LogSuccess("Valid!");
        return VertexSnapper;
    }

    private void ChangeStateToIdle()
    {
        VertexSnapper.SafeDestroy(VertexSnapper.OriginVertexPoint.gameObject);
        VertexSnapper.ChangeState(new StateIdle());
    }
}