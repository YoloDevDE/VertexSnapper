using BepInEx.Configuration;
using UnityEngine;

namespace VertexSnapper.Config;

public class VertexSnapperConfig
{
    private static VertexSnapperConfig _instance;
    public static VertexSnapperConfig Instance => _instance ??= new VertexSnapperConfig();

    // Configuration entries for buttons
    public ConfigEntry<KeyCode> SnapperMode { get; private set; }
    public ConfigEntry<KeyCode> Button2Config { get; private set; }

    public void Initialize(ConfigFile config)
    {
        // Bind button configurations
        SnapperMode = config.Bind(
            "Controls",
            "Button1",
            KeyCode.F1,
            "First button keybind"
        );

        Button2Config = config.Bind(
            "Controls",
            "Button2",
            KeyCode.F2,
            "Second button keybind"
        );
    }
}