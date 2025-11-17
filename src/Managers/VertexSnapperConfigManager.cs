using System;
using BepInEx.Configuration;
using UnityEngine;
using ZeepSDK.Messaging;

namespace VertexSnapper.Managers;

public abstract class VertexSnapperConfigManager : IDisposable
{
    private static ConfigFile _config;
    private static readonly KeyCode DefaultVertexKeyBind = KeyCode.T;
    public static ConfigEntry<KeyCode> VertexKeyBind { get; private set; }


    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    public static void Init(ConfigFile config)
    {
        _config = config;

        VertexKeyBind =
            _config.Bind(
                "Keybinds",
                "VertexSnapper Activation KeyBind",
                DefaultVertexKeyBind,
                "Holding down this key enables the vertex snapper"
            );
        _config.SettingChanged += HandleSettingsChanged;
    }

    private static void HandleSettingsChanged(object sender, SettingChangedEventArgs e)
    {
        ResetKeyBindingIfCtrl();
    }

    public static void ResetKeyBindingIfCtrl()
    {
        if (VertexKeyBind.Value is not (KeyCode.LeftControl or KeyCode.RightControl))
        {
            return;
        }

        MessengerApi.LogError($"[Vertexsnapper] <b>[CTRL]-key</b> binding detected.<br>Resetting to default (<b>[{DefaultVertexKeyBind}]-key</b>)", 10f);
        VertexKeyBind.Value = DefaultVertexKeyBind;
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