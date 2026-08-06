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

	public StateMachineBase StateMachine { get; }

	public virtual StateMachineBase SubStateMachine => null;

	public virtual void Enter()
	{
	}

	public virtual void Update()
	{
	}

	public virtual void Exit()
	{
	}
}
