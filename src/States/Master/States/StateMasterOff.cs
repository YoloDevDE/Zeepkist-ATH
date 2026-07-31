using AuthorTimeHunting.Commands;
using AuthorTimeHunting.States.Master.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;

namespace AuthorTimeHunting.States.Master.States;

public class StateMasterOff : StateBase
{
	// Constructor
	public StateMasterOff(MasterStateMachine stateMachine) : base(stateMachine)
	{
	}

	// Public Methods
	public override void Enter()
	{
		CommandStop.CommandTrigger += StopChallenge;
		CommandStart.CommandTrigger += StartChallenge;
		CommandRestart.CommandTrigger += StartChallenge;
	}

	public override void Exit()
	{
		CommandStop.CommandTrigger -= StopChallenge;
		CommandStart.CommandTrigger -= StartChallenge;
		CommandRestart.CommandTrigger -= StartChallenge;
	}

	// Private Methods
	private void StartChallenge()
	{
		if (!IsReadyToStart())
		{
			return;
		}

		StateMachine.TransitionTo(new StateMasterOn((MasterStateMachine)StateMachine));
	}

    /// <summary>
    ///     StateMasterOn.Enter() reaches straight into the online HUD. Outside a lobby that
    ///     chain is null and the transition dies halfway through, leaving
    ///     MasterStateMachine.CurrentState inconsistent. Refuse before the transition starts.
    /// </summary>
    private static bool IsReadyToStart()
	{
		if (ZeepkistNetwork.CurrentLobby == null)
		{
			Messenger.Notify().LogWarning("ATH only runs in an online lobby");
			return false;
		}

		if (PlayerManager.Instance == null || PlayerManager.Instance.currentMaster == null ||
		    PlayerManager.Instance.currentMaster.OnlineGameplayUI == null)
		{
			Messenger.Notify().LogWarning("Online HUD not ready yet, try again once a level is loaded");
			return false;
		}

		return true;
	}

	private void StopChallenge()
	{
		Messenger.Notify().LogWarning("already stopped");
	}
}