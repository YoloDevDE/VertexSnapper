using BepInEx.Configuration;
using UnityEngine;

namespace VertexSnapper.Config;

public class VertexSnapperConfig
{
    private static VertexSnapperConfig _instance;
    public static VertexSnapperConfig Instance => _instance ??= new VertexSnapperConfig();

    // Configuration entries for buttons
    public ConfigEntry<KeyCode> SnapperMode { get; private set; }
    public ConfigEntry<KeyCode> VertexMode { get; private set; }

    public void Initialize(ConfigFile config)
    {
        // Bind button configurations
        SnapperMode = config.Bind(
            "Controls",
            "SnapperMode",
            KeyCode.F1,
            "Snapper mode keybind"
        );

        VertexMode = config.Bind(
            "Controls",
            "VertexMode",
            KeyCode.LeftControl,
            "Vertex mode keybind for snapping objects"
        );
    }
}