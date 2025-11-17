using UnityEngine;
using VertexSnapper.Helper;
using VertexSnapper.Managers;
using ZeepSDK.LevelEditor;
using ZeepSDK.Messaging;

namespace VertexSnapper.States;

public class StateSetFirstCursor : IVertexSnapperState<VertexSnapper>
{
    public VertexSnapper VertexSnapper { get; set; }

    public void Enter()
    {
        KeyInputManager.OnKeyUp[VertexSnapperConfigManager.VertexKeyBind.Value] += ChangeStateToAbort;
        KeyInputManager.OnMouseDown[0] += TryChangeStateToRoaming;
        LevelEditorApi.BlockMouseInput(this);
        LevelEditorApi.BlockKeyboardInput(this);

        VertexSnapper.CacheAndRemoveBlockSelection();
        VertexSnapper.CacheOriginalMaterials(VertexSnapper.BlockSelectionCache, VertexSnapper.BlockSelectionMaterials);
        VertexSnapper.ApplyWireframeMaterial(VertexSnapper.BlockSelectionCache);

        MessengerApi.Log("[Vertexsnapper] Im gonna snap! <sprite=\"moremojis\" name=\"ZaagBladPadRood2\">", 0.6f);
    }

    public void Exit()
    {
        VertexSnapper.RestoreOriginalMaterials(VertexSnapper.BlockSelectionMaterials);

        KeyInputManager.OnKeyUp[VertexSnapperConfigManager.VertexKeyBind.Value] -= ChangeStateToAbort;
        KeyInputManager.OnMouseDown[0] -= TryChangeStateToRoaming;
        LevelEditorApi.UnblockMouseInput(this);
        LevelEditorApi.UnblockKeyboardInput(this);
    }

    public void Update()
    {
        if (RaycastUtils.IsSphereCastOnBlockSuccessful(VertexSnapper.MainCamera, out RaycastHit hit, VertexSnapper.BlockSelectionCache))
        {
            if (!VertexSnapper.FirstCursor)
            {
                VertexSnapper.FirstCursor = CursorFactory.CreateCursor("FirstCursor", MaterialFactory.CreateUnlitMaterial(Color.magenta), VertexSnapper.gameObject);
            }

            VertexSnapper.FirstCursor.transform.position = VertexSnapper.FindClosestVertexToHit(hit);
            return;
        }

        VertexSnapper.SafeDestroy(VertexSnapper.FirstCursor);
    }


    private void TryChangeStateToRoaming()
    {
        if (!OriginIsValid())
        {
            MessengerApi.LogWarning(
                $"[Vertexsnapper] No Vertex selected!<br><align=left><indent=15%>To select a vertex, hold down <#f00>[{VertexSnapperConfigManager.VertexKeyBind.Value}]</color> while hovering over the <b>block selection</b>.<br>To confirm, press the <#f00>left mouse button</color>.</align>",
                10f);
            return;
        }

        VertexSnapper.ChangeState(new StateRoaming());
    }

    private bool OriginIsValid() => VertexSnapper && VertexSnapper.FirstCursor;

    private void ChangeStateToAbort()
    {
        MessengerApi.Log("[Vertexsnapper] Im not gonna snap <sprite=\"Zeepkist\" name=\"YannicSmile\">", 0.6f);
        VertexSnapper.ChangeState(new StateAbort());
    }
}