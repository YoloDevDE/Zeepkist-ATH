using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Configs;
using AuthorTimeHunting.Service;
using BepInEx;
using HarmonyLib;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ZeepSDK")]
public class Plugin : BaseUnityPlugin
{
    private Harmony _harmony;
    private StateMachine _modStateMachine;

    private int gamestate = -1;

    private Plugin()
    {
        Util.Logger.Initialize(Logger);
        Instance = this;
    }

    /// <summary>
    ///     Singleton instance of the plugin
    /// </summary>
    public static Plugin Instance { get; private set; }

    /// <summary>
    ///     Configuration settings for the plugin
    /// </summary>
    public PluginConfig MyConfig { get; private set; }

    private void Awake()
    {
        InitializeConfig();
        InitializeHarmony();

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void Start()
    {
        InitializeServices();
        RegisterChatCommands();
        InitializeStateMachine();
    }

    private void Update()
    {
        _modStateMachine?.Update();
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        _harmony = null;
    }

    private void InitializeConfig()
    {
        MyConfig = new PluginConfig(Config);
    }

    private void InitializeHarmony()
    {
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();
    }

    private void RegisterChatCommands()
    {
        ChatCommandApi.RegisterLocalChatCommand<CommandRestart>();
        ChatCommandApi.RegisterLocalChatCommand<CommandStop>();
        ChatCommandApi.RegisterLocalChatCommand<CommandStart>();
        ChatCommandApi.RegisterLocalChatCommand<CommandSkipBroken>();
    }

    private void InitializeStateMachine()
    {
        _modStateMachine = ModStateMachineConfigurator.ModStateMachine();
        _modStateMachine.Start();
    }

    private void InitializeServices()
    {
        // Initialize singleton services
        _ = GraphQLService.Instance;
        _ = RandomLevelService.Instance;
        _ = PlaylistService.Instance;
    }
}