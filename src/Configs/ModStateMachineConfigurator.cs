using AuthorTimeHunting.Commands;
using AuthorTimeHunting.States.Mod;
using ZeepSDK.Multiplayer;

namespace AuthorTimeHunting.Configs;

public static class ModStateMachineConfigurator
{
    public static StateMachine ModStateMachine()
    {
        StateMachine modStateMachine = new StateMachine();
        modStateMachine.SetInitial<ModInit>();
        modStateMachine.Name = "ModStateMachine";

        modStateMachine
            .From<ModInit>()
            .When(() => MultiplayerApi.IsPlayingOnline)
            .To<ModReady>()
            .Or()
            .When(() => !MultiplayerApi.IsPlayingOnline)
            .To<ModNotReady>();

        modStateMachine
            .From<ModNotReady>()
            .When(() => MultiplayerApi.IsPlayingOnline)
            .To<ModReady>();

        modStateMachine
            .From<ModReady>()
            .On(sub => CommandStart.CommandTrigger += sub, unsub => CommandStart.CommandTrigger -= unsub)
            .To<ModRunning>()
            .Or()
            .On(sub => CommandRestart.CommandTrigger += sub, unsub => CommandRestart.CommandTrigger -= unsub)
            .To<ModRunning>()
            .Or()
            .When(() => !MultiplayerApi.IsPlayingOnline)
            .To<ModNotReady>();

        modStateMachine
            .From<ModRunning>()
            .On(sub => CommandStop.CommandTrigger += sub, unsub => CommandStop.CommandTrigger -= unsub)
            .To<ModReady>()
            .Or()
            .When(() => !MultiplayerApi.IsPlayingOnline)
            .To<ModNotReady>();

        return modStateMachine;
    }
}