using System;
using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Master.StateMachine;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ZeepSDK")]
public class Plugin : BaseUnityPlugin
{
    private GraphQLService _graphQLService;
    private Harmony _harmony;
    private IStateMachine _masterStateMachine;

    private void Awake()
    {
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();
        _graphQLService = GraphQLService.Instance;

        ChatCommandApi.RegisterLocalChatCommand<CommandRestart>();
        ChatCommandApi.RegisterLocalChatCommand<CommandStop>();
        ChatCommandApi.RegisterLocalChatCommand<CommandStart>();
        ChatCommandApi.RegisterLocalChatCommand<CommandSkipBroken>();

        _masterStateMachine = new MasterStateMachine();
        _masterStateMachine.TransitionTo(_masterStateMachine.InitialState);
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        // Initialize GraphQL client lazily
        // InitializeGraphQLClient();
    }

    private async void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Logger.LogInfo("Button pressed");
            LevelItem result = await _graphQLService.GetRandomLevelAsync();
            Console.WriteLine("Level: " + result);
        }
    }


    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        _harmony = null;
    }
}