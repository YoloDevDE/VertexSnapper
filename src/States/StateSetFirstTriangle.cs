using UnityEngine;
using VertexSnapper.Components;
using VertexSnapper.Managers;
using ZeepSDK.LevelEditor;
using ZeepSDK.Messaging;

namespace VertexSnapper.States;

public class StateSetFirstTriangle : IVertexSnapperState<Components.VertexSnapper>
{
    public Components.VertexSnapper VertexSnapper { get; set; }


    public void Enter()
    {
        KeyInputManager.OnMouseDown[0] += TryChangeStateToRoaming;
        LevelEditorApi.BlockMouseInput(this);
        LevelEditorApi.BlockKeyboardInput(this);
        VertexSnapper.ApplyMaterialToBlocks(VertexSnapper.PreviouslySelectedBlocks, VertexSnapper.TransparentHologramMaterial(new Color(.5f, .5f, 1f, 0.25f)));
        MessengerApi.Log("[Vertexsnapper] TRIANGLE!!", 0.6f);
    }

    public void Exit()
    {
        KeyInputManager.OnMouseDown[0] -= TryChangeStateToRoaming;
        LevelEditorApi.UnblockMouseInput(this);
        LevelEditorApi.UnblockKeyboardInput(this);
    }

    public void Update()
    {
        if (Physics.Raycast(VertexSnapper.MainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 1000f))
        {
            if (hit.collider.GetComponentInParent<BlockProperties>() != null)
            {
                TriangleHighlighter.Highlight(hit);
            }
            else
            {
                TriangleHighlighter.Clear();
            }
        }
        else
        {
            TriangleHighlighter.Clear();
        }
    }


    private void TryChangeStateToRoaming()
    {
        if (OriginIsValid())
        {
            VertexSnapper.ChangeState(new StateRoaming());
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