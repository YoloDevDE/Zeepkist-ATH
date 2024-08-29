using System;
using JetBrains.Annotations;

namespace AuthorTimeHunting.Interfaces;

public interface IStateMachine
{
    IState CurrentState { get; set; }
    [NotNull] IState InitialState { get; }
    [NotNull] IState FinalState { get; }
    event Action StateMachineFinished;

    void TransitionTo([NotNull] IState nextState)
    {
        if (CurrentState != null)
        {
            CurrentState.SubStateMachine?.Dispose();
            CurrentState.Exit();
        }

        CurrentState = nextState;
        CurrentState.Enter();
        CurrentState.Execute();
        CurrentState.SubStateMachine?.Init();
    }

    void Dispose()
    {
        TransitionTo(FinalState);
        CurrentState.Exit();
    }

    void StateMachineFinishedNotify()
    {
        InvokeFinish();
    }

    void Init()
    {
        TransitionTo(InitialState);
    }

    void InvokeFinish();
}