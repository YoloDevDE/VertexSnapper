using System;
using BepInEx.Configuration;
using UnityEngine;
using ZeepSDK.Settings;

namespace VertexSnapper.Managers;

public abstract class VertexSnapperConfigManager : IDisposable
{
    private const KeyCode DefaultVertexKeyBind = KeyCode.T;

    // Defaults for hologram colors (using 0-255 scale)
    private static readonly Color DefaultOriginHologramColor = new Color(0f / 255f, 255f / 255f, 255f / 255f, 255f / 255f); // Cyan
    private static readonly Color DefaultMovingHologramColor = new Color(255f / 255f, 255f / 255f, 0f / 255f, 255f / 255f); // Yellow
    private static readonly Color DefaultTargetHologramColor = new Color(0f / 255f, 0f / 255f, 0f / 255f, 255f / 255f); // Black

    // Default for distance indicator color
    private static readonly Color DefaultDistanceIndicatorColor = new Color(255f / 255f, 255f / 255f, 0f / 255f, 255f / 255f); // Yellow
    private static ConfigFile _config;

    public static ConfigEntry<KeyCode> VertexKeyBind { get; private set; }
    private static ConfigEntry<bool> ModEnabled { get; set; }
    public static ConfigEntry<bool> SelfSnapEnabled { get; private set; }

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

    // Convenience property so game code only needs a bool
    public static bool IsEnabled => ModEnabled?.Value ?? true;

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    public static void Init(ConfigFile config)
    {
        _config = config;

        ModEnabled =
            _config.Bind(
                "01 VertexSnapper",
                "Enabled",
                true,
                "Enable or disable the VertexSnapper mod"
            );

        SelfSnapEnabled =
            _config.Bind(
                "01 VertexSnapper",
                "Self Snap",
                false,
                "If enabled, the currently selected blocks can be snapped onto themselves"
            );

        VertexKeyBind =
            _config.Bind(
                "02 Keybinds",
                "Activation",
                DefaultVertexKeyBind,
                "Holding down this key enables the vertex snapper"
            );

        // --- Nested-style, ordered sections for holograms ---

        // Origin hologram
        OriginHologramEnabled =
            _config.Bind(
                "03 Holograms and Distance Indicator",
                "01 Origin Hologram Enabled",
                true,
                "If disabled, no wireframe material is applied to the origin hologram"
            );

        OriginHologramColor =
            _config.Bind(
                "03 Holograms and Distance Indicator",
                "01 Origin Hologram Color",
                DefaultOriginHologramColor,
                "Color for the origin hologram"
            );

        // Moving hologram
        MovingHologramEnabled =
            _config.Bind(
                "03 Holograms and Distance Indicator",
                "02 Moving Hologram Enabled",
                true,
                "If disabled, no wireframe material is applied to the moving hologram"
            );

        MovingHologramColor =
            _config.Bind(
                "03 Holograms and Distance Indicator",
                "02 Moving Hologram Color",
                DefaultMovingHologramColor,
                "Color for the moving hologram"
            );

        // Target hologram
        TargetHologramEnabled =
            _config.Bind(
                "03 Holograms and Distance Indicator",
                "03 Target Hologram Enabled",
                true,
                "If disabled, no wireframe material is applied to the target hologram"
            );

        TargetHologramColor =
            _config.Bind(
                "03 Holograms and Distance Indicator",
                "03 Target Hologram Color",
                DefaultTargetHologramColor,
                "Color for the target hologram"
            );

        // --- Distance indicator section ---

        DistanceIndicatorEnabled =
            _config.Bind(
                "03 Holograms and Distance Indicator",
                "04 Distance Indicator Enabled",
                true,
                "If disabled, the distance indicator line and text will not be shown"
            );

        DistanceIndicatorColor =
            _config.Bind(
                "03 Holograms and Distance Indicator",
                "04 Distance Indicator Color",
                DefaultDistanceIndicatorColor,
                "Color for the distance indicator"
            );

        SettingsApi.ModSettingsWindowClosed += HandleSettingsChanged;
    }

    private static void ReleaseUnmanagedResources()
    {
        if (_config != null)
        {
            SettingsApi.ModSettingsWindowClosed -= HandleSettingsChanged;
        }
    }

    private static void HandleSettingsChanged()
    {
        // Logic for when settings are closed
    }

    ~VertexSnapperConfigManager()
    {
        ReleaseUnmanagedResources();
    }
}