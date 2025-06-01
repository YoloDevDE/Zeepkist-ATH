using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthResolvingBrokenLevel(IStateMachine stateMachine) : AthState
{
    // Properties
    public override IStateMachine StateMachine { get; } = stateMachine;

    public override void Enter()
    {
        RacingApi.LevelLoaded += OnLevelLoaded;
    }

    public override async void Execute()
    {
        await PlaylistService.QueueNextRandomLevel();
        PlaylistService.SkipToLastLevel();
    }


    public override void Exit()
    {
        RacingApi.LevelLoaded -= OnLevelLoaded;
    }

    public override void OnAthTimerTick()
    {
    }

    private void OnLevelLoaded()
    {
        StateMachine.TransitionTo(new StateAthProcessingLevel(StateMachine));
    }
}