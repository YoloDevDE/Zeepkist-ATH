using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

public class AthStateMachine : IStateMachine
{
    // Constructor
    public AthStateMachine()
    {
        Ctx = new AthCtx();
        InitialState = new StateAthStarting(this);
        LastState = new StateAthStopping(this);
        Timer = new AthTimer();
    }

    // Properties
    public AthTimer Timer { get; }
    public AthCtx Ctx { get; set; }
    public IState CurrentState { get; set; }
    public IState InitialState { get; }
    public IState LastState { get; }

    // Events
    public event IStateMachine.StateMachineFinishedDelegate OnStateMachineFinished;

    // Methods
    public void StartTimer()
    {
        Timer.Start();
    }

    public void StopTimer()
    {
        Timer.Stop();
    }
}