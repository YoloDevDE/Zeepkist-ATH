using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Util;
using UnityEngine;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

public class StateAthAuthorMedalUnlocked : IState
{
    public StateAthAuthorMedalUnlocked(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    // Properties
    public IStateMachine StateMachine { get; }

    public void Enter()
    {
        AthStateMachine.Ctx.AuthorMedals++;
        AthStateMachine.Ctx.CurrentLevel.Levelbeaten = true;
    }

    public void Execute()
    {
        Messenger.Notify().LogCustomColors("You've got the Author Medal<br>You can now Respawn to continue", Color.white, new Color(0.5f, 0f, 0.5f), 7.5f);
        ChatApi.SendMessage(AthStateMachine.Ctx.MessageFinish());
        StateMachine.TransitionTo(new StateAthPostRunning(StateMachine));
    }

    public void Exit()
    {
    }
}