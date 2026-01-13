using FMODSyntax;
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
        KeyInputManager.AnyScroll += OnAnyScroll;

        LevelEditorApi.BlockMouseInput(this);
        LevelEditorApi.BlockKeyboardInput(this);

        VertexSnapper.CacheAndRemoveBlockSelection();
        if (VertexSnapperConfigManager.OriginHologramEnabled.Value)
        {
            // VertexSnapper.CacheOriginalMaterials(VertexSnapper.BlockSelectionCache, VertexSnapper.BlockSelectionMaterials);
            VertexSnapper.ApplyWireframeMaterial(
                VertexSnapper.BlockSelectionCache,
                VertexSnapperConfigManager.OriginHologramColor.Value
            );
        }

        MessengerApi.Log("[Vertexsnapper] Im gonna snap! <sprite=\"moremojis\" name=\"ZaagBladPadRood2\">", 0.6f);
    }


    public void Exit()
    {
        if (VertexSnapperConfigManager.OriginHologramEnabled.Value)
        {
            VertexSnapper.RestoreOriginalMaterials(VertexSnapper.BlockSelectionMaterials);
        }

        // Info: how to abort with middle mouse button (no warning)
        MessengerApi.Log(
            "[Vertexsnapper] You can abort vertex snapping anytime with the <#f00>middle mouse button</color> or <#f00>ESC</color>.",
            5f
        );

        KeyInputManager.OnKeyUp[VertexSnapperConfigManager.VertexKeyBind.Value] -= ChangeStateToAbort;
        KeyInputManager.OnMouseDown[0] -= TryChangeStateToRoaming;
        KeyInputManager.AnyScroll -= OnAnyScroll;

        LevelEditorApi.UnblockMouseInput(this);
        LevelEditorApi.UnblockKeyboardInput(this);
    }

    public void Update()
    {
        if (RaycastUtils.IsSphereCastOnBlockSuccessful(VertexSnapper.MainCamera, out RaycastHit hit, VertexSnapper.BlockSelectionCache))
        {
            if (!VertexSnapper.FirstCursor)
            {
                VertexSnapper.FirstCursor = CursorFactory.CreateCursor(
                    "FirstCursor",
                    MaterialFactory.CreateUnlitMaterial(Color.magenta),
                    VertexSnapper.gameObject,
                    VertexSnapper.CubeScaleFactor
                );
            }

            Vector3 closestVertexPosition = VertexSnapper.FindClosestVertexToHit(hit);
            if (VertexSnapper.FirstCursor.transform.position == closestVertexPosition)
            {
                return;
            }

            AudioEvents.MenuHover1.Play();
            VertexSnapper.FirstCursor.transform.position = closestVertexPosition;

            return;
        }

        VertexSnapper.SafeDestroy(VertexSnapper.FirstCursor);
    }

    private void OnAnyScroll(float delta)
    {
        if (!VertexSnapper.FirstCursor)
        {
            return;
        }

        const float scaleSpeed = 0.2f;
        Transform t = VertexSnapper.FirstCursor.transform;
        VertexSnapper.CubeSize = t.localScale;

        // Apply delta (uniform scaling)
        float factor = 1f + delta * scaleSpeed;
        VertexSnapper.CubeSize *= factor;


        VertexSnapper.CubeScaleFactor = Mathf.Clamp(VertexSnapper.CubeSize.x, 0.05f, 5f);
        VertexSnapper.CubeSize = new Vector3(
            VertexSnapper.CubeScaleFactor,
            VertexSnapper.CubeScaleFactor,
            VertexSnapper.CubeScaleFactor
        );

        t.localScale = VertexSnapper.CubeSize;
    }


    private void TryChangeStateToRoaming()
    {
        if (!OriginIsValid())
        {
            MessengerApi.LogWarning(
                $"[Vertexsnapper] No Vertex selected!<br><align=left><indent=15%>To select a vertex, hold down <#f00>[{VertexSnapperConfigManager.VertexKeyBind.Value}]</color> while hovering over the <b>block selection</b>.<br>To confirm, press the <#f00>left mouse button</color>.</align>",
                10f);
            AudioEvents.Blarghl.Play();
            return;
        }

        AudioEvents.MenuClick.Play();
        VertexSnapper.ChangeState(new StateRoaming());
    }

    private bool OriginIsValid() => VertexSnapper && VertexSnapper.FirstCursor;

    private void ChangeStateToAbort()
    {
        VertexSnapper.ChangeState(new StateAbort());
    }
}