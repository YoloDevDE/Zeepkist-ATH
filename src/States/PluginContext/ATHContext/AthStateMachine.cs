using System;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

public class AthStateMachine : IStateMachine
{
    public delegate void ChallengeFinished();

    // Constructor
    public AthStateMachine()
    {
        Ctx = new AthCtx();
        Timer = new AthTimer();
        InitialState = new StateAthStarting(this);
        LastState = new StateAthStopping(this);
    }


    // Properties
    public AthTimer Timer { get; }
    public AthCtx Ctx { get; set; }
    public IState CurrentState { get; set; }
    public IState InitialState { get; }

    public IState LastState { get; }
    public event Action StateChanged;

    public void TransitionTo(IState nextState)
    {
        if (CurrentState != null)
        {
            CurrentState.SubStateMachine?.Stop();
            CurrentState.Exit();
        }

        if (nextState != null)
        {
            CurrentState = nextState;
            CurrentState.Enter();
            CurrentState.Execute();
            CurrentState.SubStateMachine?.TransitionTo(CurrentState.SubStateMachine.InitialState);
        }
        else
        {
            ((IStateMachine)this).Stop();
        }

        StateChanged?.Invoke();
    }

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