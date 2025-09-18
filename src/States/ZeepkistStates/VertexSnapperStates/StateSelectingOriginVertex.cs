using UnityEngine;
using VertexSnapper.Config;
using VertexSnapper.Input;
using VertexSnapper.Interfaces;
using VertexSnapper.Managers;
using ZeepSDK.LevelEditor;
using ZeepSDK.Messaging;

namespace VertexSnapper.States.ZeepkistStates.VertexSnapperStates;

public class StateSelectingOriginVertex : IState
{
    private GameObject raycastManagerObj;
    private GameObject vertexSphereManagerObj;
    public IStateMachine StateMachine { get; set; }

    public void Enter(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
        LevelEditorApi.BlockMouseInput(this);
        LevelEditorApi.BlockKeyboardInput(this);
        // Manager aktivieren
        raycastManagerObj = new GameObject("RaycastManager");
        raycastManagerObj.AddComponent<RaycastManager>();


        KeyInput.GetKey(KeyCode.Mouse0).OnKeyDown += HandleMouseButtonDown;
        KeyInput.GetKey(VertexSnapperConfig.Instance.SnapperMode.Value).OnKeyUp += HandleSnapperModeKeyUp;
        KeyInput.GetKey(VertexSnapperConfig.Instance.SnapperMode.Value).OnKeyHold += HandleSnapperModeKeyHold;
        ;
    }

    public void Exit()
    {
        // Manager deaktivieren
        if (raycastManagerObj != null)
        {
            Object.Destroy(raycastManagerObj);
            raycastManagerObj = null;
        }

        LevelEditorApi.UnblockMouseInput(this);
        LevelEditorApi.UnblockKeyboardInput(this);
        KeyInput.GetKey(KeyCode.Mouse0).OnKeyDown -= HandleMouseButtonDown;
        KeyInput.GetKey(VertexSnapperConfig.Instance.SnapperMode.Value).OnKeyUp -= HandleSnapperModeKeyUp;
        KeyInput.GetKey(VertexSnapperConfig.Instance.SnapperMode.Value).OnKeyHold -= HandleSnapperModeKeyHold;
        ;
    }

    private void HandleSnapperModeKeyHold()
    {
        RaycastManager raycastManager = raycastManagerObj.GetComponent<RaycastManager>();
        BlockProperties currentBlockProperties = raycastManager?.CurrentHitRootBlockProperties;
        if (currentBlockProperties == null)
        {
            return;
        }

        foreach (MeshFilter currentMeshFilter in currentBlockProperties.GetComponentsInChildren<MeshFilter>())
        {
            Mesh currentMesh = currentMeshFilter.sharedMesh;
            if (!currentMesh)
            {
                continue;
            }

            Vector3[] vertices = currentMesh.vertices;
            Transform currentTransform = currentMeshFilter.transform;

            foreach (Vector3 vertex in vertices)
            {
                Vector3 worldPos = currentTransform.TransformPoint(vertex); // local -> world
                VertexSphereManager.Instance.CreateSphereAt(worldPos);
            }
        }
    }


    private void HandleSnapperModeKeyUp()
    {
        MessengerApi.Log("SnapperModeKeyUp");
        VertexSphereManager.Instance.DestroyAllSpheres();
        StateMachine.ChangeState(new StateIdle());
    }

    private void HandleMouseButtonDown()
    {
        MessengerApi.Log("MouseButtonDown");
    }
}