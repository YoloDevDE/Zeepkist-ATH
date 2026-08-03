namespace AuthorTimeHunting.States;

/// <summary>
///     A single state of a <see cref="StateMachineBase" />.
///     Every hook is virtual and does nothing by default, so a state only writes down the
///     moments it actually cares about. Nothing here subscribes to game events - the state
///     machine subscribes once and forwards, which is why a state can never leak a handler.
/// </summary>
public abstract class StateBase
{
	protected StateBase(StateMachineBase stateMachine)
	{
		StateMachine = stateMachine;
	}

	/// <summary>The machine this state belongs to.</summary>
	public StateMachineBase StateMachine { get; }

	/// <summary>
	///     A machine nested inside this state, driven by the parent's transitions. Null for
	///     the vast majority of states.
	/// </summary>
	public virtual StateMachineBase SubStateMachine => null;

	/// <summary>Called once when the state is entered. The state's actual work happens here.</summary>
	public virtual void Enter()
	{
	}

	/// <summary>Called every frame while the state is current.</summary>
	public virtual void Update()
	{
	}

	/// <summary>Called once when the state is left, before the next one is entered.</summary>
	public virtual void Exit()
	{
	}
}
