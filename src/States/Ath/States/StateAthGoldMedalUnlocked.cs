using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using UnityEngine;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthGoldMedalUnlocked : IState
{
    public StateAthGoldMedalUnlocked(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    // Properties
    public IStateMachine StateMachine { get; }

    public void Enter()
    {
        AthStateMachine.Ctx.CurrentLevel.GoldSkipUnlocked = true;
    }

    public void Execute()
    {
        Messenger.Notify().LogCustomColors("Gold Medal acquired!<br>You can now skip without penalty", Color.black, new Color(1f, 0.84f, 0f), 10f);
        ChatApi.SendMessage(AthStateMachine.Ctx.MessageLevelResult());
        StateMachine.TransitionTo(new StateAthPausing(StateMachine));
    }

    public void Exit()
    {
    }
}