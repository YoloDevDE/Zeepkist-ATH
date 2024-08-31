using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Master.StateMachine;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ZeepSDK")]
public class Plugin : BaseUnityPlugin
{
    private Harmony _harmony;
    private IStateMachine _masterStateMachine;


    // Declare the ConfigEntry for the "Save Playlist on Run End" option
    public static ConfigEntry<bool> SavePlaylistOnRunEnd { get; set; }

    private void Awake()
    {
        // Initialize the ConfigEntry with a default value of false
        SavePlaylistOnRunEnd = Config.Bind(
            "General", // Category
            "Save Playlist on Run End", // Key
            false, // Default value
            "Literally what it says. what did you expect" // Description
        );

        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();

        ChatCommandApi.RegisterLocalChatCommand<CommandRestart>();
        ChatCommandApi.RegisterLocalChatCommand<CommandStop>();
        ChatCommandApi.RegisterLocalChatCommand<CommandStart>();
        ChatCommandApi.RegisterLocalChatCommand<CommandSkipBroken>();

        _masterStateMachine = new MasterStateMachine();
        _masterStateMachine.TransitionTo(_masterStateMachine.InitialState);
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        // You can now access _savePlaylistOnRunEnd.Value to check if the option is enabled
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        _harmony = null;
    }
}