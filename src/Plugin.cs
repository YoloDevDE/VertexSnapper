using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using ZeepSDK.LevelEditor;

namespace VertexSnapper;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ZeepSDK")]
public class Plugin : BaseUnityPlugin
{
    private Harmony _harmony;
    private Plugin() { }
    public static Plugin Instance { get; private set; }
    public new ManualLogSource Logger => base.Logger;

    private void Awake()
    {
        Instance = this;
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void Start()
    {
        VertexSnapperConfigManager.Init(Config);
        LevelEditorApi.EnteredLevelEditor += HandleEnteredLevelEditor;
        LevelEditorApi.ExitedLevelEditor += HandleExitedLevelEditor;
    }


    private void OnDestroy()
    {
        LevelEditorApi.EnteredLevelEditor -= HandleEnteredLevelEditor;
        LevelEditorApi.ExitedLevelEditor -= HandleExitedLevelEditor;
        _harmony?.UnpatchSelf();
        _harmony = null;
    }


    private void HandleExitedLevelEditor()
    {
        VertexSnapper vertexSnapper = FindObjectOfType<VertexSnapper>();
        Destroy(vertexSnapper);
    }

    private void HandleEnteredLevelEditor()
    {
        GameObject vertexSnapper = new GameObject("VertexSnapper");
        vertexSnapper.AddComponent<VertexSnapper>();
    }
}