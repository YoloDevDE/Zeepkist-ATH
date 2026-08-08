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

namespace AuthorTimeHunting.UI.Hud;

/// <summary>
///     Rewrites the game's own running-time display into the only number that decides
///     anything: how much time is left before the next medal is gone.
///     <code>
///     counting down          driving               all medals gone
/// 
///        ● ● ●
///      Attempt 3
///      -00:01.749             00:12.340             00:34.343
///     AT -00:32.123          G  +00:02.100
///                            AT +00:12.300
///     </code>
///     The time counting up was never the question. Every run is a race against one of two
///     numbers, and reading a rising clock against a fixed target means doing the subtraction
///     yourself, every second, while driving. So the subtraction is the display: the medal
///     still in reach, and the time until it is not. It goes yellow as that time gets short and
///     flashes red once it is nearly out, and when both medals are gone it stops pretending: the
///     clock itself turns red and the two gaps take the place of the countdown.
///     Every row is the same width and every line is always written, because a block that
///     changes shape moves the number the eye came to read. Rows that do not apply right now
///     are written in a fully transparent colour rather than left out, and so is the sign in
///     front of a time that has none - a missing minus would slide the digits one glyph left.
///     The countdown clock is GameMaster's own physics time, which a restart sets to -1.749 and
///     which reaches zero at the instant the zeepkists are released. That is already the clock
///     this wants, to the frame, so there is no second one running alongside it - the run's own
///     ticker cannot serve here because it does not start until the release and the game reads
///     the finish time straight off it.
///     A run that is over stops being a race, and the medal row says which way it went. Chasing a
///     medal it had been reddening against, it turns the colour of a medal taken; past both of
///     them it turns the colour of one lost. Leaving either as the warning it was while driving
///     reads as a verdict that never came. The game's own finish panel gets the verdict in words,
///     in place of a time nobody needs twice (see <see cref="FinishVerdict" />).
///     The lamps sit above the clock, where a start light belongs: three red, then three amber,
///     then three green on the release. All three are always on screen and only their colour
///     changes, because lamps appearing one at a time move the row under the eye that is trying to
///     read it. Each change gets a beep and the release gets one an octave up (see
///     <see cref="HudSounds" />), so the start can be taken without looking at all. On a level
///     whose author time is under two seconds the green light would still be on when the run is
///     already decided, so it is skipped there.
///     What it deliberately does not do is copy the light on the start block. That one holds red
///     for one and a quarter seconds and amber for half of one, which is not a rhythm - it is a
///     wait and then a surprise, and a start cannot be timed against it. These lamps split the
///     countdown evenly instead, so red, amber and green are the same distance apart and the
///     third one is where the second one told you it would be.
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

	/// <summary>
	///     Halfway, which is the whole point: red at the start of the countdown, amber here, green
	///     on the release puts the same gap between each pair. The game's own TrafficLightCountdown
	///     goes amber at 1.25s of its 1.75, so its red lasts two and a half times as long as its
	///     amber, and the two clocks are one clock - so this could have been inherited and is not.
	/// </summary>
	private const float AmberAt = CountdownSeconds / 2f;

	private const float GreenHoldSeconds = 1f;

	/// <summary>
	///     The game's label holds one line and grows downwards, and this one holds four. Lifting
	///     it by all three would put the block where the single line used to be and nothing where
	///     the eye goes; one line up is the compromise that keeps the clock near its old place.
	/// </summary>
	private const float LinesUp = 1f;

	private const double ShortestLevelWorthLights = 2d;

	/// <summary>Glyphs in "● ● ●", so the row can be centred without measuring the markup.</summary>
	private const int LampRowWidth = 5;

	private const int NoLights = 0;

	/// <summary>
	///     The three the light actually has. Filling the lamps in one at a time gave five stages and
	///     five beeps for one and a half seconds of countdown, which is a drum roll rather than a
	///     start light - the real one on the start block has three bulbs and lights one at a time.
	/// </summary>
	private const int AllRed = 1;

	private const int AllAmber = 2;

	private const int GreenStage = 3;

	private const double CloseFraction = 0.75;
	private const double CriticalFraction = 0.92;

	/// <summary>
	///     How the chase is going, in the three steps the colour already had. Kept as stages rather
	///     than a flag per sound so each one is announced on its edge, the way the lamps are, and so
	///     a medal falling away and the next one being picked up walks back down through them.
	/// </summary>
	private const int PaceSafe = 0;

	private const int PaceClose = 1;

	private const int PaceCritical = 2;

	/// <summary>
	///     How often the warning swings from yellow to red and back. Slow enough to read as a
	///     pulse rather than a strobe - a hard switch between two colours four times a second was
	///     the loudest thing on the screen at the moment the player most needs to look at the
	///     track.
	/// </summary>
	private const float PulsesPerSecond = 1.2f;

	/// <summary>
	///     The lamp, in order of preference, from the big filled circle down to a letter every font
	///     on earth has. The HUD font has none of the good ones and cannot be given them (see
	///     <see cref="GlyphChoice" />), so it is asked which of these it can draw rather than told.
	///     The bullet is the one that matters: it is a filled circle, it is small, and a font
	///     without it does not exist.
	/// </summary>
	private static readonly string[] LampGlyphs = ["●", "⬤", "◉", "•", "O"];

	private static readonly string PlainHex = Hex(Color.Style.Surface.White);
	private static readonly string AuthorHex = Hex(Color.Zeepkist.Medal.Author);
	private static readonly string GoldHex = Hex(Color.Zeepkist.Medal.Gold);

	/// <summary>
	///     How the medal being chased is going: comfortable, then tight. Read at a glance out of the
	///     corner of an eye while driving, so they are the saturated ends of the palette rather than
	///     the muted ones a panel would use. The last stretch has no colour of its own - it is a
	///     pulse between two of these, mixed where it is used.
	/// </summary>
	private static readonly string SafeHex = Hex(Color.Style.Status.Positive);

	private static readonly string CloseHex = Hex(Color.Style.Status.Close);

	private static readonly string AmberHex = Hex(Color.Style.Status.Close);
	private static readonly string RedHex = Hex(Color.Style.Status.Alert);
	private static readonly string GreenHex = Hex(Color.Style.Status.Positive);

	/// <summary>The medal is in the bag: nothing left to run out of.</summary>
	private static readonly string ClaimedHex = Hex(Color.Style.Status.Positive);

	/// <summary>Nothing left to chase. Full red, the one colour that cannot be read as anything else.</summary>
	private static readonly string MissedHex = Hex(Color.Style.Status.Alert);

	/// <summary>The place a sign takes up on a time that has none.</summary>
	private static readonly string NoSign = Paint(MinusSign, HiddenHex);

	/// <summary>A label cell with nothing in it, for the row that is only a clock.</summary>
	private static readonly string EmptyCell = new(' ', LabelWidth);

	/// <summary>What follows a medal, the sprite itself taking the first of the cell's three places.</summary>
	private static readonly string BesideSprite = new(' ', LabelWidth - 1);

	private readonly RaceTimeBehaviour _behaviour;

	private readonly Dictionary<TMP_Text, LabelState> _borrowed = new();

	private readonly MedalSpriteAsset _medals = new();

	private readonly HudSounds _sounds = new();

	private readonly FinishVerdict _verdict = new();

	/// <summary>
	///     The lamp row as it looks at every stage there is, built once the label has said which
	///     shape it can draw. This is written every frame of every run, and there are only six.
	/// </summary>
	private string[] _lampRows;

	/// <summary>How the chase last sounded, so each step is played once as it is entered.</summary>
	private int _lastPace = PaceSafe;

	private int _lastStage = NoLights;

	/// <summary>Whether this attempt has already been told the author time is behind it.</summary>
	private bool _missedAuthor;

	/// <summary>Whether the label took the medal sprites. If it did not, the two letters stand in.</summary>
	private bool _sprites;

	public RaceTimeDisplay()
	{
		GameObject host = new(nameof(RaceTimeDisplay)) { hideFlags = HideFlags.HideAndDontSave };

		Object.DontDestroyOnLoad(host);
		_behaviour = host.AddComponent<RaceTimeBehaviour>();
		_behaviour.Bind(this);
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

	private string AuthorCell => Cell(MedalSpriteAsset.Author, AuthorLabel, AuthorHex, PlainHex);

	private string GoldCell => Cell(MedalSpriteAsset.Gold, GoldLabel, GoldHex, PlainHex);

	/// <summary>
	///     The attempt is over and its time is on the board. The level's clock is only stopped
	///     between a scored finish and the next attempt, so a finish the hunt would not score -
	///     a checkpoint missed - never counts as one.
	/// </summary>
	private bool Finished
	{
		get
		{
			AthCtx ctx = ActiveRun?.Ctx;

			return ctx?.CurrentLevel != null && !ctx.CurrentLevel.IsTiming && ctx.LastRunTime >= 0;
		}
	}

	public void Dispose()
	{
		Restore();
		_sounds.Dispose();
		_medals.Dispose();

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

		int stage = Stage(master.currentLevelPhysicsTime, level.TimeAuthor);
		double elapsed = Elapsed(player);

		Announce(stage);
		Rearm(master.currentLevelPhysicsTime);
		Mourn(elapsed, level.TimeAuthor);

		label.text = Compose(stage, master.currentLevelPhysicsTime, elapsed, level.TimeGold, level.TimeAuthor);

		Judge(player.screenPointer.resultTime);
	}

	/// <summary>A new attempt is a new warning, and the countdown running is what says one has begun.</summary>
	private void Rearm(float physicsTime)
	{
		if (physicsTime >= 0f)
		{
			return;
		}

		_lastPace = PaceSafe;
		_missedAuthor = false;
	}

	/// <summary>
	///     The author time going past, said once. That is the moment the run stops being the run it
	///     set out to be, and all the display does about it is start counting a different row - a
	///     sound is what makes it land on a player who is looking at the track.
	/// </summary>
	private void Mourn(double elapsed, double authorTime)
	{
		if (_missedAuthor || authorTime <= 0d || elapsed < authorTime || Finished)
		{
			return;
		}

		_missedAuthor = true;

		try
		{
			_sounds.Missed();
		}
		catch (Exception e)
		{
			Logger.LogWarning($"RaceTimeDisplay: Could not play the missed author time: {e.Message}");
		}
	}

	private void Judge(TMP_Text label)
	{
		if (!Finished)
		{
			_verdict.Restore();
			return;
		}

		_verdict.Draw(label, ActiveRun.Ctx);
	}

	private double Elapsed(ReadyToReset player)
	{
		if (Finished)
		{
			return ActiveRun.Ctx.LastRunTime;
		}

		return player.ticker.GetTicker();
	}

	/// <summary>
	///     The whole block, always written in full: the attempt, the lamps, the clock and the
	///     medal still in reach. What does not apply right now is painted in
	///     <see cref="HiddenHex" /> instead of being left out, so no row ever moves.
	/// </summary>
	private string Compose(int stage, float physicsTime, double elapsed, double goldTime, double authorTime)
	{
		bool countingDown = stage != NoLights && physicsTime < 0f;
		double run = Math.Max(0d, elapsed);

		bool gone = !countingDown && goldTime > 0d && run >= goldTime;

		return Mono(AttemptRow(countingDown)
		            + "\n" + _lampRows[stage]
		            + "\n" + ClockRow(countingDown, countingDown ? -physicsTime : run, gone)
		            + "\n" + MedalRows(countingDown, Finished, run, goldTime, authorTime));
	}

	private static int Stage(float physicsTime, double authorTime)
	{
		if (!Plugin.Instance.MyConfig.StartLights.Value)
		{
			return NoLights;
		}

		if (physicsTime < 0f)
		{
			return Countdown(CountdownSeconds + physicsTime);
		}

		if (physicsTime < GreenHoldSeconds && authorTime > ShortestLevelWorthLights)
		{
			return GreenStage;
		}

		return NoLights;
	}

	private static int Countdown(float countedDown)
	{
		return countedDown < AmberAt ? AllRed : AllAmber;
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
			PlayStage(stage);
		}
		catch (Exception e)
		{
			Logger.LogWarning($"RaceTimeDisplay: Could not play the start light sound: {e.Message}");
		}
	}

	private void PlayStage(int stage)
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

	private static string[] BuildLampRows(string glyph)
	{
		string[] rows = new string[GreenStage + 1];

		for (int stage = 0; stage < rows.Length; stage++)
		{
			rows[stage] = LampRow(stage, glyph);
		}

		return rows;
	}

	private static string LampRow(int stage, string glyph)
	{
		string hex = LampHex(stage);

		return Centered(LampRowWidth) + Paint($"{glyph} {glyph} {glyph}", hex);
	}

	/// <summary>Three red, three amber, three green - the whole light, one colour at a time.</summary>
	private static string LampHex(int stage)
	{
		if (stage == NoLights)
		{
			return HiddenHex;
		}

		if (stage == GreenStage)
		{
			return GreenHex;
		}

		return stage == AllRed ? RedHex : AmberHex;
	}

	private string AttemptRow(bool countingDown)
	{
		Level level = ActiveRun?.Ctx.CurrentLevel;
		string attempt = level == null ? "" : $"Attempt {level.Attempt + 1}";

		return Centered(attempt.Length) + Paint(attempt, countingDown ? PlainHex : HiddenHex);
	}

	/// <summary>
	///     The clock itself goes red the moment the last medal is out of reach and stays that way,
	///     driving or finished. Up to there it is the neutral number the gaps below it are measured
	///     against; past it there is nothing left to measure, and the run being a write-off is the
	///     only thing the display still has to say.
	/// </summary>
	private static string ClockRow(bool countingDown, double clock, bool gone)
	{
		if (countingDown)
		{
			return Row(EmptyCell, Paint(MinusSign, AmberHex), TimeFormatter.FormatTime(clock), AmberHex);
		}

		return Row(EmptyCell, NoSign, TimeFormatter.FormatTime(clock), gone ? MissedHex : PlainHex);
	}

	private string MedalRows(bool countingDown, bool finished, double elapsed, double goldTime,
		double authorTime)
	{
		if (countingDown || goldTime <= 0d || authorTime <= 0d)
		{
			return HiddenRow();
		}

		if (elapsed < authorTime)
		{
			return Chase(AuthorCell, finished, elapsed, authorTime);
		}

		if (elapsed < goldTime)
		{
			return Chase(GoldCell, finished, elapsed, goldTime);
		}

		return Lost(GoldCell, finished, elapsed - goldTime)
		       + "\n" + Lost(AuthorCell, finished, elapsed - authorTime);
	}

	/// <summary>A row nobody can see, so the one below it does not climb a line when it appears.</summary>
	private string HiddenRow()
	{
		return Row(Cell(MedalSpriteAsset.Author, AuthorLabel, HiddenHex, HiddenHex), NoSign,
			TimeFormatter.FormatTime(0d), HiddenHex);
	}

	/// <summary>
	///     What a row is labelled with: the medal itself where the game's art and the label are both
	///     there for it, and the letters it used to say where they are not. A medal is recognised
	///     where "AT" has to be read, and at the size this is written it is the difference between
	///     glancing at the row and looking at it.
	///     The sprite is tinted white rather than the colour the letters carried - white is what
	///     leaves the medal its own colours, which is the whole reason for using the art.
	/// </summary>
	private string Cell(int medal, string text, string textHex, string spriteHex)
	{
		if (!_sprites)
		{
			return Paint(text.PadRight(LabelWidth), textHex);
		}

		return MedalSpriteAsset.Tag(medal, spriteHex) + BesideSprite;
	}

	private string Chase(string cell, bool finished, double elapsed, double target)
	{
		string paceHex = finished ? ClaimedHex : PaceHex(elapsed, target);

		return Row(cell, Paint(MinusSign, paceHex), TimeFormatter.FormatTime(target - elapsed), paceHex);
	}

	/// <summary>
	///     A medal that is already gone. While driving that is just how far past it the clock is;
	///     once the attempt is over it is the result, and reads as one.
	/// </summary>
	private static string Lost(string cell, bool finished, double over)
	{
		string hex = finished ? MissedHex : PlainHex;

		return Row(cell, Paint(PlusSign, hex), TimeFormatter.FormatTime(over), hex);
	}

	private static string Row(string cell, string sign, string time, string timeHex)
	{
		return cell + sign + Paint(time, timeHex);
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

	/// <summary>
	///     The last stretch breathes between the two warning colours rather than sitting on one.
	///     Colour alone is something the eye stops seeing after a second of it; movement is not, and
	///     this is the moment the player has to decide whether the run is still worth finishing. It
	///     is a fade rather than a switch, because a switch is read as a fault light and this is a
	///     warning - and the number stays fully readable at every point of it either way.
	/// </summary>
	private string PaceHex(double elapsed, double target)
	{
		int pace = Pace(elapsed / target);

		Sound(pace);

		if (pace == PaceSafe)
		{
			return SafeHex;
		}

		if (pace == PaceClose)
		{
			return CloseHex;
		}

		return Hex(Color32.Lerp(Color.Style.Status.Close, Color.Style.Status.Alert, Pulse()));
	}

	private static int Pace(double fraction)
	{
		if (fraction < CloseFraction)
		{
			return PaceSafe;
		}

		return fraction < CriticalFraction ? PaceClose : PaceCritical;
	}

	/// <summary>
	///     A sine between 0 and 1. Unscaled, so the rhythm is the same one every time rather than
	///     one the game's own slowdowns can stretch.
	/// </summary>
	private static float Pulse()
	{
		return (Mathf.Sin(Time.unscaledTime * PulsesPerSecond * 2f * Mathf.PI) + 1f) * 0.5f;
	}

	/// <summary>
	///     One tone as each step is entered, and none while it lasts. The colour says how long is
	///     left from then on; a sound repeating that every second would be the mod shouting.
	///     Stepping back down is silent, which is what lets a medal falling away work: the chase
	///     moves to the next one, the fraction drops, and that medal gets its own warning later
	///     instead of inheriting one already spent.
	/// </summary>
	private void Sound(int pace)
	{
		if (pace == _lastPace)
		{
			return;
		}

		_lastPace = pace;

		try
		{
			PlayPace(pace);
		}
		catch (Exception e)
		{
			Logger.LogWarning($"RaceTimeDisplay: Could not play the warning: {e.Message}");
		}
	}

	/// <summary>
	///     A single beep as the medal gets tight, two as it is about to be gone. One tone for both
	///     would make the second warning the same news as the first, and the second one is the one
	///     that decides whether the run is still worth finishing.
	/// </summary>
	private void PlayPace(int pace)
	{
		if (pace == PaceClose)
		{
			_sounds.Warning();

			return;
		}

		if (pace == PaceCritical)
		{
			_sounds.Alert();
		}
	}

	private static string Hex(Color32 colour)
	{
		return $"#{colour.r:X2}{colour.g:X2}{colour.b:X2}";
	}

	/// <summary>
	///     Asks for the medals on every frame that has not got them yet, rather than once when the
	///     label is first taken. The sprites are cut out of the game's own art, and the game loads
	///     that when it feels like it - a label borrowed one frame too early answered "no medals"
	///     and then kept that answer for the rest of the session, which is how a feature that works
	///     ends up showing "AT" and "G" forever.
	/// </summary>
	private void Dress(TMP_Text label)
	{
		if (_sprites)
		{
			return;
		}

		_sprites = _medals.Install(label);
	}

	/// <summary>
	///     Taking the label and then dressing it, in that order: what the label had is written down
	///     first, or the sprite asset put on it a moment later is what <see cref="Restore" /> would
	///     hand back to the game.
	/// </summary>
	private void Borrow(TMP_Text label)
	{
		Adopt(label);
		Dress(label);
	}

	private void Adopt(TMP_Text label)
	{
		if (_borrowed.ContainsKey(label))
		{
			return;
		}

		_lampRows ??= BuildLampRows(GlyphChoice.First(label, LampGlyphs));

		RectTransform transform = label.rectTransform;

		_borrowed[label] = new LabelState(label.overflowMode, label.enableAutoSizing, label.fontSize,
			label.enableWordWrapping, transform.anchoredPosition, label.spriteAsset);

		label.overflowMode = TextOverflowModes.Overflow;

		label.enableWordWrapping = false;

		label.enableAutoSizing = false;

		transform.anchoredPosition += new Vector2(0f, label.fontSize * LinesUp);
	}

	/// <summary>
	///     Handing the labels back includes handing back their sprite assets, so the next run has
	///     to be dressed again from scratch. Leaving <see cref="_sprites" /> set was the same bug
	///     the medals already had once, one run later: the display would go on writing sprite tags
	///     against whatever sprite asset the game keeps on that label.
	/// </summary>
	private void Restore()
	{
		_verdict.Restore();
		_sprites = false;

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
			entry.Key.spriteAsset = entry.Value.SpriteAsset;
		}

		_borrowed.Clear();
	}
}
