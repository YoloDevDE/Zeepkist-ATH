using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Configs;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Util;
using ZeepSDK.UI;

namespace AuthorTimeHunting.States.Mod;

public class ModRunning : IState
{
    private MyToolbarDrawer _toolbarDrawer;
    private StateMachine GameModeStateMachine { get; set; }


    public void Enter()
    {
        _toolbarDrawer = new MyToolbarDrawer();
        UIApi.AddToolbarDrawer(_toolbarDrawer);

        GameModeStateMachine = GameModeConfigurator.SoloAthMode();
        GameModeStateMachine.Start();
        CommandStart.CommandTrigger += ModAlreadyRunning;
    }


    public void Exit()
    {
        UIApi.RemoveToolbarDrawer(_toolbarDrawer);
        GameModeStateMachine.Stop();
        SpeechBubble.Info("Stopped");
        CommandStart.CommandTrigger -= ModAlreadyRunning;
    }

    public void Update()
    {
        GameModeStateMachine.Update();
    }

    private void ModAlreadyRunning()
    {
        SpeechBubble.Warning("Already running!");
    }
}