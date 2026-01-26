using AuthorTimeHunting.Service;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthResolvingDuplicateLevel(IStateMachine stateMachine) : AthState
{
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

    private void OnLevelLoaded()
    {
        StateMachine.TransitionTo(new StateAthProcessingLevel(StateMachine));
    }


    public override void OnAthTimerTick() { }
}