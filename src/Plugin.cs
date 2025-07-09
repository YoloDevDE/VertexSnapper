using BepInEx;
using HarmonyLib;
using VertexSnapper.Config;
using VertexSnapper.Input;
using VertexSnapper.States;

namespace VertexSnapper;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private Plugin()
    {
        Util.Logger.Initialize(Logger);
        Instance = this;
    }

    public static Plugin Instance { get; private set; }
    public Harmony Harmony { get; set; }
    public GameStateMachine GameStateMachine { get; set; }
    public VertexSnapperConfig PluginConfig { get; private set; }

    private void Awake()
    {
        // Initialize configuration first
        PluginConfig = VertexSnapperConfig.Instance;
        PluginConfig.Initialize(Config);

        Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        Harmony.PatchAll();
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void Start()
    {
        Instance = this;
        GameStateMachine = new GameStateMachine();
        GameStateMachine.ChangeState(new GameStateNotInEditor());
    }

    private void Update()
    {
        KeyInput.Update();
    }

    private void OnDestroy()
    {
        Harmony?.UnpatchSelf();
        Harmony = null;
    }
}