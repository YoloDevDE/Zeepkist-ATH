using System;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.States;
using AuthorTimeHunting.Util;
using Crosstales;
using UnityEngine;
using ZeepSDK.PhotoMode;
using ZeepSDK.Racing;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.States.Ath.StateMachine;

public class AthStateMachine : MonoBehaviour, IStateMachine
{
	private static readonly TimeSpan ServerMessageThrottle = TimeSpan.FromMilliseconds(1000);

	/// <summary>
	///     How many frames in a row the tick may throw before the run is given up on.
	/// </summary>
	private const int MaxConsecutiveTickFailures = 10;

	private int _consecutiveTickFailures;
	private float _accumulatedDeltaTime;
	private bool _eventsSubscribed;
	private string _lastServerMessage;
	private DateTime _lastServerMessageTime = DateTime.MinValue;
	private bool _timerStarted;

	public AthCtx Ctx { get; set; }

	private void Awake()
	{
		// One AthStateMachine per run, so this is the run's starting line: fresh context,
		// and a level pool that does not carry the exclusions of previous runs.
		Ctx = new AthCtx();
		RandomLevelService.Instance.Reset();
		InitialState = new StateAthStarting(this);
		FinalState = new StateAthStopping(this);
		_eventsSubscribed = false;
		_timerStarted = false;
		_accumulatedDeltaTime = 0f;
	}

	private void Update()
	{
		if (!_timerStarted)
		{
			return;
		}

		_accumulatedDeltaTime += Time.deltaTime;
		OnAthTimerTick();
	}


	public IState CurrentState { get; set; }
	public IState InitialState { get; private set; }
	public IState FinalState { get; private set; }
	public event Action StateMachineFinished;

	public void InvokeFinish()
	{
		StateMachineFinished?.Invoke();
	}


	public void SetServerMessage(bool paused)
	{
		// No level yet means /ath start followed straight by /ath stop - there is
		// nothing to render and every CurrentLevel access below would throw.
		if (Ctx.CurrentLevel == null)
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
			Logger.LogError($"AthStateMachine: {eventName} failed in {state.GetType().Name}: {e.Message}\n{e.StackTrace}");
			return false;
		}
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

		Logger.LogError($"AthStateMachine: Tick failed {MaxConsecutiveTickFailures} frames in a row, stopping the run.");
		_consecutiveTickFailures = 0;
		StopTimer();

		try
		{
			// TransitionTo is a default interface member, so it needs the interface.
			((IStateMachine)this).TransitionTo(FinalState);
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
		PhotoModeApi.PhotoModeEntered -= OnPhotoModeEntered;

		_eventsSubscribed = false;
	}

	public void StartTimer()
	{
		if (_timerStarted)
		{
			return;
		}

		_accumulatedDeltaTime = 0f;
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
		_accumulatedDeltaTime = 0f;
		_timerStarted = false;
	}

	public void Dispose()
	{
		StopTimer();

		// Reading .gameObject on an already destroyed component throws before the
		// null check can help - ask Unity about the component itself instead.
		if (this == null)
		{
			return;
		}

		Destroy(gameObject);
	}
}