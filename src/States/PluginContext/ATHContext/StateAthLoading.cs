using AuthorTimeHunting.Interfaces;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

public class StateAthLoading : IState
{
    public StateAthLoading(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public IStateMachine StateMachine { get; }

    public void Enter()
    {
        RacingApi.PlayerSpawned += OnPlayerSpawned;
    }

    public void Execute()
    {
    }

    public void Exit()
    {
        RacingApi.PlayerSpawned -= OnPlayerSpawned;
    }

    private void OnPlayerSpawned()
    {
        StateMachine.TransitionTo(new StateAthSpawning(StateMachine));
    }
}