using System;

namespace AuthorTimeHunting.Interfaces;

public interface IStateMachine
{
    IState CurrentState { get; set; }
    IState InitialState { get; }
    IState LastState { get; }

    event Action StateChanged;

    void TransitionTo(IState nextState)
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
        CurrentState.Exit();

        if (LastState == null)
        {
            return;
        }

        if (CurrentState != LastState)
        {
            CurrentState = LastState;
            CurrentState.Enter();
            CurrentState.Execute();
        }

        CurrentState.Exit();
    }

    void Reset()
    {
        TransitionTo(InitialState);
    }
}