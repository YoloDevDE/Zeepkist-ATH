using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Master.StateMachine;
using AuthorTimeHunting.UI;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ZeepSDK")]
public class Plugin : BaseUnityPlugin
{
    private Harmony _harmony;
    private IStateMachine _masterStateMachine;

    private GameObject LocalUI;


    // Singleton instance
    public static Plugin Instance { get; private set; }

    // Config entries
    public static ConfigEntry<bool> SavePlaylistOnRunEnd { get; set; }
    public static ConfigEntry<bool> Minimalist { get; set; }
    public static ConfigEntry<bool> TutorialConfig { get; set; }

    private void Awake()
    {
        // Set up singleton instance
        if (!Instance)
        {
            Instance = this;
            DontDestroyOnLoad(this); // Keep the plugin object alive across scenes
        }
        else
        {
            Logger.LogWarning("Plugin instance already exists!");
            Destroy(this);
            return;
        }


        // Initialize the ConfigEntry with default values
        SavePlaylistOnRunEnd = Config.Bind("General", "Save Playlist on Run End", false, "Literally what it says. what did you expect");
        Minimalist = Config.Bind("General", "Minimalist", false, "Makes it a bit less text");
        TutorialConfig = Config.Bind("General", "Tutorial", true, "Enables/Disables the tutorial");

        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();

        // Register chat commands
        ChatCommandApi.RegisterLocalChatCommand<CommandRestart>();
        ChatCommandApi.RegisterLocalChatCommand<CommandStop>();
        ChatCommandApi.RegisterLocalChatCommand<CommandStart>();
        ChatCommandApi.RegisterLocalChatCommand<CommandSkipBroken>();

        // Initialize the state machine
        _masterStateMachine = new MasterStateMachine();
        _masterStateMachine.TransitionTo(_masterStateMachine.InitialState);

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void Start()
    {
        // Create a draggable panel
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            LocalUI = UIBuilder.Begin()
                .Canvas("MainCanvas")
                .Panel("DraggablePanel", new Color(0, 0, 0, 0.5f), new Vector2(500, 300))
                .MakeDraggable(position => Debug.Log($"Dragged to: {position}"))
                .Button("TestButton", "Click Me!", Color.green, new Vector2(150, 50), () => Debug.Log("Button Clicked!"))
                .SetPosition(new Vector2(0, 0))
                .Build();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }


        _harmony?.UnpatchSelf();
        _harmony = null;
    }
}