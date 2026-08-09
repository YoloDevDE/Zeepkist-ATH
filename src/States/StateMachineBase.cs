using System;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.States;

/// <summary>
///     Drives a set of <see cref="StateBase" /> instances: holds the current one, runs the
///     transitions and tears the machine down on request.
///     This used to be an interface whose transition logic sat in C# 8 default interface
///     members. That worked, but an interface cannot hold fields, which forced CurrentState
///     to be publicly settable even though nothing outside ever set it.
/// </summary>
public abstract class StateMachineBase
{
	public StateBase CurrentState { get; private set; }

	public abstract StateBase InitialState { get; }

	public abstract StateBase FinalState { get; }

	public event Action StateMachineFinished;

	public void Init()
	{
		Logger.LogInfo($"StateMachine: Initializing with initial state {InitialState.GetType().Name}");
		TransitionTo(InitialState);
	}

	public void TransitionTo(StateBase nextState)
	{
		if (nextState == null)
		{
			throw new ArgumentNullException(nameof(nextState));
		}

		Logger.LogInfo(
			$"StateMachine: Transitioning from {(CurrentState == null ? "null" : CurrentState.GetType().Name)} to {nextState.GetType().Name}");

		if (CurrentState != null)
		{
			Logger.LogDebug(
				$"StateMachine: Disposing sub-state machine of {CurrentState.GetType().Name} -> {CurrentState.SubStateMachine?.GetType().Name}[{CurrentState.SubStateMachine?.CurrentState?.GetType().Name}]");
			CurrentState.SubStateMachine?.StopGracefully();

			Logger.LogDebug($"StateMachine: Exiting state {CurrentState.GetType().Name}");
			CurrentState.Exit();
		}

		CurrentState = nextState;

		Logger.LogDebug($"StateMachine: Entering state {CurrentState.GetType().Name}");
		CurrentState.Enter();

		if (CurrentState.SubStateMachine == null)
		{
			return;
		}

		Logger.LogDebug($"StateMachine: Initializing sub-state machine in {CurrentState.GetType().Name}");
		CurrentState.SubStateMachine.Init();
	}

	public virtual void Update()
	{
		if (CurrentState == null)
		{
			return;
		}

		CurrentState.Update();
		CurrentState.SubStateMachine?.Update();
	}

	public void StopGracefully()
	{
		try
		{
			Logger.LogInfo(
				$"StateMachine: Disposing. Current state: {(CurrentState == null ? "null" : CurrentState.GetType().Name)}");

			if (CurrentState == null)
			{
				Logger.LogDebug("StateMachine: No current state to exit during disposal");
				return;
			}

			string currentStateName = CurrentState.GetType().Name;
			string finalStateName = FinalState.GetType().Name;

			Logger.LogDebug(
				$"StateMachine: Checking if current state ({currentStateName}) matches final state ({finalStateName})");

			LogAndTransitionToFinalState(currentStateName, finalStateName);

			Logger.LogDebug($"StateMachine: Exiting current state {CurrentState.GetType().Name} during disposal");
			CurrentState.Exit();
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "StateMachine.StopGracefully");
		}
		finally
		{
			Logger.LogInfo("StateMachine: Disposal complete");
		}
	}

	private void LogAndTransitionToFinalState(string currentStateName, string finalStateName)
	{
		if (currentStateName == finalStateName)
		{
			Logger.LogInfo($"StateMachine: Already in final state ({finalStateName}), skipping transition");

			return;
		}

		Logger.LogInfo($"StateMachine: Transitioning to final state {finalStateName} during disposal");
		TransitionTo(FinalState);
	}

	public void InvokeFinish()
	{
		StateMachineFinished?.Invoke();
	}
}
