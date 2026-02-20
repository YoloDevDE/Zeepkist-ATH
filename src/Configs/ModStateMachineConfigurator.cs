using AuthorTimeHunting.Commands;
using AuthorTimeHunting.States.Mod;
using ZeepkistClient;

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
            .When(() => ZeepkistNetwork.IsMasterClient)
            .To<ModReady>()
            .Or()
            .When(() => !ZeepkistNetwork.IsMasterClient)
            .To<ModNotReady>();

        modStateMachine
            .From<ModNotReady>()
            .When(() => ZeepkistNetwork.IsMasterClient)
            .To<ModReady>();

        modStateMachine
            .From<ModReady>()
            .On(sub => CommandStart.CommandTrigger += sub, unsub => CommandStart.CommandTrigger -= unsub)
            .To<ModRunning>()
            .Or()
            .On(sub => CommandRestart.CommandTrigger += sub, unsub => CommandRestart.CommandTrigger -= unsub)
            .To<ModRunning>()
            .Or()
            .When(() => !ZeepkistNetwork.IsMasterClient)
            .To<ModNotReady>();

        modStateMachine
            .From<ModRunning>()
            .On(sub => CommandStop.CommandTrigger += sub, unsub => CommandStop.CommandTrigger -= unsub)
            .To<ModReady>()
            .Or()
            .When(() => !ZeepkistNetwork.IsMasterClient)
            .To<ModNotReady>();

        return modStateMachine;
    }
}