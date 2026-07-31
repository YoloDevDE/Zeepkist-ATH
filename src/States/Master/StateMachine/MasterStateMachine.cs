using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Master.States;

namespace AuthorTimeHunting.States.Master.StateMachine;

/// <summary>
///     The mod's own lifecycle: off until /ath start, on until /ath stop. The running state
///     carries the run's machine as a sub-state machine.
/// </summary>
public class MasterStateMachine : StateMachineBase
{
	public MasterStateMachine(ModServices services)
	{
		Services = services;
		InitialState = new StateMasterOff(this);
		FinalState = new StateMasterOff(this);
	}

	/// <summary>Session-scoped services, handed down to the run's machine.</summary>
	public ModServices Services { get; }

	public override StateBase InitialState { get; }
	public override StateBase FinalState { get; }
}