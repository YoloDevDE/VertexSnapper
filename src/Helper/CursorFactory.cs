using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

namespace VertexSnapper.Helper;

public abstract class CursorFactory
{
    public static GameObject CreateCursor(
        string cursorName,
        [NotNull] Material material,
        GameObject parent = null,
        float scale = 0.5f)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Renderer renderer = go.GetComponent<Renderer>();

        go.transform.localScale = Vector3.one * scale;

        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sharedMaterial = material;

        go.name = cursorName;
        go.layer = 2;
        if (parent)
        {
            go.transform.SetParent(parent.transform);
        }

        return go;
    }
}