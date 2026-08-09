using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Master.States;

namespace AuthorTimeHunting.States.Master.StateMachine;

/// <summary>
///     The mod's own lifecycle: off until a start is asked for, on until a stop is. The
///     running state carries the run's machine as a sub-state machine.
/// </summary>
public class AthMasterController : StateMachineBase
{
	public AthMasterController(ModServices services)
	{
		Services = services;
		InitialState = new StateMasterOff(this);
		FinalState = new StateMasterOff(this);
	}

	public ModServices Services { get; }

	public override StateBase InitialState { get; }
	public override StateBase FinalState { get; }
}
