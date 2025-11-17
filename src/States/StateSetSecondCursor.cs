using System.Linq;
using UnityEngine;
using VertexSnapper.Components;
using VertexSnapper.Helper;
using VertexSnapper.Managers;
using ZeepSDK.LevelEditor;

namespace VertexSnapper.States;

public class StateSetSecondCursor : IVertexSnapperState<VertexSnapper>
{
    // Merkt sich den aktuell gehighlighteten Block
    private BlockProperties _currentTargetBlock;
    public VertexSnapper VertexSnapper { get; set; }

    public void Enter()
    {
        KeyInputManager.OnKeyUp[VertexSnapperConfigManager.VertexKeyBind.Value] += ChangeStateToRoaming;
        KeyInputManager.OnMouseDown[0] += InvokeSnapProcess;
        KeyInputManager.OnMouseDown[2] += ChangeStateToAbort;
        LevelEditorApi.BlockMouseInput(this);
    }

    public void Exit()
    {
        CleanUpResources();

        KeyInputManager.OnKeyUp[VertexSnapperConfigManager.VertexKeyBind.Value] -= ChangeStateToRoaming;
        KeyInputManager.OnMouseDown[2] -= ChangeStateToAbort;
        KeyInputManager.OnMouseDown[0] -= InvokeSnapProcess;
        LevelEditorApi.UnblockMouseInput(this);
    }

    public void Update()
    {
        if (!VertexSnapper.Hologram)
        {
            VertexSnapper.Hologram = VertexSnapper.CreateHologram(
                VertexSnapper.BlockSelectionCache.Select(b => b.gameObject),
                WireframeBundleLoader.WireframeMaterial,
                Color.yellow);
            VertexSnapper.Hologram.layer = LayerMask.NameToLayer("Ignore Raycast");
            VertexSnapper.CreateAnchorPoint(
                VertexSnapper.Hologram,
                VertexSnapper.HologramOffsets,
                VertexSnapper.FirstCursor.transform);
        }

        if (RaycastUtils.IsSphereCastOnBlockSuccessful(VertexSnapper.MainCamera, out RaycastHit hit))
        {
            // Versuche, den Block unter dem Hit zu bekommen
            BlockProperties block;
            RaycastUtils.TryGetBlocksFromHit(hit, out block);

            // Wenn sich der Zielblock geändert hat: komplett aufräumen
            if (block != _currentTargetBlock)
            {
                CleanUpResources();
                _currentTargetBlock = block;
            }

            // SecondCursor + Highlight für aktuellen Block anlegen
            if (!VertexSnapper.SecondCursor)
            {
                VertexSnapper.SecondCursor = CursorFactory.CreateCursor(
                    "SecondCursor",
                    MaterialFactory.CreateUnlitMaterial(Color.magenta),
                    VertexSnapper.gameObject);

                if (block)
                {
                    VertexSnapper.CacheOriginalMaterials([block], VertexSnapper.TargetBlockMaterials);
                    VertexSnapper.ApplyWireframeMaterial([block], Color.gray);
                }
            }

            VertexSnapper.SecondCursor.transform.position = VertexSnapper.FindClosestVertexToHit(hit);
            DistanceIndicator.Show(
                VertexSnapper.FirstCursor.transform.position,
                VertexSnapper.SecondCursor.transform.position);

            VertexSnapper.MoveHologramToCursor(VertexSnapper.SecondCursor.transform.position);
        }
        else
        {
            // Kein Treffer mehr: alles zurücksetzen
            CleanUpResources();
            _currentTargetBlock = null;
        }
    }

    private void CleanUpResources()
    {
        VertexSnapper.RestoreOriginalMaterials(VertexSnapper.TargetBlockMaterials);
        VertexSnapper.SafeDestroy(VertexSnapper.SecondCursor);
        VertexSnapper.SafeDestroy(VertexSnapper.Hologram);
        DistanceIndicator.DestroyIndicator();
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