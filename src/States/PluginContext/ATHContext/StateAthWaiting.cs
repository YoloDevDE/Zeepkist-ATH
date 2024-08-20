using AuthorTimeHunting.Interfaces;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

public class StateAthWaiting : IState
{
    private readonly AthStateMachine _stateMachine;

    public StateAthWaiting(AthStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public IStateMachine StateMachine => _stateMachine;

    public void Enter()
    {
    }

    public void Execute()
    {
    }

    public void Exit()
    {
    }
}