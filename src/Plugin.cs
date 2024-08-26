using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Master.StateMachine;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ZeepSDK")]
public class Plugin : BaseUnityPlugin
{
    private Harmony _harmony;
    private IStateMachine _masterStateMachine;
    public ManualLogSource Log;


    private void Awake()
    {
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();

        ChatCommandApi.RegisterLocalChatCommand<CommandRestart>();
        ChatCommandApi.RegisterLocalChatCommand<CommandStop>();
        ChatCommandApi.RegisterLocalChatCommand<CommandStart>();
        ChatCommandApi.RegisterLocalChatCommand<CommandSkipBroken>();


        _masterStateMachine = new MasterStateMachine();
        _masterStateMachine.TransitionTo(_masterStateMachine.InitialState);
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }


    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        _harmony = null;
    }
}