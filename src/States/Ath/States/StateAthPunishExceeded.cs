using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using UnityEngine;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthPunishExceeded : IState
{
    public StateAthPunishExceeded(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    public IStateMachine StateMachine { get; }

    public void Enter()
    {
        RacingApi.LevelLoaded += OnLevelLoaded;
        AthStateMachine.Timer.Tick += OnTimerTick;
    }

    public void Execute()
    {
        Messenger.Notify().LogCustomColors("Well.. I tried to warn you.. Challenge is over once the level is loaded.", Color.white, Color.red, 10f);
    }

    public void Exit()
    {
        RacingApi.LevelLoaded -= OnLevelLoaded;
        AthStateMachine.Timer.Tick -= OnTimerTick;
    }

    private void OnTimerTick()
    {
        AthStateMachine.Ctx.LoadingTimeInSeconds += 1;
    }

    private void OnLevelLoaded()
    {
        StateMachine.StateMachineFinishedNotify();
    }
}