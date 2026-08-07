using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States;
using AuthorTimeHunting.States.Master.StateMachine;
using AuthorTimeHunting.Util;
using BepInEx;
using HarmonyLib;
using ZeepSDK.ChatCommands;
using ZeepSDK.UI;

namespace AuthorTimeHunting;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ZeepSDK")]
public class Plugin : BaseUnityPlugin
{
	private const string ToastTag = "ATH";

	private Harmony _harmony;
	private StateMachineBase _masterStateMachine;

	private Plugin()
	{
		Util.Logger.Initialize(Logger);
		FrogNotification.Initialize(ToastTag);
		Instance = this;
	}

	public static Plugin Instance { get; private set; }

	public PluginConfig MyConfig { get; private set; }

	public ModServices Services { get; private set; }

	private void Awake()
	{
		InitializeConfig();
		Services = new ModServices();
		UIApi.AddZeepGUIDrawer(Services.RunOverlay);

		UIApi.AddZeepGUIDrawer(Services.LevelCard);
		UIApi.AddZeepGUIDrawer(Services.Leaderboard);
		UIApi.AddZeepGUIDrawer(Services.LevelSummary);
		UIApi.AddZeepGUIDrawer(Services.Results);
		UIApi.AddZeepGUIDrawer(Services.Help);

		UIApi.AddZeepGUIDrawer(Services.Welcome);
		UIApi.AddZeepGUIDrawer(Services.Menu);
		UIApi.AddZeepGUIDrawer(Services.Status);
		UIApi.AddZeepGUIDrawer(Services.Debug);

		UIApi.AddZeepGUIDrawer(Services.Loading);
		UIApi.AddToolbarDrawer(Services.Toolbar);
		UIApi.AddToolbarDrawer(Services.DebugToolbar);
		CommandAth.CommandTrigger += Services.Menu.Toggle;
		Services.PlayMenu.Listen();
		InitializeHarmony();
		RegisterChatCommands();
		InitializeStateMachine();

		Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
	}

	private void OnDestroy()
	{
		if (Services != null)
		{
			CommandAth.CommandTrigger -= Services.Menu.Toggle;
			Services.PlayMenu.Dispose();
			UIApi.RemoveZeepGUIDrawer(Services.RunOverlay);
			UIApi.RemoveZeepGUIDrawer(Services.LevelCard);
			UIApi.RemoveZeepGUIDrawer(Services.Leaderboard);
			UIApi.RemoveZeepGUIDrawer(Services.LevelSummary);
			UIApi.RemoveZeepGUIDrawer(Services.Results);
			UIApi.RemoveZeepGUIDrawer(Services.Help);
			UIApi.RemoveZeepGUIDrawer(Services.Welcome);
			UIApi.RemoveZeepGUIDrawer(Services.Menu);
			UIApi.RemoveZeepGUIDrawer(Services.Status);
			UIApi.RemoveZeepGUIDrawer(Services.Debug);
			UIApi.RemoveZeepGUIDrawer(Services.Loading);
			UIApi.RemoveToolbarDrawer(Services.Toolbar);
			UIApi.RemoveToolbarDrawer(Services.DebugToolbar);
		}

		Services?.RaceTime.Dispose();
		Services?.Loading.Dispose();
		Services?.GameState.Dispose();
		Services?.Trace.Dispose();
		Services?.Health.Dispose();
		Services?.WorkshopDownloads.Dispose();
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
		ChatCommandApi.RegisterLocalChatCommand<CommandAth>();
	}

	private void InitializeStateMachine()
	{
		_masterStateMachine = new MasterStateMachine(Services);
		_masterStateMachine.Init();
	}
}
