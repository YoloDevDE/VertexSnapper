using BepInEx.Configuration;
using UnityEngine;
using VertexSnapper.Helper;

namespace VertexSnapper.Managers;

public abstract class VertexSnapperConfigManager
{
    private const KeyCode DefaultVertexKeyBind = KeyCode.T;

    // Defaults for hologram colors (using 0-255 scale)
    private static readonly Color DefaultOriginHologramColor = new Color().Primary(); // Cyan
    private static readonly Color DefaultMovingHologramColor = new Color().Warning(); // Yellow
    private static readonly Color DefaultTargetHologramColor = new Color().Secondary(); // Black

    // Default for distance indicator color
    private static readonly Color DefaultDistanceIndicatorColor = new Color().Warning(); // Yellow
    public static ConfigFile Config { get; private set; }

    public static ConfigEntry<KeyCode> VertexKeyBind { get; private set; }
    public static ConfigEntry<KeyCode> ModifierKeyBind { get; private set; }
    private static ConfigEntry<bool> ModEnabled { get; set; }
    public static ConfigEntry<bool> SoundEnabled { get; private set; }

    // Hologram enable toggles
    public static ConfigEntry<bool> OriginHologramEnabled { get; private set; }
    public static ConfigEntry<bool> MovingHologramEnabled { get; private set; }
    public static ConfigEntry<bool> TargetHologramEnabled { get; private set; }

    // Distance indicator toggle
    public static ConfigEntry<bool> DistanceIndicatorEnabled { get; private set; }

    // Color entries
    public static ConfigEntry<Color> OriginHologramColor { get; private set; }
    public static ConfigEntry<Color> MovingHologramColor { get; private set; }
    public static ConfigEntry<Color> TargetHologramColor { get; private set; }
    public static ConfigEntry<Color> DistanceIndicatorColor { get; private set; }

    // Convenience properties
    public static bool IsEnabled => ModEnabled?.Value ?? true;
    public static bool IsModifierPressed => Input.GetKey(ModifierKeyBind.Value) || ModifierKeyBind.Value == KeyCode.None;

    public static void Init(ConfigFile config)
    {
        Config = config;

        ModEnabled =
            Config.Bind(
                "01 General",
                "Active",
                true,
                "Enable or disable the VertexSnapper mod"
            );


        SoundEnabled =
            Config.Bind(
                "01 General",
                "Cool Sounds",
                true,
                "Enable or disable cool sound effects for the VertexSnapper (Uncool if turned off)"
            );

        VertexKeyBind =
            Config.Bind(
                "02 Keybinds",
                "Snapper Key",
                DefaultVertexKeyBind,
                "Holding down this key enables the \"Vertexsnapper\""
            );

        ModifierKeyBind =
            Config.Bind(
                "02 Keybinds",
                "Modifier Key",
                KeyCode.LeftShift,
                "If you wanna snap onto the selection itself, press this key while holding down the snapper key"
            );

        // --- Nested-style, ordered sections for holograms ---

        // Origin hologram
        OriginHologramEnabled =
            Config.Bind(
                "03 Holograms and Distance Indicator",
                "01 Origin Hologram Enabled",
                true,
                "If disabled, no wireframe material is applied to the origin hologram"
            );

        OriginHologramColor =
            Config.Bind(
                "03 Holograms and Distance Indicator",
                "01 Origin Hologram Color",
                DefaultOriginHologramColor,
                "Color for the origin hologram"
            );

        // Moving hologram
        MovingHologramEnabled =
            Config.Bind(
                "03 Holograms and Distance Indicator",
                "02 Moving Hologram Enabled",
                true,
                "If disabled, no wireframe material is applied to the moving hologram"
            );

        MovingHologramColor =
            Config.Bind(
                "03 Holograms and Distance Indicator",
                "02 Moving Hologram Color",
                DefaultMovingHologramColor,
                "Color for the moving hologram"
            );

        // Target hologram
        TargetHologramEnabled =
            Config.Bind(
                "03 Holograms and Distance Indicator",
                "03 Target Hologram Enabled",
                true,
                "If disabled, no wireframe material is applied to the target hologram"
            );

        TargetHologramColor =
            Config.Bind(
                "03 Holograms and Distance Indicator",
                "03 Target Hologram Color",
                DefaultTargetHologramColor,
                "Color for the target hologram"
            );

        // --- Distance indicator section ---

        DistanceIndicatorEnabled =
            Config.Bind(
                "03 Holograms and Distance Indicator",
                "04 Distance Indicator Enabled",
                true,
                "If disabled, the distance indicator line and text will not be shown"
            );

        DistanceIndicatorColor =
            Config.Bind(
                "03 Holograms and Distance Indicator",
                "04 Distance Indicator Color",
                DefaultDistanceIndicatorColor,
                "Color for the distance indicator"
            );
    }
}