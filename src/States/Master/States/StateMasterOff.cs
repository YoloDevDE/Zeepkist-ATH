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

	public StateMasterOff(AthMasterController controller, IGamemode pendingGamemode = null) : base(controller)
	{
		_pendingGamemode = pendingGamemode;
	}

	private AthMasterController AthMaster => (AthMasterController)Controller;

	public override void Enter()
	{
		AthRequests.StopRequested += StopChallenge;
		AthRequests.StartRequested += StartChallenge;
		AthRequests.RestartRequested += StartChallenge;
		AthMaster.Services.GameState.BecameRacing += OnBecameRacing;

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
		AthMaster.Services.GameState.BecameRacing -= OnBecameRacing;
		_pendingGamemode = null;
	}

	/// <summary>
	///     Every start builds the hunt its own lobby, even one asked for by a player who is
	///     already hosting one. Reusing the lobby that happened to be there was a shortcut past
	///     the whole setup screen: the run began wherever the playlist and the round timer had
	///     been left, and the player saw none of it happen. A lobby ATH opened itself is the only
	///     one it knows the state of.
	/// </summary>
	private void StartChallenge()
	{
		Controller.TransitionTo(new StateMasterConnectingToServer(AthMaster, AthMaster.Services.Gamemodes.Selected));
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

		Controller.TransitionTo(new StateMasterOn(AthMaster, gamemode));
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
