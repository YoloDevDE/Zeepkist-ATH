using AuthorTimeHunting.Commands;
using BepInEx;
using HarmonyLib;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ZeepSDK")]
public class Plugin : BaseUnityPlugin
{
    private Harmony harmony;
    public ChallengeStateManager ChallengeStateManager;

    private void Awake()
    {
        harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
        ChatCommandApi.RegisterLocalChatCommand<StartChallengeCommand>();
        ChatCommandApi.RegisterLocalChatCommand<RestartChallengeCommand>();
        ChatCommandApi.RegisterLocalChatCommand<StopChallengeCommand>();
        ChatCommandApi.RegisterLocalChatCommand<SkipLevelCommand>();
        ChatCommandApi.RegisterLocalChatCommand<SkipBrokenLevelCommand>();
        ChallengeStateManager = new ChallengeStateManager();

        StartChallengeCommand.OnHandle += ChallengeStateManager.StartChallenge;
        RestartChallengeCommand.OnHandle += ChallengeStateManager.RestartChallenge;
        StopChallengeCommand.OnHandle += StopChallenge;
        // Plugin startup logic
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void StopChallenge()
    {
        ChallengeStateManager.Challenge.SwitchState(new StateEnding(ChallengeStateManager.Challenge));
    }

    private void Update()
    {
    }

    private void OnDestroy()
    {
        harmony?.UnpatchSelf();
        harmony = null;
    }
}