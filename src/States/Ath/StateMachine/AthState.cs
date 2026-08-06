using AuthorTimeHunting.Service;

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

	public AthStateMachine AthStateMachine { get; }

	public PlaylistService PlaylistService => AthStateMachine.Services.Playlist;

	public RandomLevelService RandomLevels => AthStateMachine.RandomLevels;

	public virtual void OnRoundStarted()
	{
	}

	public virtual void OnRoundEnded()
	{
	}

	public virtual void OnPlayerSpawned()
	{
	}

	public virtual void OnCrossedFinishLine(float time)
	{
	}

	public virtual void OnLevelLoaded()
	{
	}

	public virtual void OnPhotoModeEntered()
	{
	}
}
