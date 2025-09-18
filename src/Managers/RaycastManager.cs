using UnityEngine;
using ZeepSDK.Messaging;

namespace VertexSnapper.Managers;

public class RaycastManager : MonoBehaviour
{
    private Camera camera;
    private RaycastHit currentHit;

    public BlockProperties CurrentHitRootBlockProperties => currentHit.collider.GetComponentInParent<BlockProperties>();

    private void Start()
    {
        camera = Camera.main;
    }

    private void Update()
    {
        Ray ray = camera.ScreenPointToRay(UnityEngine.Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            return;
        }

        if (currentHit.collider == hit.collider)
        {
            return;
        }

        currentHit = hit;

        // Root suchen mit BlockProperties
        BlockProperties block = hit.collider.GetComponentInParent<BlockProperties>();
        if (block != null)
        {
            GameObject root = block.gameObject;
            Component[] comps = root.GetComponents<Component>();

            string output = $"Hit Root: {root.name}\n";
            foreach (Component comp in comps)
            {
                if (comp)
                {
                    output += $" - {comp.GetType()}\n";
                }
            }

            MessengerApi.Log(output.TrimEnd());
        }
        else
        {
            MessengerApi.Log($"Hit: {hit.collider.name} (kein BlockProperties gefunden)");
        }
    }
}