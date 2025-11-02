// C#

using UnityEngine;

namespace VertexSnapper;

public static class WireframeBundleLoader
{
    private static AssetBundle _bundle;

    public static Material WireframeMaterial { get; private set; }

    // Call once (e.g., on entering editor/tools mode)
    public static bool InitWireframeMaterial(string bundlePath)
    {
        if (_bundle)
        {
            return true;
        }

        // Try with and without extension
        _bundle = AssetBundle.LoadFromFile(bundlePath);
        if (!_bundle)
        {
            _bundle = AssetBundle.LoadFromFile(bundlePath + ".assetbundle");
        }

        if (!_bundle)
        {
            _bundle = AssetBundle.LoadFromFile(bundlePath + ".bundle");
        }

        if (!_bundle)
        {
            Debug.LogError("[Wireframe] Failed to load AssetBundle at: " + bundlePath);
            return false;
        }

        // Option A: material exported (recommended)
        if (!WireframeMaterial)
        {
            // If you know the exact asset name, use: _bundle.LoadAsset<Material>("WireframeMat");
            foreach (Material mat in _bundle.LoadAllAssets<Material>())
            {
                WireframeMaterial = mat;
                break;
            }
        }

        // Option B: only a shader exported
        if (!WireframeMaterial)
        {
            Shader wireShader = null;
            foreach (Shader sh in _bundle.LoadAllAssets<Shader>())
            {
                wireShader = sh;
                break;
            }

            if (wireShader)
            {
                WireframeMaterial = new Material(wireShader) { name = "Wireframe_Material_Runtime" };
            }
        }

        if (!WireframeMaterial)
        {
            Debug.LogError("[Wireframe] No Material or Shader found in bundle.");
            return false;
        }


        if (!WireframeMaterial)
        {
            return false;
        }

        WireframeMaterial = new Material(WireframeMaterial);
        return true;
    }
}