using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.States.Master.States;

public class StateMasterOff : IState, IRootState
{
    // Constructor
    public StateMasterOff(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    // Properties
    public IStateMachine StateMachine { get; }

    // Public Methods
    public void Enter()
    {
        CommandStop.CommandTrigger += StopChallenge;
        CommandStart.CommandTrigger += StartChallenge;
        CommandRestart.CommandTrigger += StartChallenge;
    }

    public void Execute()
    {
        // No implementation needed for Execute in this state
    }

    public void Exit()
    {
        CommandStop.CommandTrigger -= StopChallenge;
        CommandStart.CommandTrigger -= StartChallenge;
        CommandRestart.CommandTrigger -= StartChallenge;
    }

    // Private Methods
    private void StartChallenge()
    {
        StateMachine.TransitionTo(new StateMasterOn(StateMachine));
    }

    private void StopChallenge()
    {
        Messenger.Notify().LogWarning("already stopped");
    }
}