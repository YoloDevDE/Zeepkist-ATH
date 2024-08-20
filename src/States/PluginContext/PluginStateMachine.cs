using AuthorTimeHunting.Interfaces;

namespace AuthorTimeHunting.States.PluginContext;

public class PluginStateMachine : IStateMachine
{
    public PluginStateMachine()
    {
        InitialState = new StateOff(this);
        LastState = new StateOff(this);
    }

    public IState LastState { get; set; }
    public event IStateMachine.StateMachineFinishedDelegate OnStateMachineFinished;
    public IState CurrentState { get; set; }
    public IState InitialState { get; set; }
}