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
	/// <summary>The state the machine is in. Only transitions change this.</summary>
	public StateBase CurrentState { get; private set; }

	/// <summary>Where <see cref="Init" /> starts.</summary>
	public abstract StateBase InitialState { get; }

	/// <summary>Where <see cref="StopGracefully" /> ends up.</summary>
	public abstract StateBase FinalState { get; }

	/// <summary>Raised once the machine has finished its work.</summary>
	public event Action StateMachineFinished;

	/// <summary>Enters <see cref="InitialState" />.</summary>
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

		Logger.LogDebug($"StateMachine: Executing state {CurrentState.GetType().Name}");
		CurrentState.Execute();

		if (CurrentState.SubStateMachine != null)
		{
			Logger.LogDebug($"StateMachine: Initializing sub-state machine in {CurrentState.GetType().Name}");
			CurrentState.SubStateMachine.Init();
		}
	}

	/// <summary>
	///     Winds the machine down into <see cref="FinalState" />. Called on the sub-machine of
	///     a state that is being left, so it has to swallow its own failures - the parent
	///     transition must complete either way.
	/// </summary>
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

			if (currentStateName != finalStateName)
			{
				Logger.LogInfo($"StateMachine: Transitioning to final state {finalStateName} during disposal");
				TransitionTo(FinalState);
			}
			else
			{
				Logger.LogInfo($"StateMachine: Already in final state ({finalStateName}), skipping transition");
			}

			Logger.LogDebug($"StateMachine: Exiting current state {CurrentState.GetType().Name} during disposal");
			CurrentState.Exit();
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "StateMachine.StopGracefully");
			// Ensure resources are released even if exception occurs
		}
		finally
		{
			Logger.LogInfo("StateMachine: Disposal complete");
		}
	}

	public void InvokeFinish()
	{
		StateMachineFinished?.Invoke();
	}
}