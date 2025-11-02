using System;
using BepInEx.Configuration;
using UnityEngine;
using ZeepSDK.Messaging;

namespace VertexSnapper;

public abstract class VertexSnapperConfigManager : IDisposable
{
    private static ConfigFile _config;
    public static ConfigEntry<double> VertexSnapperSphereRadius { get; private set; }
    public static ConfigEntry<KeyCode> VertexKeyBind { get; private set; }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    public static void Init(ConfigFile config)
    {
        _config = config;
        VertexSnapperSphereRadius =
            _config.Bind(
                "General",
                "Vertex Sphere Radius",
                0.5
            );

        VertexKeyBind =
            _config.Bind(
                "Keybinds",
                "VertexSnapper Activation KeyBind",
                KeyCode.LeftAlt,
                "Holding down this key enables the vertex snapper (DO NOT USE 'CTRL'"
            );
        _config.SettingChanged += HandleSettingsChanged;
    }

    private static void HandleSettingsChanged(object sender, SettingChangedEventArgs e)
    {
        ResetKeyBindingIfCtrl();
    }

    private static void ResetKeyBindingIfCtrl()
    {
        if (VertexKeyBind.Value is not (KeyCode.LeftControl or KeyCode.RightControl))
        {
            return;
        }

        MessengerApi.LogError("[Vertexsnapper] <b>[CTRL]-key</b> binding detected.<br>Resetting to default (<b>[LEFT_ALT]-key</b>)", 10f);
        VertexKeyBind.Value = KeyCode.LeftAlt;
        _config.Save();
    }

    private static void ReleaseUnmanagedResources()
    {
        _config.SettingChanged -= HandleSettingsChanged;
    }

    ~VertexSnapperConfigManager()
    {
        ReleaseUnmanagedResources();
    }
}