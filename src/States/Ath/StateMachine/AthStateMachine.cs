using System;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.States;
using AuthorTimeHunting.UI;
using AuthorTimeHunting.Util;
using Crosstales;
using UnityEngine;
using ZeepSDK.PhotoMode;
using ZeepSDK.Racing;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.States.Ath.StateMachine;

/// <summary>
///     The run's state machine. A plain class, not a MonoBehaviour: it needs a Unity update
///     loop, but it also needs to inherit <see cref="StateMachineBase" />, and C# has no
///     multiple inheritance. So the frame loop lives in a small nested MonoBehaviour that
///     does nothing but call back in - see AthStateMachine.AthLoopBehaviour.cs.
///     It subscribes to the game's events exactly once and forwards them to whichever state
///     is current. States never subscribe to anything themselves, so they cannot leak a
///     handler no matter how a run ends.
/// </summary>
public partial class AthStateMachine : StateMachineBase
{
	/// <summary>
	///     How many frames in a row the tick may throw before the run is given up on.
	/// </summary>
	private const int MaxConsecutiveTickFailures = 10;

	private static readonly TimeSpan ServerMessageThrottle = TimeSpan.FromMilliseconds(1000);
	private readonly AthPanelDrawer _panelDrawer;

	private AthLoopBehaviour _behaviour;
	private int _consecutiveTickFailures;
	private bool _eventsSubscribed;
	private string _lastServerMessage;
	private DateTime _lastServerMessageTime = DateTime.MinValue;
	private bool _timerStarted;

	public AthStateMachine(ModServices services)
	{
		// One AthStateMachine per run, so this is the run's starting line: fresh context,
		// and a level pool that does not carry the exclusions of previous runs.
		Services = services;
		Ctx = new AthCtx();
		RandomLevels = services.CreateRandomLevelService();
		InitialState = new StateAthStarting(this);
		FinalState = new StateAthStopping(this);

		GameObject host = new(nameof(AthStateMachine))
		{
			hideFlags = HideFlags.HideAndDontSave
		};

		Object.DontDestroyOnLoad(host);
		_behaviour = host.AddComponent<AthLoopBehaviour>();
		_behaviour.Bind(this);

		_panelDrawer = new AthPanelDrawer();
		UIApi.AddZeepGUIDrawer(_panelDrawer);
	}

	public AthCtx Ctx { get; }

	/// <summary>Session-scoped services shared with the rest of the mod.</summary>
	public ModServices Services { get; }

	/// <summary>This run's level pool. A new run gets a new one.</summary>
	public RandomLevelService RandomLevels { get; }

	public override StateBase InitialState { get; }
	public override StateBase FinalState { get; }

	/// <summary>
	///     Refreshes the run HUD. Either renders it into the game's server message area or
	///     hands a snapshot to the in-game window, depending on the config.
	/// </summary>
	public void SetServerMessage(bool paused)
	{
		// No level yet means /ath start followed straight by /ath stop - there is
		// nothing to render and every CurrentLevel access below would throw.
		if (Ctx.CurrentLevel == null)
		{
			return;
		}

		// The window reads the run directly every frame, so nothing has to be pushed to it.
		if (Plugin.Instance.MyConfig.InGameHud.Value)
		{
			return;
		}

		var colors = new
		{
			State = paused ? "#999999" : "#42b336", TimeLeft = paused
				? "#999999"
				: Ctx.IsTimeRunningLow
					? "#bf3939"
					: Ctx.IsTimeAfterSkipRunningLow
						? "#b3b300"
						: "#42b336",
			Author = ColorDefinitions.Author.CTToHexRGB(), Default = "#e6e6e6", AuthorSkip = "#e600e6",
			GoldSkip = "#FFD600", FreeSkip = "#00ffff", EndRunSkip = "#0f0f0f", PenaltySkip = "#bf3939",
			Section = "#ffd4a6"
		};

		string skipText = Ctx.CurrentLevel.AuthorTimeAcquired
			? $"<color={colors.AuthorSkip}>Author Skip</color>"
			: Ctx.CurrentLevel.GoldMedalAcquired
				? $"<color={colors.GoldSkip}>Gold Skip</color>"
				: Ctx.AvaiableFreeSkips > 0
					? $"<color={colors.FreeSkip}>Free Skip ({Ctx.AvaiableFreeSkips}x left)</color>"
					: Ctx.IsTimeRunningLow
						? $"<color={colors.EndRunSkip}><sprite=\"Zeepkist\" name=\"Skull\"> FATAL SKIP <sprite=\"Zeepkist\" name=\"Skull\"></color>"
						: $"<color={colors.PenaltySkip}>Penalty Skip!</color>";

		string punishmentText = Ctx.Penalties == 0
			? ""
			: $"(<color={colors.TimeLeft}>{TimeFormatter.FormatDuration((int)Ctx.GetRemainingTimeWithoutPunishments().TotalMilliseconds)}</color> - <color=#ff4a4a>{TimeSpan.FromMilliseconds(Ctx.PenaltyTimeInMilliseconds * Ctx.Penalties).ToFormattedString()}</color>)";

		// string message = $"/servermessage white 0 " +
		string message =
			$"<size=\"20%\"><align=left><b><color=#{colors.Author}><uppercase>Author-Time-Hunting</uppercase></color></b><br>" +
			$"<color={colors.Section}><b>=== Run Settings ===</b></color><br>" +
			$"<color={colors.Default}>Duration      : {TimeSpan.FromMilliseconds(Ctx.Duration).ToFormattedString()}</color><br>" +
			$"<color={colors.Default}>Skip Penalty  : <color={colors.PenaltySkip}>{TimeSpan.FromMilliseconds(Ctx.PenaltyTimeInMilliseconds).ToFormattedString()}</color></color><br>" +
			$"<color={colors.Section}><b>=== Current Run ===</b></color><br>" +
			$"<color={colors.Default}>State         : <color={colors.State}>{(paused ? "PAUSED" : "ACTIVE")}</color></color><br>" +
			$"<color={colors.Default}>Time Left     : <color={colors.TimeLeft}>{TimeFormatter.FormatDuration((int)Ctx.GetRemainingTime().TotalMilliseconds)}</color> {punishmentText}</color><br>" +
			$"<color={colors.Default}>AT/Gold/Skips : <color={colors.AuthorSkip}>{Ctx.AuthorMedals}</color><color={colors.Default}>/</color><color={colors.GoldSkip}>{Ctx.GoldMedals}</color><color={colors.Default}>/</color><color={colors.PenaltySkip}>{Ctx.Penalties}</color></color><br>" +
			$"<color={colors.Section}><b>=== Current Level ===</b></color><br>" +
			$"<color={colors.Default}>Level Time    : <color={colors.State}>{TimeFormatter.FormatDuration((int)Ctx.CurrentLevel.GetPlayDuration().TotalMilliseconds)}</color></color><br>" +
			$"<color={colors.Default}>Skip Type     : {skipText}</color><br>" +
			$"<color={colors.Default}>Attempt       : {Ctx.CurrentLevel.Attempt}</color><br>" + "</align></size>";

		// ChatApi.SendMessage(message);
		DateTime now = DateTime.UtcNow;

		if (message == _lastServerMessage && now - _lastServerMessageTime < ServerMessageThrottle)
		{
			return;
		}

		_lastServerMessage = message;
		_lastServerMessageTime = now;
		PlayerManager.Instance.currentMaster.OnlineGameplayUI.serverMessageText.text = message;
	}

	#region Event Forwarding

	/// <summary>
	///     Forwards a game event to the current state without letting an exception in that
	///     state escape. These handlers run inside ZeepSDK's event dispatch, which other mods
	///     subscribe to as well - an exception escaping here is not ours alone to lose.
	///     Returns false when the state threw.
	/// </summary>
	private bool TryForward(string eventName, Action<AthState> forward)
	{
		if (CurrentState is not AthState state)
		{
			return true;
		}

		try
		{
			forward(state);
			return true;
		}
		catch (Exception e)
		{
			Logger.LogError(
				$"AthStateMachine: {eventName} failed in {state.GetType().Name}: {e.Message}\n{e.StackTrace}");
			return false;
		}
	}

	/// <summary>
	///     Shows one of the run's panels. The in-game window and the chat message are two
	///     renderings of the same PanelView, so the wording never diverges.
	/// </summary>
	public void Show(PanelView panel)
	{
		if (panel == null)
		{
			return;
		}

		if (Plugin.Instance.MyConfig.InGameHud.Value)
		{
			_panelDrawer.Show(panel);
			return;
		}

		ChatMessageService.SendCustomMessage(PanelChatRenderer.Render(panel));
	}

	/// <summary>Stops the run clock until <see cref="ResumeRun" />.</summary>
	public void PauseRun()
	{
		if (Ctx.IsPaused)
		{
			return;
		}

		Ctx.IsPaused = true;
		Ctx.CurrentLevel?.PauseTiming();
		Logger.LogInfo("AthStateMachine: Run paused.");
	}

	/// <summary>Starts the clock again, but only if a level is actually being played.</summary>
	public void ResumeRun()
	{
		if (!Ctx.IsPaused)
		{
			return;
		}

		Ctx.IsPaused = false;

		if (CurrentState is StateAthOnARun)
		{
			Ctx.CurrentLevel?.ResumeTiming();
		}

		Logger.LogInfo("AthStateMachine: Run resumed.");
	}

	public void OnAthTimerTick()
	{
		if (TryForward(nameof(OnAthTimerTick), state => state.OnAthTimerTick()))
		{
			_consecutiveTickFailures = 0;
			return;
		}

		// The tick runs every frame: a state that keeps throwing would spam the log forever
		// while the run silently stops working. Tolerate a hiccup - a single null reference
		// during a level transition should not end an hour-long run - but not a pattern.
		_consecutiveTickFailures++;

		if (_consecutiveTickFailures < MaxConsecutiveTickFailures)
		{
			return;
		}

		Logger.LogError(
			$"AthStateMachine: Tick failed {MaxConsecutiveTickFailures} frames in a row, stopping the run.");
		_consecutiveTickFailures = 0;
		StopTimer();

		try
		{
			TransitionTo(FinalState);
		}
		catch (Exception e)
		{
			Logger.LogError($"AthStateMachine: Could not stop the run after repeated tick failures: {e.Message}");
		}
	}

	private void OnRoundStarted()
	{
		TryForward(nameof(OnRoundStarted), state => state.OnRoundStarted());
	}

	private void OnRoundEnded()
	{
		TryForward(nameof(OnRoundEnded), state => state.OnRoundEnded());
	}

	private void OnPlayerSpawned()
	{
		TryForward(nameof(OnPlayerSpawned), state => state.OnPlayerSpawned());
	}

	private void OnCrossedFinishLine(float time)
	{
		TryForward(nameof(OnCrossedFinishLine), state => state.OnCrossedFinishLine(time));
	}

	private void OnLevelLoaded()
	{
		TryForward(nameof(OnLevelLoaded), state => state.OnLevelLoaded());
	}

	private void OnPhotoModeEntered()
	{
		TryForward(nameof(OnPhotoModeEntered), state => state.OnPhotoModeEntered());
	}

	/// <summary>
	///     Counted rather than forwarded: no state cares that a crash happened, the level
	///     stats panel just wants the tally. Giving every state an OnCrashed override to
	///     ignore would be twelve no-ops for one counter.
	/// </summary>
	private void OnCrashed(CrashReason reason)
	{
		Ctx.CurrentLevel?.RegisterCrash();
	}

	/// <summary>Fires once per wheel, so a bad landing can add four. Same reasoning as above.</summary>
	private void OnWheelBroken()
	{
		Ctx.CurrentLevel?.RegisterWheelLost();
	}

	private void SubscribeEvents()
	{
		if (_eventsSubscribed)
		{
			return;
		}

		RacingApi.RoundStarted += OnRoundStarted;
		RacingApi.RoundEnded += OnRoundEnded;
		RacingApi.PlayerSpawned += OnPlayerSpawned;
		RacingApi.CrossedFinishLine += OnCrossedFinishLine;
		RacingApi.LevelLoaded += OnLevelLoaded;
		RacingApi.Crashed += OnCrashed;
		RacingApi.WheelBroken += OnWheelBroken;
		PhotoModeApi.PhotoModeEntered += OnPhotoModeEntered;

		_eventsSubscribed = true;
	}

	private void UnsubscribeEvents()
	{
		if (!_eventsSubscribed)
		{
			return;
		}

		RacingApi.RoundStarted -= OnRoundStarted;
		RacingApi.RoundEnded -= OnRoundEnded;
		RacingApi.PlayerSpawned -= OnPlayerSpawned;
		RacingApi.CrossedFinishLine -= OnCrossedFinishLine;
		RacingApi.LevelLoaded -= OnLevelLoaded;
		RacingApi.Crashed -= OnCrashed;
		RacingApi.WheelBroken -= OnWheelBroken;
		PhotoModeApi.PhotoModeEntered -= OnPhotoModeEntered;

		_eventsSubscribed = false;
	}

	#endregion

	#region Lifecycle

	public void StartTimer()
	{
		if (_timerStarted)
		{
			return;
		}

		SubscribeEvents();
		_timerStarted = true;
	}

	public void StopTimer()
	{
		if (!_timerStarted)
		{
			return;
		}

		UnsubscribeEvents();
		_timerStarted = false;
	}

	public void Dispose()
	{
		StopTimer();
		UIApi.RemoveZeepGUIDrawer(_panelDrawer);
		_panelDrawer.Clear();

		// Unity's overloaded == reports a destroyed object as null, so this covers both
		// "already disposed" and "the GameObject went away underneath us".
		if (_behaviour == null)
		{
			return;
		}

		Object.Destroy(_behaviour.gameObject);
		_behaviour = null;
	}

	#endregion
}