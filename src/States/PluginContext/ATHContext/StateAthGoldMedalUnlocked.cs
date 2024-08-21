using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Util;
using UnityEngine;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

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
        Messenger.Notify().LogCustomColors("You've got the Gold Medal !<br>Gold Skip: Unlocked", Color.black, new Color(1f, 0.84f, 0f), 7.5f);
        ChatApi.SendMessage(AthStateMachine.Ctx.MessageFinish());
        StateMachine.TransitionTo(new StateAthPausing(StateMachine));
    }

    public void Exit()
    {
    }
}