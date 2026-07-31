using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Master.StateMachine;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.States.Master.States;

public class StateMasterOff : StateBase
{
	/// <summary>
	///     Set when a start was requested while no race was running. The observer's
	///     BecameRacing then starts the run, instead of the command doing it directly.
	/// </summary>
	private bool _startPending;

	/// <param name="stateMachine">The mod's lifecycle machine.</param>
	/// <param name="startPending">
	///     True when a start was already requested and only the race is missing - used by
	///     /ath restart outside a running race.
	/// </param>
	public StateMasterOff(MasterStateMachine stateMachine, bool startPending = false) : base(stateMachine)
	{
		_startPending = startPending;
	}

	private MasterStateMachine Master => (MasterStateMachine)StateMachine;

	public override void Enter()
	{
		CommandStop.CommandTrigger += StopChallenge;
		CommandStart.CommandTrigger += StartChallenge;
		CommandRestart.CommandTrigger += StartChallenge;
		Master.Services.GameState.BecameRacing += OnBecameRacing;
	}

	public override void Exit()
	{
		CommandStop.CommandTrigger -= StopChallenge;
		CommandStart.CommandTrigger -= StartChallenge;
		CommandRestart.CommandTrigger -= StartChallenge;
		Master.Services.GameState.BecameRacing -= OnBecameRacing;
		_startPending = false;
	}

	private void StartChallenge()
	{
		if (!GameStateObserver.IsInOnlineLobby)
		{
			Messenger.Notify().LogWarning("ATH only runs in an online lobby");
			return;
		}

		// A run has to begin on a level that is actually loaded. Starting during the podium
		// or while the next map is still loading would have StateAthStarting rewrite a
		// playlist the game is in the middle of switching.
		if (!GameStateObserver.IsRacing)
		{
			_startPending = true;
			Messenger.Notify().Log("ATH starts as soon as the level is loaded");
			Logger.LogInfo($"StateMasterOff: Start requested while {DescribeWait()}, waiting for the race to start.");
			return;
		}

		BeginRun();
	}

	private void OnBecameRacing()
	{
		if (!_startPending)
		{
			return;
		}

		_startPending = false;
		Logger.LogInfo("StateMasterOff: Race is running, starting the pending run.");
		BeginRun();
	}

	private void BeginRun()
	{
		if (!IsHudReady())
		{
			return;
		}

		StateMachine.TransitionTo(new StateMasterOn(Master));
	}

	/// <summary>
	///     StateMasterOn.Enter() reaches straight into the online HUD. If that chain is not
	///     there the transition would die halfway through, leaving
	///     MasterStateMachine.CurrentState inconsistent. Refuse before the transition starts.
	/// </summary>
	private static bool IsHudReady()
	{
		if (PlayerManager.Instance == null || PlayerManager.Instance.currentMaster == null ||
		    PlayerManager.Instance.currentMaster.OnlineGameplayUI == null)
		{
			Messenger.Notify().LogWarning("Online HUD not ready yet, try again in a moment");
			return false;
		}

		return true;
	}

	private static string DescribeWait()
	{
		return !GameStateObserver.IsLevelReady
			? "the level is loading"
			: $"the lobby is in {GameStateObserver.LobbyState}";
	}

	private void StopChallenge()
	{
		if (_startPending)
		{
			_startPending = false;
			Messenger.Notify().Log("pending start cancelled");
			return;
		}

		Messenger.Notify().LogWarning("already stopped");
	}
}