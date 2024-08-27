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
            Console.WriteLine($"[{GetType().Name}] Exiting state: {CurrentState.GetType().Name}");
            CurrentState.SubStateMachine?.Dispose();
            CurrentState.Exit();
        }

        CurrentState = nextState;
        Console.WriteLine($"[{GetType().Name}] Entering state: {CurrentState.GetType().Name}");
        CurrentState.Enter();
        Console.WriteLine($"[{GetType().Name}] Executing state: {CurrentState.GetType().Name}");
        CurrentState.Execute();
        CurrentState.SubStateMachine?.Init();
    }

    void Dispose()
    {
        Console.WriteLine($"[{GetType().Name}] Transitioning from '{CurrentState.GetType().Name}' to final state: '{FinalState.GetType().Name}'");
        TransitionTo(FinalState);
        Console.WriteLine($"[{GetType().Name}] Exiting state: {CurrentState.GetType().Name}");
        CurrentState.Exit();
    }

    void StateMachineFinishedNotify()
    {
        Console.WriteLine($"[{GetType().Name}] Transitioning from '{CurrentState.GetType().Name}' to final state: '{FinalState.GetType().Name}'");
        InvokeFinish();
    }

    void Init()
    {
        TransitionTo(InitialState);
    }

    void InvokeFinish();
}