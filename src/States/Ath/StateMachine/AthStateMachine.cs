using System;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.States;
using AuthorTimeHunting.Util;
using Crosstales;
using UnityEngine;
using ZeepSDK.PhotoMode;
using ZeepSDK.Racing;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.States.Ath.StateMachine;

/// <summary>
///     The run's state machine. A plain class, not a MonoBehaviour: it needs a Unity update
///     loop, but it also needs to inherit <see cref="StateMachineBase" />, and C# has no
///     multiple inheritance. So the frame loop lives in a small MonoBehaviour that does
///     nothing but call back in - see <see cref="AthLoopBehaviour" />.
///     It subscribes to the game's events exactly once and forwards them to whichever state
///     is current. States never subscribe to anything themselves, so they cannot leak a
///     handler no matter how a run ends.
/// </summary>
public class AthStateMachine : StateMachineBase
{
	private const int MaxConsecutiveTickFailures = 10;

	private static readonly TimeSpan ServerMessageThrottle = TimeSpan.FromMilliseconds(1000);

	private AthLoopBehaviour _behaviour;
	private int _consecutiveTickFailures;
	private bool _eventsSubscribed;
	private string _lastServerMessage;
	private DateTime _lastServerMessageTime = DateTime.MinValue;
	private float? _pendingFinishTime;

	public AthStateMachine(ModServices services, IGamemode gamemode)
	{
		Services = services;
		Gamemode = gamemode;
		Ctx = new AthCtx(gamemode.CreateSettings());
		RandomLevels = services.CreateRandomLevelService();
		InitialState = new StateAthStarting(this);
		FinalState = new StateAthStopping(this);

		GameObject host = new(nameof(AthStateMachine)) { hideFlags = HideFlags.HideAndDontSave };

		Object.DontDestroyOnLoad(host);
		_behaviour = host.AddComponent<AthLoopBehaviour>();
		_behaviour.Bind(this);
	}

	public AthCtx Ctx { get; }

	public bool IsTimerRunning { get; private set; }

	public IGamemode Gamemode { get; }

	public ModServices Services { get; }

	public RandomLevelService RandomLevels { get; }

	public override StateBase InitialState { get; }
	public override StateBase FinalState { get; }

	public bool IsBetweenAttempts =>
		Ctx.CurrentLevel != null && CurrentState is StateAthWaitingForNextRun or StateAthWaitingForRespawn;

	public void SetServerMessage(bool paused)
	{
		if (Ctx.CurrentLevel == null)
		{
			return;
		}

		if (Plugin.Instance.MyConfig.InGameHud.Value)
		{
			return;
		}

		var colors = new
		{
			State = paused ? "#999999" : "#42b336",
			TimeLeft = paused ? "#999999"
				: Ctx.IsTimeRunningLow ? "#bf3939"
				: Ctx.IsTimeAfterSkipRunningLow ? "#b3b300"
				: "#42b336",
			Author = Color.Zeepkist.Medal.Author.CTToHexRGB(),
			Default = "#e6e6e6",
			AuthorSkip = "#e600e6",
			GoldSkip = "#FFD600",
			FreeSkip = "#00ffff",
			EndRunSkip = "#0f0f0f",
			PenaltySkip = "#bf3939",
			Section = "#ffd4a6"
		};

		string skipText = Ctx.CurrentLevel.AuthorTimeAcquired ?
			$"<color={colors.AuthorSkip}>Author Skip</color>" :
			Ctx.CurrentLevel.GoldMedalAcquired ?
				$"<color={colors.GoldSkip}>Gold Skip</color>" :
				Ctx.AvaiableFreeSkips > 0 ?
					$"<color={colors.FreeSkip}>Free Skip ({Ctx.AvaiableFreeSkips}x left)</color>" :
					Ctx.IsTimeRunningLow ?
						$"<color={colors.EndRunSkip}><sprite=\"Zeepkist\" name=\"Skull\"> FATAL SKIP <sprite=\"Zeepkist\" name=\"Skull\"></color>" :
						$"<color={colors.PenaltySkip}>Penalty Skip!</color>";

		string punishmentText = Ctx.Penalties == 0 ?
			"" :
			$"(<color={colors.TimeLeft}>{TimeFormatter.FormatDuration((int)Ctx.GetRemainingTimeWithoutPunishments().TotalMilliseconds)}</color> - <color=#ff4a4a>{TimeSpan.FromMilliseconds(Ctx.PenaltyTimeInMilliseconds * Ctx.Penalties).ToFormattedString()}</color>)";

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

	public void ResumeRun()
	{
		if (!Ctx.IsPaused)
		{
			return;
		}

		Ctx.IsPaused = false;

		if (CurrentState is StateAthWaitingForFinish)
		{
			Ctx.CurrentLevel?.ResumeTiming();
		}

		Logger.LogInfo("AthStateMachine: Run resumed.");
	}

	public override void Update()
	{
		ForwardPendingFinish();

		if (TryForward(nameof(Update), state => state.Update()))
		{
			_consecutiveTickFailures = 0;
			CurrentState?.SubStateMachine?.Update();
			return;
		}

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

	/// <summary>
	///     The one event that is not handled where it arrives. Crossing the finish reaches us
	///     from inside the trigger the physics step is running, and everything a finish sets off
	///     - scoring the run, ending the level, drawing the next one - happens on that stack, in
	///     the middle of a frame that still has to be simulated. The frame it lands in stutters.
	///     So the finish is written down and handled at the start of the next Update instead. The
	///     work is the same, but the frame is ours: nothing is waiting on it, and a state that
	///     wants to read the game reads it one frame later, before anything has reset.
	///     Only the finish. The other events are cheap and go straight through.
	/// </summary>
	private void OnCrossedFinishLine(float time)
	{
		_pendingFinishTime = time;
	}

	private void ForwardPendingFinish()
	{
		if (_pendingFinishTime == null)
		{
			return;
		}

		float time = _pendingFinishTime.Value;
		_pendingFinishTime = null;

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

	private void OnCrashed(CrashReason reason)
	{
		Ctx.CurrentLevel?.RegisterCrash();
	}

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
		if (IsTimerRunning)
		{
			return;
		}

		SubscribeEvents();
		IsTimerRunning = true;
	}

	public void StopTimer()
	{
		if (!IsTimerRunning)
		{
			return;
		}

		UnsubscribeEvents();
		IsTimerRunning = false;
	}

	public void Dispose()
	{
		StopTimer();

		if (_behaviour == null)
		{
			return;
		}

		Object.Destroy(_behaviour.gameObject);
		_behaviour = null;
	}

	#endregion
}
