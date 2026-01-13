using System;
using System.Globalization;
using BepInEx.Configuration;
using UnityEngine;
using ZeepSDK.Messaging;
using ZeepSDK.Settings;

namespace VertexSnapper.Managers;

public abstract class VertexSnapperConfigManager : IDisposable
{
    // Defaults for hologram colors (hex RGB, no alpha)
    private const string DefaultOriginHologramColorHex = "00FFFF"; // green
    private const string DefaultMovingHologramColorHex = "FFFF00"; // cyan
    private const string DefaultTargetHologramColorHex = "000000"; // red

    // Default for distance indicator color
    private const string DefaultDistanceIndicatorColorHex = "FFFF00"; // white
    private static ConfigFile _config;

    private static readonly KeyCode DefaultVertexKeyBind = KeyCode.T;

    public static ConfigEntry<KeyCode> VertexKeyBind { get; private set; }
    public static ConfigEntry<bool> ModEnabled { get; private set; }
    public static ConfigEntry<bool> SelfSnapEnabled { get; private set; }

    // Hologram enable toggles
    public static ConfigEntry<bool> OriginHologramEnabled { get; private set; }
    public static ConfigEntry<bool> MovingHologramEnabled { get; private set; }
    public static ConfigEntry<bool> TargetHologramEnabled { get; private set; }

    // Distance indicator toggle
    public static ConfigEntry<bool> DistanceIndicatorEnabled { get; private set; }

    // Hex RGB strings (RRGGBB or #RRGGBB)
    public static ConfigEntry<string> OriginHologramColorHex { get; private set; }
    public static ConfigEntry<string> MovingHologramColorHex { get; private set; }
    public static ConfigEntry<string> TargetHologramColorHex { get; private set; }
    public static ConfigEntry<string> DistanceIndicatorColorHex { get; private set; }

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
                "If enabled, the selected blocks can be snapped onto themselves"
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

        OriginHologramColorHex =
            _config.Bind(
                "03 Holograms and Distance Indicator",
                "01 Origin Hologram Color",
                DefaultOriginHologramColorHex,
                "Hex RGB color for the origin hologram (RRGGBB or #RRGGBB)"
            );

        // Moving hologram
        MovingHologramEnabled =
            _config.Bind(
                "03 Holograms and Distance Indicator",
                "02 Moving Hologram Enabled",
                true,
                "If disabled, no wireframe material is applied to the moving hologram"
            );

        MovingHologramColorHex =
            _config.Bind(
                "03 Holograms and Distance Indicator",
                "02 Moving Hologram Color",
                DefaultMovingHologramColorHex,
                "Hex RGB color for the moving hologram (RRGGBB or #RRGGBB)"
            );

        // Target hologram
        TargetHologramEnabled =
            _config.Bind(
                "03 Holograms and Distance Indicator",
                "03 Target Hologram Enabled",
                true,
                "If disabled, no wireframe material is applied to the target hologram"
            );

        TargetHologramColorHex =
            _config.Bind(
                "03 Holograms and Distance Indicator",
                "03 Target Hologram Color",
                DefaultTargetHologramColorHex,
                "Hex RGB color for the target hologram (RRGGBB or #RRGGBB)"
            );

        // --- Distance indicator section ---

        DistanceIndicatorEnabled =
            _config.Bind(
                "03 Holograms and Distance Indicator",
                "04 Distance Indicator Enabled",
                true,
                "If disabled, the distance indicator line and text will not be shown"
            );

        DistanceIndicatorColorHex =
            _config.Bind(
                "03 Holograms and Distance Indicator",
                "04 Distance Indicator Color",
                DefaultDistanceIndicatorColorHex,
                "Hex RGB color for the distance indicator (RRGGBB or #RRGGBB)"
            );
        ValidateHologramColors();

        SettingsApi.ModSettingsWindowClosed += HandleSettingsChanged;
    }


   

    /// <summary>
    ///     Ensures all color entries are valid hex RGB. If not, they are reset to defaults.
    /// </summary>
    private static void ValidateHologramColors()
    {
        ValidateColorEntry(OriginHologramColorHex, DefaultOriginHologramColorHex, "Origin Hologram");
        ValidateColorEntry(MovingHologramColorHex, DefaultMovingHologramColorHex, "Moving Hologram");
        ValidateColorEntry(TargetHologramColorHex, DefaultTargetHologramColorHex, "Target Hologram");
        ValidateColorEntry(DistanceIndicatorColorHex, DefaultDistanceIndicatorColorHex, "Distance Indicator");
    }

    private static void ValidateColorEntry(ConfigEntry<string> entry, string defaultHex, string displayName)
    {
        if (entry == null)
        {
            return;
        }

        string raw = entry.Value ?? string.Empty;
        string normalized = raw.Trim().ToUpperInvariant();

        if (normalized.StartsWith("#"))
        {
            normalized = normalized.Substring(1);
        }

        bool isValid =
            normalized.Length == 6 &&
            int.TryParse(normalized, NumberStyles.HexNumber, null, out _);

        if (isValid)
        {
            // Store back normalized (with leading '#') to keep the config clean
            string formatted = "#" + normalized;
            if (entry.Value != formatted)
            {
                entry.Value = formatted;
                _config.Save();
            }


            return;
        }

        // Invalid -> reset to default and log an error
        string defaultFormatted = "#" + defaultHex;
        MessengerApi.LogError(
            $"[Vertexsnapper] Invalid hex color for <b>{displayName}</b>: \"{raw}\".<br>" +
            $"Expected format: <b>RRGGBB</b> or <b>#RRGGBB</b>. Resetting to <b>{defaultFormatted}</b>.",
            10f);

        entry.Value = defaultFormatted;
        _config.Save();
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
        // Validate color entries whenever settings change
        ValidateHologramColors();
    }

    ~VertexSnapperConfigManager()
    {
        ReleaseUnmanagedResources();
    }
}