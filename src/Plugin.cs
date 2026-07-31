using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States;
using AuthorTimeHunting.States.Master.StateMachine;
using BepInEx;
using HarmonyLib;
using ZeepSDK.ChatCommands;
using ZeepSDK.UI;

namespace AuthorTimeHunting;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ZeepSDK")]
public class Plugin : BaseUnityPlugin
{
	private Harmony _harmony;
	private StateMachineBase _masterStateMachine;
	private ModServices _services;

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
		_services = new ModServices();
		UIApi.AddZeepGUIDrawer(_services.Hud);
		UIApi.AddZeepGUIDrawer(_services.Window);
		UIApi.AddZeepGUIDrawer(_services.Overlay);
		CommandAth.CommandTrigger += _services.Window.Toggle;
		InitializeHarmony();
		RegisterChatCommands();
		InitializeStateMachine();

		Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
	}


	private void OnDestroy()
	{
		if (_services != null)
		{
			CommandAth.CommandTrigger -= _services.Window.Toggle;
			UIApi.RemoveZeepGUIDrawer(_services.Hud);
			UIApi.RemoveZeepGUIDrawer(_services.Window);
			UIApi.RemoveZeepGUIDrawer(_services.Overlay);
		}

		_services?.GameState.Dispose();
		_services?.WorkshopDownloads.Dispose();
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
	}

	private void InitializeStateMachine()
	{
		_masterStateMachine = new MasterStateMachine(_services);
		_masterStateMachine.Init();
	}
}