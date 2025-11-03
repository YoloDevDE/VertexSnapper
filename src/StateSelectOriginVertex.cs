using ZeepSDK.LevelEditor;
using ZeepSDK.Messaging;

namespace VertexSnapper;

public class StateSelectOriginVertex : IVertexSnapperState<VertexSnapper>
{
    public VertexSnapper VertexSnapper { get; set; }


    public void Enter()
    {
        KeyInputManager.OnKeyUp[VertexSnapperConfigManager.VertexKeyBind.Value] += ChangeStateToAbort;
        KeyInputManager.OnMouseDown[0] += TryChangeStateToRoaming;
        LevelEditorApi.BlockMouseInput(this);
        LevelEditorApi.BlockKeyboardInput(this);
        VertexSnapper.CacheAndRemoveBlockSelection();
        VertexSnapper.ApplyWireframeMaterial();
        MessengerApi.Log("[Vertexsnapper] Im gonna snap! <sprite=\"moremojis\" name=\"ZaagBladPadRood2\">", 0.6f);
    }

    public void Exit()
    {
        KeyInputManager.OnKeyUp[VertexSnapperConfigManager.VertexKeyBind.Value] -= ChangeStateToAbort;
        KeyInputManager.OnMouseDown[0] -= TryChangeStateToRoaming;
        LevelEditorApi.UnblockMouseInput(this);
        LevelEditorApi.UnblockKeyboardInput(this);
    }

    public void Update()
    {
        VertexSnapper.CreateAndMoveFirstCursorToClosestVertex();
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
        return VertexSnapper;
    }

    private void ChangeStateToAbort()
    {
        MessengerApi.Log("[Vertexsnapper] Im not gonna snap <sprite=\"Zeepkist\" name=\"YannicSmile\">", 0.6f);
        VertexSnapper.ChangeState(new StateAbort());
    }
}