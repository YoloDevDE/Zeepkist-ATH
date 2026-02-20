using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Util;
using ZeepSDK.UI;

namespace AuthorTimeHunting.States.Mod;

public class ModReady : IState
{
    private MyToolbarDrawer _toolbarDrawer;

    public void Enter()
    {
        _toolbarDrawer = new MyToolbarDrawer();
        UIApi.AddToolbarDrawer(_toolbarDrawer);
        CommandStop.CommandTrigger += ModAlreadyStopped;
    }

    public void Exit()
    {
        UIApi.RemoveToolbarDrawer(_toolbarDrawer);
        CommandStop.CommandTrigger -= ModAlreadyStopped;
    }

    public void Update() { }

    private void ModAlreadyStopped()
    {
        SpeechBubble.Warning("Already stopped!");
    }
}