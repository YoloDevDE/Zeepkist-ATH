using System;
using AuthorTimeHunting.Util;
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
        Logger.LogInfo($"StateMachine: Transitioning from {(CurrentState == null ? "null" : CurrentState.GetType().Name)} to {nextState.GetType().Name}");

        if (CurrentState != null)
        {
            Logger.LogDebug($"StateMachine: Disposing sub-state machine of {CurrentState.GetType().Name} -> {CurrentState.SubStateMachine?.GetType().Name}[{CurrentState.SubStateMachine?.CurrentState?.GetType().Name}]");
            CurrentState.SubStateMachine?.StopGracefully();

            Logger.LogDebug($"StateMachine: Exiting state {CurrentState.GetType().Name}");
            CurrentState.Exit();
        }

        CurrentState = nextState;

        Logger.LogDebug($"StateMachine: Entering state {CurrentState.GetType().Name}");
        CurrentState.Enter();

        Logger.LogDebug($"StateMachine: Executing state {CurrentState.GetType().Name}");
        CurrentState.Execute();

        if (CurrentState.SubStateMachine != null)
        {
            Logger.LogDebug($"StateMachine: Initializing sub-state machine in {CurrentState.GetType().Name}");
            CurrentState.SubStateMachine.Init();
        }
    }

    private void StopGracefully()
    {
        try
        {
            Logger.LogInfo($"StateMachine: Disposing. Current state: {(CurrentState == null ? "null" : CurrentState.GetType().Name)}");

            if (CurrentState != null)
            {
                string currentStateName = CurrentState.GetType().Name;
                string finalStateName = FinalState.GetType().Name;

                Logger.LogDebug($"StateMachine: Checking if current state ({currentStateName}) matches final state ({finalStateName})");

                if (CurrentState.GetType().Name != FinalState.GetType().Name)
                {
                    Logger.LogInfo($"StateMachine: Transitioning to final state {finalStateName} during disposal");
                    TransitionTo(FinalState);
                }
                else
                {
                    Logger.LogInfo($"StateMachine: Already in final state ({finalStateName}), skipping transition");
                }

                Logger.LogDebug($"StateMachine: Exiting current state {CurrentState.GetType().Name} during disposal");
                CurrentState?.Exit();
            }
            else
            {
                Logger.LogDebug("StateMachine: No current state to exit during disposal");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "StateMachine.Dispose");
            // Ensure resources are released even if exception occurs
        }

        Logger.LogInfo("StateMachine: Disposal complete");
    }

    void StateMachineFinishedNotify()
    {
        Logger.LogInfo("StateMachine: Notifying finish");
        InvokeFinish();
    }

    void Init()
    {
        Logger.LogInfo($"StateMachine: Initializing with initial state {InitialState.GetType().Name}");
        TransitionTo(InitialState);
    }

    void InvokeFinish();
}