using UnityEngine;

namespace VertexSnapper.Helper;

public abstract class MaterialFactory
{
    public static Material CreateUnlitMaterial(Color color)
    {
        Shader shader = Shader.Find("Unlit/Color");
        Material material = new Material(shader)
        {
            color = color
        };


        return material;
    }
}