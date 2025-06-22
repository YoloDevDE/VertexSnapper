using UnityEngine;
using VertexSnapper.Core;

namespace VertexSnapper.Utils;

public class CursorManager
{
    private readonly VertexSnapData data;
    private readonly VertexSnapLogger logger;

    public CursorManager(VertexSnapLogger logger, VertexSnapData data)
    {
        this.logger = logger;
        this.data = data;
    }

    public void CreateCursor()
    {
        logger.LogMethodEntry(nameof(CreateCursor));

        if (data.Cursor != null)
        {
            logger.LogDebug("Cursor already exists");
            logger.LogMethodExit(nameof(CreateCursor));
            return;
        }

        GameObject cursorObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cursorObject.name = "VertexSnapCursor";

        data.Cursor = cursorObject.transform;
        data.Cursor.localScale = Vector3.one * 0.1f;

        // Remove collider to avoid interference
        Collider collider = cursorObject.GetComponent<Collider>();
        if (collider != null)
        {
            Object.Destroy(collider);
        }

        // Set up cursor material
        SetupCursorMaterial(cursorObject);

        logger.LogDebug("Cursor created successfully");
        logger.LogVariableValue("cursor scale", data.Cursor.localScale);
        logger.LogMethodExit(nameof(CreateCursor));
    }

    public void DestroyCursor()
    {
        logger.LogMethodEntry(nameof(DestroyCursor));

        if (data.Cursor != null)
        {
            Object.Destroy(data.Cursor.gameObject);
            data.Cursor = null;
            logger.LogDebug("Cursor destroyed");
        }
        else
        {
            logger.LogDebug("No cursor to destroy");
        }

        logger.LogMethodExit(nameof(DestroyCursor));
    }

    public void MoveCursor(Vector3 position)
    {
        logger.LogMethodEntry(nameof(MoveCursor), $"to {position}");

        if (data.Cursor != null)
        {
            data.Cursor.position = position;
            logger.LogVariableValue("cursor moved to", position);
        }
        else
        {
            logger.LogWarning("Cannot move cursor - cursor is null");
        }

        logger.LogMethodExit(nameof(MoveCursor));
    }

    public bool IsCursorValid()
    {
        bool isValid = data.Cursor != null && data.Cursor.gameObject != null;
        logger.LogVariableValue("cursor valid", isValid);
        return isValid;
    }

    private void SetupCursorMaterial(GameObject cursorObject)
    {
        logger.LogMethodEntry(nameof(SetupCursorMaterial));

        Renderer renderer = cursorObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = Color.yellow;
            material.SetFloat("_Metallic", 0.8f);
            material.SetFloat("_Smoothness", 0.9f);
            renderer.material = material;

            logger.LogDebug("Cursor material applied");
        }
        else
        {
            logger.LogWarning("No renderer found on cursor object");
        }

        logger.LogMethodExit(nameof(SetupCursorMaterial));
    }
}