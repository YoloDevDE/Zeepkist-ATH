using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Master.StateMachine;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.States.Master.States;

public class StateMasterOff : StateBase
{
	/// <summary>
	///     The mode a queued start is waiting to run, or null when nothing is queued. This
	///     used to be a bare bool: now that a start carries a mode, remembering that one was
	///     requested is not enough - the mode has to survive the wait too, or a queued Ranked
	///     start would come back as whatever happens to be selected when the race begins.
	/// </summary>
	private IGamemode _pendingGamemode;

	/// <param name="stateMachine">The mod's lifecycle machine.</param>
	/// <param name="pendingGamemode">
	///     Set when a start was already requested and only the race is missing - used by
	///     /ath restart outside a running race.
	/// </param>
	public StateMasterOff(MasterStateMachine stateMachine, IGamemode pendingGamemode = null) : base(stateMachine)
	{
		_pendingGamemode = pendingGamemode;
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
		_pendingGamemode = null;
	}

	private void StartChallenge()
	{
		// Read once, here: the run is committed to a mode the moment it is requested, so a
		// selection changed during the wait below does not reach into a start already made.
		IGamemode gamemode = Master.Services.Gamemodes.Selected;

		if (!GameStateObserver.IsInOnlineLobby)
		{
			ToastNotification.Warn("ATH only runs in an online lobby");
			return;
		}

		// A run has to begin on a level that is actually loaded. Starting during the podium
		// or while the next map is still loading would have StateAthStarting rewrite a
		// playlist the game is in the middle of switching.
		if (!GameStateObserver.IsRacing)
		{
			_pendingGamemode = gamemode;
			ToastNotification.Info($"{gamemode.DisplayName} starts as soon as the level is loaded");
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

	/// <summary>
	///     StateMasterOn.Enter() reaches straight into the online HUD. If that chain is not
	///     there the transition would die halfway through, leaving
	///     MasterStateMachine.CurrentState inconsistent. Refuse before the transition starts.
	/// </summary>
	private bool IsHudReady()
	{
		if (PlayerManager.Instance == null || PlayerManager.Instance.currentMaster == null ||
		    PlayerManager.Instance.currentMaster.OnlineGameplayUI == null)
		{
			ToastNotification.Warn("Online HUD not ready yet, try again in a moment");
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
		if (_pendingGamemode != null)
		{
			_pendingGamemode = null;
			ToastNotification.Info("Pending start cancelled");
			return;
		}

		ToastNotification.Warn("ATH is not running");
	}
}