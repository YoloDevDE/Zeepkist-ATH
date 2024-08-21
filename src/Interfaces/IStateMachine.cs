using System;

namespace AuthorTimeHunting.Interfaces;

public interface IStateMachine
{
    IState CurrentState { get; set; }
    IState InitialState { get; }
    IState LastState { get; }

    event Action StateMachineFinished;

    void TransitionTo(IState nextState)
    {
        CurrentState?.SubStateMachine?.Stop();
        CurrentState?.Exit();
        if (nextState != null)
        {
            CurrentState = nextState;
            CurrentState.Enter();
            CurrentState.Execute();
        }
        else
        {
            Stop();
        }
    }

    void Stop()
    {
        if (CurrentState == null)
        {
            return;
        }

        CurrentState.SubStateMachine?.Stop();
        CurrentState.StateMachine.TransitionTo(CurrentState.StateMachine.LastState);
        CurrentState.Exit();
    }
}