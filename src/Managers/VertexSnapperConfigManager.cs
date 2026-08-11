using BepInEx.Configuration;
using UnityEngine;
using VertexSnapper.Helper;

namespace VertexSnapper.Managers;

public abstract class VertexSnapperConfigManager
{
	private const KeyCode DefaultVertexKeyBind = KeyCode.T;

	private const KeyCode DefaultNoteWindowKeyBind = KeyCode.F8;

	private const KeyCode DefaultSnapModeCycleKeyBind = KeyCode.Tab;

	// Defaults for hologram colors (using 0-255 scale)
	private static readonly Color DefaultOriginHologramColor = new Color().Primary(); // Cyan
	private static readonly Color DefaultMovingHologramColor = new Color().Warning(); // Yellow
	private static readonly Color DefaultTargetHologramColor = new Color().Secondary(); // Black

	// Default for distance indicator color
	private static readonly Color DefaultDistanceIndicatorColor = new Color().Warning(); // Yellow
	public static ConfigFile Config { get; private set; }

	public static ConfigEntry<KeyCode> VertexKeyBind { get; private set; }
	public static ConfigEntry<KeyCode> ModifierKeyBind { get; private set; }
	public static ConfigEntry<KeyCode> SnapModeCycleKeyBind { get; private set; }
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

	// Diagnostics
	public static ConfigEntry<bool> TraceEnabled { get; private set; }
	public static ConfigEntry<KeyCode> NoteWindowKeyBind { get; private set; }

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

		TraceEnabled =
			Config.Bind(
				"01 General",
				"Write a Trace Log",
				false,
				"Only for bug hunting. Writes every state change, key press and gizmo move to " +
				"BepInEx/config/VertexSnapper.trace.log, and enables the note window."
			);

		SnapModeCycleKeyBind =
			Config.Bind(
				"02 Keybinds",
				"Snap Mode Key",
				DefaultSnapModeCycleKeyBind,
				"While the snapper key is held, this key cycles Point -> Edge -> Face"
			);

		NoteWindowKeyBind =
			Config.Bind(
				"02 Keybinds",
				"Note Window Key",
				DefaultNoteWindowKeyBind,
				"Opens a small box to type a note into the trace log. Only works while the trace log is on"
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
