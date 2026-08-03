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
	/// <summary>
	///     Prefix shown on every toast notification. MyPluginInfo only carries the full
	///     plugin name, which is too long for the corner of the screen.
	/// </summary>
	private const string ToastTag = "ATH";

	private Harmony _harmony;
	private StateMachineBase _masterStateMachine;

	private Plugin()
	{
		Util.Logger.Initialize(Logger);
		ToastNotification.Initialize(ToastTag);
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

	/// <summary>
	///     The session's services. Public because the chat commands are instantiated by
	///     ZeepSDK and have nowhere else to reach them from - the state machines are handed
	///     theirs properly.
	/// </summary>
	public ModServices Services { get; private set; }

	private void Awake()
	{
		InitializeConfig();
		Services = new ModServices();
		UIApi.AddZeepGUIDrawer(Services.RunOverlay);

		// Before the control panel, which hangs under it and needs this frame's rect rather
		// than last frame's - otherwise it lags a frame behind every height change.
		UIApi.AddZeepGUIDrawer(Services.LevelStats);
		UIApi.AddZeepGUIDrawer(Services.Control);
		UIApi.AddZeepGUIDrawer(Services.LevelSummary);
		UIApi.AddZeepGUIDrawer(Services.Results);
		UIApi.AddZeepGUIDrawer(Services.Debug);
		UIApi.AddToolbarDrawer(Services.Toolbar);
		CommandAth.CommandTrigger += Services.ToggleUi;
		CommandAthDebug.CommandTrigger += Services.Debug.Toggle;
		InitializeHarmony();
		RegisterChatCommands();
		InitializeStateMachine();

		Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
	}


	private void OnDestroy()
	{
		if (Services != null)
		{
			CommandAth.CommandTrigger -= Services.ToggleUi;
			CommandAthDebug.CommandTrigger -= Services.Debug.Toggle;
			UIApi.RemoveZeepGUIDrawer(Services.RunOverlay);
			UIApi.RemoveZeepGUIDrawer(Services.Control);
			UIApi.RemoveZeepGUIDrawer(Services.LevelStats);
			UIApi.RemoveZeepGUIDrawer(Services.LevelSummary);
			UIApi.RemoveZeepGUIDrawer(Services.Results);
			UIApi.RemoveZeepGUIDrawer(Services.Debug);
			UIApi.RemoveToolbarDrawer(Services.Toolbar);
		}

		Services?.RaceTime.Dispose();
		Services?.Leaderboard.Dispose();
		Services?.GameState.Dispose();
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
		ChatCommandApi.RegisterLocalChatCommand<CommandRestart>();
		ChatCommandApi.RegisterLocalChatCommand<CommandStop>();
		ChatCommandApi.RegisterLocalChatCommand<CommandStart>();
		ChatCommandApi.RegisterLocalChatCommand<CommandSkipBroken>();
		ChatCommandApi.RegisterLocalChatCommand<CommandAth>();
		ChatCommandApi.RegisterLocalChatCommand<CommandAthDebug>();
		ChatCommandApi.RegisterLocalChatCommand<CommandAthHistory>();
	}

	private void InitializeStateMachine()
	{
		_masterStateMachine = new MasterStateMachine(Services);
		_masterStateMachine.Init();
	}
}
