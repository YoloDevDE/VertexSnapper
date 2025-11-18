using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using VertexSnapper.Managers;
using ZeepSDK.LevelEditor;

// using <YourNamespace>; // where PlayerManager lives

namespace VertexSnapper;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ZeepSDK")]
public class Plugin : BaseUnityPlugin
{
    private bool _configInitialized;
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

        // Subscribe to Unity's sceneLoaded event
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        // Config is now initialized in HandleSceneLoaded when 3D_MainMenu loads
        LevelEditorApi.EnteredLevelEditor += HandleEnteredLevelEditor;
        LevelEditorApi.ExitedLevelEditor += HandleExitedLevelEditor;
    }

    private void OnDestroy()
    {
        LevelEditorApi.EnteredLevelEditor -= HandleEnteredLevelEditor;
        LevelEditorApi.ExitedLevelEditor -= HandleExitedLevelEditor;

        SceneManager.sceneLoaded -= HandleSceneLoaded;

        _harmony?.UnpatchSelf();
        _harmony = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_configInitialized)
        {
            return;
        }

        if (scene.name != "3D_MainMenu")
        {
            return;
        }

        VertexSnapperConfigManager.Init(Config);
        _configInitialized = true;

        Logger.LogInfo("[VertexSnapper] Config initialized on scene: 3D_MainMenu");
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