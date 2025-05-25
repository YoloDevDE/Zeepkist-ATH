using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Master.StateMachine;
using BepInEx;
using HarmonyLib;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ZeepSDK")]
public class Plugin : BaseUnityPlugin
{
    private const string ConfigCategoryGeneral = "General";

    private Harmony _harmony;
    private IStateMachine _masterStateMachine;

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
    public PluginConfig Config { get; private set; }

    private void Awake()
    {
        InitializeConfig();
        InitializeHarmony();
        RegisterChatCommands();
        InitializeStateMachine();

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void Start()
    {
        InitializeServices();
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        _harmony = null;
    }

    private void InitializeConfig()
    {
        Config = new PluginConfig(base.Config);
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
        _masterStateMachine = new MasterStateMachine();
        _masterStateMachine.TransitionTo(_masterStateMachine.InitialState);
    }

    private void InitializeServices()
    {
        // Initialize singleton services
        _ = GraphQLService.Instance;
        _ = RandomLevelService.Instance;
        _ = PlaylistService.Instance;
    }
}