using System.Collections.Generic;
using System.Linq;
using FMODSyntax;
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
        KeyInputManager.OnMouseDown[0] -= InvokeSnapProcess;

        KeyInputManager.OnMouseDown[2] -= ChangeStateToAbort;
        LevelEditorApi.UnblockMouseInput(this);
    }

    public void Update()
    {
        List<BlockProperties> disallowedBlocks = VertexSnapperConfigManager.SelfSnapEnabled.Value ? null : VertexSnapper.BlockSelectionCache;
        if (RaycastUtils.IsSphereCastOnBlockSuccessful(VertexSnapper.MainCamera, out RaycastHit hit, null, disallowedBlocks))
        {
            // Versuche, den Block unter dem Hit zu bekommen
            RaycastUtils.TryGetBlocksFromHit(hit, out BlockProperties block);

            // Wenn sich der Zielblock geändert hat: komplett aufräumen
            if (block != _currentTargetBlock)
            {
                CleanUpResources();
                _currentTargetBlock = block;
                AudioEvents.MenuHoverDisabled.PlayIfEnabled();
            }

            if (!VertexSnapper.Hologram && VertexSnapperConfigManager.MovingHologramEnabled.Value)
            {
                VertexSnapper.Hologram = VertexSnapper.CreateHologram(
                    VertexSnapper.BlockSelectionCache.Select(b => b.gameObject),
                    WireframeBundleLoader.WireframeMaterial,
                    VertexSnapperConfigManager.MovingHologramColor.Value);
                VertexSnapper.Hologram.layer = LayerMask.NameToLayer("Ignore Raycast");
                VertexSnapper.CreateAnchorPoint(
                    VertexSnapper.Hologram,
                    VertexSnapper.HologramOffsets,
                    VertexSnapper.FirstCursor.transform);
            }

            // SecondCursor + Highlight für aktuellen Block anlegen
            if (!VertexSnapper.SecondCursor)
            {
                VertexSnapper.SecondCursor = CursorFactory.CreateCursor(
                    "SecondCursor",
                    MaterialFactory.CreateUnlitMaterial(Color.magenta),
                    VertexSnapper.gameObject);

                if (block && VertexSnapperConfigManager.TargetHologramEnabled.Value)
                {
                    VertexSnapper.CacheOriginalMaterials([block], VertexSnapper.TargetBlockMaterials);
                    VertexSnapper.ApplyWireframeMaterial(
                        [block],
                        VertexSnapperConfigManager.TargetHologramColor.Value);
                }
            }

            Vector3 closestVertexPosition = VertexSnapper.FindClosestVertexToHit(hit);
            if (VertexSnapper.SecondCursor.transform.position != closestVertexPosition)
            {
                AudioEvents.MenuHover1.PlayIfEnabled();
                VertexSnapper.SecondCursor.transform.position = closestVertexPosition;
                if (VertexSnapperConfigManager.DistanceIndicatorEnabled.Value)
                {
                    DistanceIndicator.Show(
                        VertexSnapper.FirstCursor.transform.position,
                        VertexSnapper.SecondCursor.transform.position);
                }

                if (VertexSnapperConfigManager.TargetHologramEnabled.Value)
                {
                    VertexSnapper.MoveHologramToCursor(closestVertexPosition);
                }
            }
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
            ChangeStateToCleanUp();
        }
    }

    private void ChangeStateToCleanUp()
    {
        VertexSnapper.ChangeState(new StateCleanUp());
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