using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Interfaces;
using ZeepSDK.Level;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

public class StateAthLoading : IState
{
    public StateAthLoading(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    // Properties
    public IStateMachine StateMachine { get; }

    public void Enter()
    {
        RacingApi.LevelLoaded += OnLevelLoaded;
        AthStateMachine.Timer.Tick += TimerOnTick;
    }

    public void Execute()
    {
    }

    public void Exit()
    {
        RacingApi.LevelLoaded -= OnLevelLoaded;
        AthStateMachine.Timer.Tick -= TimerOnTick;
    }

    private void TimerOnTick()
    {
        AthStateMachine.Ctx.LoadingTime += 1;
    }

    private void OnLevelLoaded()
    {
        AthStateMachine.Ctx.CurrentLevel = new Level(LevelApi.CurrentLevel);
        StateMachine.TransitionTo(new StateAthPausing(StateMachine));
    }
}