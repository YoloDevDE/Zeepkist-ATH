using System;
using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using TMPro;
using UnityEngine;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.UI;

/// <summary>
///     Rewrites the game's own running-time display into the only number that decides
///     anything: how much time is left before the next medal is gone.
///     <code>
///     counting down          driving               all medals gone
/// 
///      Attempt 3
///        o o o
///      -00:01.749             00:12.340             00:34.343
///     AT -00:32.123          G  +00:02.100
///                            AT +00:12.300
///     </code>
///     The time counting up was never the question. Every run is a race against one of two
///     numbers, and reading a rising clock against a fixed target means doing the subtraction
///     yourself, every second, while driving. So the subtraction is the display: the medal
///     still in reach, and the time until it is not. It turns red as it runs out, and when
///     both medals are gone it stops pretending and shows the two gaps instead.
///     Every row is the same width and every line is always written, because a block that
///     changes shape moves the number the eye came to read. Rows that do not apply right now
///     are written in a fully transparent colour rather than left out, and so is the sign in
///     front of a time that has none - a missing minus would slide the digits one glyph left.
///     The countdown clock is GameMaster's own physics time, which a restart sets to -1.749 and
///     which reaches zero at the instant the zeepkists are released. That is already the clock
///     this wants, to the frame, so there is no second one running alongside it - the run's own
///     ticker cannot serve here because it does not start until the release and the game reads
///     the finish time straight off it.
///     The lamps sit above the clock, where a start light belongs. All three are always on
///     screen and only their colour changes, because three lamps appearing one at a time move
///     the row under the eye that is trying to read it. Each change gets a beep and the release
///     gets one an octave up (see <see cref="StartLightSounds" />), so the start can be taken
///     without looking at all. On a level whose author time is under two seconds the green light
///     would still be on when the run is already decided, so it is skipped there.
///     No Harmony patch. The game writes this label from ReadyToReset.Update, and Unity runs
///     every LateUpdate after every Update, so writing from a LateUpdate of our own wins the
///     frame deterministically. It also means there is nothing to undo: stop writing and the
///     game's own text is back one frame later.
/// </summary>
public class RaceTimeDisplay : IDisposable
{
	private const string MonoSpace = "0.62em";

	private const string AuthorLabel = "AT";
	private const string GoldLabel = "G";

	private const int LabelWidth = 3;

	/// <summary>Glyphs in "00:00.000".</summary>
	private const int TimeWidth = 9;

	/// <summary>Label, sign and time, the width every row is padded or centred to.</summary>
	private const int RowWidth = LabelWidth + 1 + TimeWidth;

	private const string MinusSign = "-";
	private const string PlusSign = "+";

	/// <summary>Drawn but not seen: the row keeps its place, the player keeps their eye still.</summary>
	private const string HiddenHex = "#00000000";

	/// <summary>
	///     What GameMaster sets its physics clock to when a level restarts. It ticks up from
	///     there and the start block releases the moment it hits zero.
	/// </summary>
	private const float CountdownSeconds = 1.749f;

	private const float SecondLampAt = 0.75f;
	private const float ThirdLampAt = 1.25f;

	private const float GreenHoldSeconds = 1f;

	private const string LampGlyph = "o";

	/// <summary>Glyphs in "o o o", so the row can be centred without measuring the markup.</summary>
	private const int LampRowWidth = 5;

	/// <summary>
	///     The game's label holds one line and grows downwards. This one holds four, so it is
	///     lifted by three of them - otherwise the block starts where the single line used to sit
	///     and runs down over everything below it.
	/// </summary>
	private const float LinesUp = 3f;

	private const int Lamps = 3;
	private const int NoLights = 0;
	private const int GreenStage = Lamps + 1;

	private const double ShortestLevelWorthLights = 2d;

	private const double CloseFraction = 0.75;
	private const double CriticalFraction = 0.92;

	private static readonly string PlainHex = Hex(Color.Style.Surface.White);
	private static readonly string AuthorHex = Hex(Color.Zeepkist.Medal.Author);
	private static readonly string GoldHex = Hex(Color.Zeepkist.Medal.Gold);

	private static readonly string SafeHex = Hex(Color.Style.Status.Good);
	private static readonly string CloseHex = Hex(Color.Style.Status.Warning);
	private static readonly string CriticalHex = Hex(Color.Style.Status.Danger);

	private static readonly string AmberHex = Hex(Color.Style.Pace.Close);
	private static readonly string GreenHex = Hex(Color.Style.Status.Positive);
	private static readonly string UnlitHex = Hex(Color.Style.Surface.Unlit);

	/// <summary>The place a sign takes up on a time that has none.</summary>
	private static readonly string NoSign = Paint(MinusSign, HiddenHex);

	private readonly RaceTimeBehaviour _behaviour;

	private readonly Dictionary<TMP_Text, LabelState> _borrowed = new();

	private readonly StartLightSounds _sounds;

	private int _lastStage = NoLights;

	public RaceTimeDisplay()
	{
		GameObject host = new(nameof(RaceTimeDisplay)) { hideFlags = HideFlags.HideAndDontSave };

		Object.DontDestroyOnLoad(host);
		_behaviour = host.AddComponent<RaceTimeBehaviour>();
		_behaviour.Bind(this);
		_sounds = new StartLightSounds(host);
	}

	public AthStateMachine ActiveRun
	{
		get;
		set
		{
			field = value;

			if (value == null)
			{
				Restore();
			}
		}
	}

	private bool IsActive => ActiveRun?.Ctx.CurrentLevel != null;

	public void Dispose()
	{
		Restore();
		_sounds.Dispose();

		if (_behaviour != null)
		{
			Object.Destroy(_behaviour.gameObject);
		}
	}

	public void LateUpdate()
	{
		if (!IsActive)
		{
			Restore();
			return;
		}

		try
		{
			Draw();
		}
		catch (Exception e)
		{
			Logger.LogError($"RaceTimeDisplay: Failed, switching it off: {e.Message}\n{e.StackTrace}");
			ActiveRun = null;
		}
	}

	private void Draw()
	{
		PlayerManager manager = PlayerManager.Instance;

		if (manager == null || manager.currentMaster == null)
		{
			return;
		}

		GameMaster master = manager.currentMaster;
		List<ReadyToReset> players = master.PlayersReady;

		if (players == null || players.Count == 0 || master.setupScript == null)
		{
			return;
		}

		LevelScriptableObject level = master.setupScript.GlobalLevel;

		if (level == null)
		{
			return;
		}

		ReadyToReset player = players[0];

		if (player == null || player.ticker == null || player.screenPointer == null)
		{
			return;
		}

		TMP_Text label = player.screenPointer.elapsedTime;

		if (label == null)
		{
			return;
		}

		Borrow(label);
		label.text = Compose(master.currentLevelPhysicsTime, Elapsed(player), level.TimeGold, level.TimeAuthor);
	}

	private double Elapsed(ReadyToReset player)
	{
		AthCtx ctx = ActiveRun?.Ctx;

		if (ctx?.CurrentLevel != null && !ctx.CurrentLevel.IsTiming && ctx.LastRunTime >= 0)
		{
			return ctx.LastRunTime;
		}

		return player.ticker.GetTicker();
	}

	/// <summary>
	///     The whole block, always written in full: the attempt, the lamps, the clock and the
	///     medal still in reach. What does not apply right now is painted in
	///     <see cref="HiddenHex" /> instead of being left out, so no row ever moves.
	/// </summary>
	private string Compose(float physicsTime, double elapsed, double goldTime, double authorTime)
	{
		int stage = Stage(physicsTime, authorTime);

		Announce(stage);

		bool countingDown = stage != NoLights && physicsTime < 0f;
		double run = Math.Max(0d, elapsed);

		return Mono(AttemptRow(countingDown)
		            + "\n" + LampRow(stage)
		            + "\n" + ClockRow(countingDown, countingDown ? -physicsTime : run)
		            + "\n" + MedalRows(countingDown, run, goldTime, authorTime));
	}

	private static int Stage(float physicsTime, double authorTime)
	{
		if (!Plugin.Instance.MyConfig.StartLights.Value)
		{
			return NoLights;
		}

		if (physicsTime < 0f)
		{
			return LitLamps(CountdownSeconds + physicsTime);
		}

		if (physicsTime < GreenHoldSeconds && authorTime > ShortestLevelWorthLights)
		{
			return GreenStage;
		}

		return NoLights;
	}

	private static int LitLamps(float countedDown)
	{
		if (countedDown < SecondLampAt)
		{
			return 1;
		}

		return countedDown < ThirdLampAt ? 2 : Lamps;
	}

	/// <summary>
	///     One click per lamp and a different one on the release, played on the edge so a stage
	///     that lasts half a second does not turn into a drum roll. A sound that will not play is
	///     not worth losing the display over, so it is caught here rather than upstairs.
	/// </summary>
	private void Announce(int stage)
	{
		if (stage == _lastStage)
		{
			return;
		}

		_lastStage = stage;

		try
		{
			Play(stage);
		}
		catch (Exception e)
		{
			Logger.LogWarning($"RaceTimeDisplay: Could not play the start light sound: {e.Message}");
		}
	}

	private void Play(int stage)
	{
		if (stage == GreenStage)
		{
			_sounds.Go();

			return;
		}

		if (stage != NoLights)
		{
			_sounds.Lamp();
		}
	}

	private string AttemptRow(bool countingDown)
	{
		Level level = ActiveRun?.Ctx.CurrentLevel;
		string attempt = level == null ? "" : $"Attempt {level.Attempt + 1}";

		return Centered(attempt.Length) + Paint(attempt, countingDown ? PlainHex : HiddenHex);
	}

	private static string LampRow(int stage)
	{
		return Centered(LampRowWidth)
		       + Paint(LampGlyph, LampHex(1, stage)) + " "
		       + Paint(LampGlyph, LampHex(2, stage)) + " "
		       + Paint(LampGlyph, LampHex(3, stage));
	}

	private static string LampHex(int lamp, int stage)
	{
		if (stage == NoLights)
		{
			return HiddenHex;
		}

		if (stage == GreenStage)
		{
			return GreenHex;
		}

		return lamp <= stage ? AmberHex : UnlitHex;
	}

	private static string ClockRow(bool countingDown, double clock)
	{
		if (countingDown)
		{
			return Row("", PlainHex, Paint(MinusSign, AmberHex), TimeFormatter.FormatTime(clock), AmberHex);
		}

		return Row("", PlainHex, NoSign, TimeFormatter.FormatTime(clock), PlainHex);
	}

	private static string MedalRows(bool countingDown, double elapsed, double goldTime, double authorTime)
	{
		if (countingDown || goldTime <= 0d || authorTime <= 0d)
		{
			return HiddenRow();
		}

		if (elapsed < authorTime)
		{
			return Chase(AuthorLabel, AuthorHex, elapsed, authorTime);
		}

		if (elapsed < goldTime)
		{
			return Chase(GoldLabel, GoldHex, elapsed, goldTime);
		}

		return Lost(GoldLabel, GoldHex, elapsed - goldTime)
		       + "\n" + Lost(AuthorLabel, AuthorHex, elapsed - authorTime);
	}

	private static string Chase(string label, string labelHex, double elapsed, double target)
	{
		string paceHex = PaceHex(elapsed, target);

		return Row(label, labelHex, Paint(MinusSign, paceHex), TimeFormatter.FormatTime(target - elapsed), paceHex);
	}

	private static string Lost(string label, string labelHex, double over)
	{
		return Row(label, labelHex, Paint(PlusSign, PlainHex), TimeFormatter.FormatTime(over), PlainHex);
	}

	/// <summary>A row nobody can see, so the one below it does not climb a line when it appears.</summary>
	private static string HiddenRow()
	{
		return Row(AuthorLabel, HiddenHex, NoSign, TimeFormatter.FormatTime(0d), HiddenHex);
	}

	private static string Row(string label, string labelHex, string sign, string time, string timeHex)
	{
		return Paint(label.PadRight(LabelWidth), labelHex) + sign + Paint(time, timeHex);
	}

	private static string Centered(int width)
	{
		return new string(' ', Math.Max(0, (RowWidth - width) / 2));
	}

	private static string Paint(string text, string hex)
	{
		return $"<color={hex}>{text}</color>";
	}

	private static string Mono(string text)
	{
		return $"<mspace={MonoSpace}>{text}</mspace>";
	}

	private static string PaceHex(double elapsed, double target)
	{
		double fraction = elapsed / target;

		if (fraction < CloseFraction)
		{
			return SafeHex;
		}

		return fraction < CriticalFraction ? CloseHex : CriticalHex;
	}

	private static string Hex(Color32 colour)
	{
		return $"#{colour.r:X2}{colour.g:X2}{colour.b:X2}";
	}

	private void Borrow(TMP_Text label)
	{
		if (_borrowed.ContainsKey(label))
		{
			return;
		}

		RectTransform transform = label.rectTransform;

		_borrowed[label] = new LabelState(label.overflowMode, label.enableAutoSizing, label.fontSize,
			label.enableWordWrapping, transform.anchoredPosition);

		label.overflowMode = TextOverflowModes.Overflow;

		label.enableWordWrapping = false;

		label.enableAutoSizing = false;

		transform.anchoredPosition += new Vector2(0f, label.fontSize * LinesUp);
	}

	private void Restore()
	{
		foreach (KeyValuePair<TMP_Text, LabelState> entry in _borrowed)
		{
			if (entry.Key == null)
			{
				continue;
			}

			entry.Key.overflowMode = entry.Value.Overflow;
			entry.Key.enableAutoSizing = entry.Value.AutoSizing;
			entry.Key.fontSize = entry.Value.FontSize;
			entry.Key.enableWordWrapping = entry.Value.WordWrapping;
			entry.Key.rectTransform.anchoredPosition = entry.Value.Position;
		}

		_borrowed.Clear();
	}
}
