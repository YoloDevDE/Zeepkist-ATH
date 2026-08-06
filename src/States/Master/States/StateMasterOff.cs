using System;
using System.Threading.Tasks;
using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Master.StateMachine;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.States.Master.States;

public class StateMasterOff : StateBase
{
	private IGamemode _pendingGamemode;

	public StateMasterOff(MasterStateMachine stateMachine, IGamemode pendingGamemode = null) : base(stateMachine)
	{
		_pendingGamemode = pendingGamemode;
	}

	private MasterStateMachine Master => (MasterStateMachine)StateMachine;

	public override void Enter()
	{
		AthRequests.StopRequested += StopChallenge;
		AthRequests.StartRequested += StartChallenge;
		AthRequests.RestartRequested += StartChallenge;
		Master.Services.GameState.BecameRacing += OnBecameRacing;

		StartPendingRun();
	}

	/// <summary>
	///     A start handed over by the lobby setup arrives when the race is already running:
	///     BecameRacing fired while the lobby was still being built, and an edge that has passed
	///     never comes again. So a pending run looks at the state once by itself.
	///     A frame later, because the transition into this state has not finished yet - starting
	///     the next one from inside Enter would run the run's own machine through its initial
	///     state twice.
	/// </summary>
	private async void StartPendingRun()
	{
		if (_pendingGamemode == null)
		{
			return;
		}

		await Task.Yield();

		if (_pendingGamemode == null || !GameStateObserver.IsRacing)
		{
			return;
		}

		try
		{
			OnBecameRacing();
		}
		catch (Exception e)
		{
			Logger.LogError($"StateMasterOff: Could not start the pending run: {e.Message}\n{e.StackTrace}");
		}
	}

	public override void Exit()
	{
		AthRequests.StopRequested -= StopChallenge;
		AthRequests.StartRequested -= StartChallenge;
		AthRequests.RestartRequested -= StartChallenge;
		Master.Services.GameState.BecameRacing -= OnBecameRacing;
		_pendingGamemode = null;
	}

	private void StartChallenge()
	{
		IGamemode gamemode = Master.Services.Gamemodes.Selected;

		if (!GameStateObserver.IsLobbyHost)
		{
			StateMachine.TransitionTo(new StateMasterConnectingToServer(Master, gamemode));
			return;
		}

		if (!GameStateObserver.IsRacing)
		{
			_pendingGamemode = gamemode;
			FrogNotification.Info($"{gamemode.DisplayName} starts as soon as the level is loaded");
			Logger.LogInfo($"StateMasterOff: Start requested while {DescribeWait()}, waiting for the race to start.");
			return;
		}

		BeginRun(gamemode);
	}

	private void OnBecameRacing()
	{
		if (_pendingGamemode == null)
		{
			return;
		}

		IGamemode gamemode = _pendingGamemode;
		_pendingGamemode = null;
		Logger.LogInfo($"StateMasterOff: Race is running, starting the pending {gamemode.Id} run.");
		BeginRun(gamemode);
	}

	private void BeginRun(IGamemode gamemode)
	{
		if (!IsHudReady())
		{
			return;
		}

		StateMachine.TransitionTo(new StateMasterOn(Master, gamemode));
	}

	private bool IsHudReady()
	{
		if (PlayerManager.Instance == null || PlayerManager.Instance.currentMaster == null ||
		    PlayerManager.Instance.currentMaster.OnlineGameplayUI == null)
		{
			FrogNotification.Warn("Online HUD not ready yet, try again in a moment");
			return false;
		}

		return true;
	}

	private static string DescribeWait()
	{
		return !GameStateObserver.IsLevelReady ?
			"the level is loading" :
			$"the lobby is in {GameStateObserver.LobbyState}";
	}

	private void StopChallenge()
	{
		if (_pendingGamemode != null)
		{
			_pendingGamemode = null;
			FrogNotification.Info("Pending start cancelled");
			return;
		}

		FrogNotification.Warn("ATH is not running");
	}
}
