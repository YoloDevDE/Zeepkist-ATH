using AuthorTimeHunting.States.Master.States;

namespace AuthorTimeHunting.States.Master.StateMachine;

/// <summary>
///     The mod's own lifecycle: off until /ath start, on until /ath stop. The running state
///     carries the run's machine as a sub-state machine.
/// </summary>
public class MasterStateMachine : StateMachineBase
{
	public MasterStateMachine()
	{
		InitialState = new StateMasterOff(this);
		FinalState = new StateMasterOff(this);
	}

	public override StateBase InitialState { get; }
	public override StateBase FinalState { get; }
}
