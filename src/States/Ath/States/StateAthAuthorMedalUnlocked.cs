using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.States.Ath.States;

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
        AthStateMachine.Ctx.CurrentLevel.GoldSkipUnlocked = true;
    }

    public void Execute()
    {
        Messenger.Notify().LogCustomColors("Author Medal acquired!<br>[Respawn to continue]", Color.white, new Color(0.5f, 0f, 0.5f), 10f);
        MessageSenderService.SendLocalMessage(AthStateMachine.Ctx.MessageLevelResult());
        StateMachine.TransitionTo(new StateAthPostRun(StateMachine));
    }

    public void Exit()
    {
    }
}