using AuthorTimeHunting.Service;
using AuthorTimeHunting.UI;

namespace AuthorTimeHunting.States.Ath.StateMachine;

/// <summary>
///     A state of the run. On top of the lifecycle hooks it inherits, it gets one overridable
///     method per game event ATH listens to.
///     None of these are abstract: a state overrides the handful of moments it cares about
///     and stays silent about the rest. The events themselves are subscribed once, centrally,
///     by <see cref="StateMachine.AthStateMachine" /> and forwarded to whichever state is
///     current - a state never registers or removes a handler and therefore cannot leak one.
/// </summary>
public abstract class AthState : StateBase
{
	protected AthState(AthStateMachine stateMachine) : base(stateMachine)
	{
		AthStateMachine = stateMachine;
	}

	/// <summary>The run's state machine, already typed - no cast at the call site.</summary>
	public AthStateMachine AthStateMachine { get; }

	/// <summary>Shorthand for the session's playlist service.</summary>
	public PlaylistService PlaylistService => AthStateMachine.Services.Playlist;

	/// <summary>Shorthand for this run's level pool.</summary>
	public RandomLevelService RandomLevels => AthStateMachine.RandomLevels;

	/// <summary>Every frame while the run's timer is running.</summary>
	public virtual void OnAthTimerTick()
	{
	}

	/// <summary>A lobby round has started.</summary>
	public virtual void OnRoundStarted()
	{
	}

	/// <summary>A lobby round has ended, e.g. because someone skipped.</summary>
	public virtual void OnRoundEnded()
	{
	}

	/// <summary>The local player spawned, which in Zeepkist also means respawned.</summary>
	public virtual void OnPlayerSpawned()
	{
	}

	/// <summary>The local player crossed the finish line with the given time.</summary>
	public virtual void OnCrossedFinishLine(float time)
	{
	}

	/// <summary>A level finished loading.</summary>
	public virtual void OnLevelLoaded()
	{
	}

	/// <summary>The player entered photo mode.</summary>
	public virtual void OnPhotoModeEntered()
	{
	}
}