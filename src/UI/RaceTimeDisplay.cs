using System;
using System.Collections.Generic;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using TMPro;
using UnityEngine;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.UI;

/// <summary>
///     Rewrites the game's own running-time display into two aligned lines: the time itself,
///     and under it the best medal still within reach with the time it needs.
///     <code>
///        00:43.123          00:45.444        00:49.000
///     AT 00:45.423     GOLD 00:48.345        missed
///     </code>
///     The target line is static - it names a medal and its time, and only changes when a
///     medal drops out of reach. That is the whole reading: the number above is chasing the
///     number below, and how far apart they are is visible without doing the subtraction.
///     Deltas came before this and were too much to take in mid-run.
///     No Harmony patch. The game writes this label from ReadyToReset.Update, and Unity runs
///     every LateUpdate after every Update, so writing from a LateUpdate of our own wins the
///     frame deterministically. It also means there is nothing to undo: stop writing and the
///     game's own text is back one frame later.
/// </summary>
public class RaceTimeDisplay : IDisposable
{
	/// <summary>
	///     Character width for the monospacing tag. The three lines only line up if the digits
	///     do, and the game's font is proportional.
	/// </summary>
	private const string MonoSpace = "0.62em";

	// Words rather than the game's medal sprites: a TMP sprite tag only resolves against a
	// sprite asset assigned to the label, and the running-time label has none. Assigning one
	// means borrowing another mod's asset bundle, which ATH does not depend on.
	private const string AuthorLabel = "AT";
	private const string GoldLabel = "GOLD";
	private const string MissedLabel = "missed";

	/// <summary>
	///     Share of a target time at which it counts as "closing in". Everything before this is
	///     the same colour, because a warning that is on from the start line is not a warning.
	/// </summary>
	private const double CloseFraction = 0.85;

	private readonly RaceTimeBehaviour _behaviour;

	/// <summary>Restored when we stop writing, because we did not own them.</summary>
	private readonly Dictionary<TMP_Text, LabelState> _borrowed = new();

	public RaceTimeDisplay()
	{
		GameObject host = new(nameof(RaceTimeDisplay)) { hideFlags = HideFlags.HideAndDontSave };

		Object.DontDestroyOnLoad(host);
		_behaviour = host.AddComponent<RaceTimeBehaviour>();
		_behaviour.Bind(this);
	}

	/// <summary>
	///     The run currently in progress, or null when ATH is idle. Set by StateMasterOn.
	/// </summary>
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

	/// <summary>
	///     Whether the medal lines are ours to write. A run exists from /ath start onwards, but
	///     for the first few seconds of it there is no level yet - only the countdown and the
	///     playlist being rewritten. Writing a medal target over that meant the game's timer
	///     showed an author time belonging to whatever map the lobby happened to be sitting on.
	///     A level in the context is exactly the moment the first run begins.
	/// </summary>
	private bool IsActive => ActiveRun?.Ctx.CurrentLevel != null;

	public void Dispose()
	{
		Restore();

		if (_behaviour != null)
		{
			Object.Destroy(_behaviour.gameObject);
		}
	}

	private void LateUpdate()
	{
		if (!IsActive)
		{
			// No-op once the labels have been handed back, so this costs nothing per frame.
			Restore();
			return;
		}

		try
		{
			Draw();
		}
		catch (Exception e)
		{
			// Every frame, so it must not be allowed to spam. One failure disables it.
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

		// Local players only - PlayersReady is the split-screen list, and online opponents
		// are ghosts that never appear in it. Index 0 is us in every setup ATH supports.
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
		label.text = Compose(Elapsed(player), level.TimeGold, level.TimeAuthor);
	}

	/// <summary>
	///     The time to show: the live ticker while the player is driving, and the time they
	///     finished with once they are not.
	///     The game's ticker keeps counting past the finish line - it is reset by the next
	///     spawn, not by crossing - so the number carried on climbing while the player sat on
	///     the round-over screen, and the run they had just done was gone before they could
	///     read it. The run's own clock is the honest signal for "still driving", because it is
	///     the same one the time budget is spent from.
	/// </summary>
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
	///     Builds the block. Public and static so the formatting can be reasoned about - and
	///     one day tested - without a running game.
	/// </summary>
	public static string Compose(double elapsed, double goldTime, double authorTime)
	{
		PluginConfig config = Plugin.Instance.MyConfig;

		string colour = config.RaceTimeColorChange.Value ? Hex(TimeColour(elapsed, goldTime, authorTime)) : Hex(HudPalette.White);

		string time = TimeFormatter.FormatTime(elapsed);

		if (!config.RaceTimeShowTarget.Value)
		{
			return Line(time, colour);
		}

		if (elapsed >= goldTime)
		{
			// Nothing left to chase. No indent either - there is no label to clear.
			return Line(time, colour) + "\n" + Line(MissedLabel, Hex(HudPalette.Danger));
		}

		bool author = elapsed < authorTime;
		string label = author ? AuthorLabel : GoldLabel;
		Color32 medal = author ? HudPalette.Author : HudPalette.Gold;
		string target = TimeFormatter.FormatTime(author ? authorTime : goldTime);

		// The running time is pushed right by the label plus its separating space, so the two
		// times sit in the same column. Monospacing is what makes counting characters legal.
		string indent = new(' ', label.Length + 1);

		return Line(indent + time, colour) + "\n" + Line($"{label} {target}", Hex(medal));
	}

	private static string Line(string text, string colour)
	{
		return $"<mspace={MonoSpace}><color={colour}>{text}</color></mspace>";
	}

	/// <summary>
	///     How close the attempt is to losing the medal it is currently chasing, as five steps
	///     from white to red.
	///     It used to be the medal's own colour - magenta while the author time was ahead, gold
	///     after - which said which medal was in play and nothing about how it was going. Magenta
	///     at a tenth of a second before the author time looked exactly like magenta at the start
	///     line. The ladder answers the question actually being asked mid-run: is this attempt
	///     still worth finishing.
	/// </summary>
	private static Color32 TimeColour(double elapsed, double goldTime, double authorTime)
	{
		if (elapsed >= goldTime)
		{
			return HudPalette.PaceGone;
		}

		if (elapsed >= goldTime * CloseFraction)
		{
			return HudPalette.PaceCritical;
		}

		if (elapsed >= authorTime)
		{
			return HudPalette.PaceLost;
		}

		return elapsed >= authorTime * CloseFraction ? HudPalette.PaceClose : HudPalette.PaceSafe;
	}

	private static string Hex(Color32 colour)
	{
		return $"#{colour.r:X2}{colour.g:X2}{colour.b:X2}";
	}

	/// <summary>
	///     Takes the label over, once, and keeps what it looked like first.
	///     Two settings have to move. Overflow, because the label is sized for one line and
	///     would otherwise clip the other two away entirely. And auto-sizing, which is what
	///     made the text shrink to a crumb: TMP is told to fit three lines into a box built
	///     for one, and it obliges by dropping the font size until they do. Turning it off
	///     lets the text keep its size and spill out of the box instead, which is what the
	///     overflow change is for.
	/// </summary>
	private void Borrow(TMP_Text label)
	{
		if (_borrowed.ContainsKey(label))
		{
			return;
		}

		_borrowed[label] = new LabelState(label.overflowMode, label.enableAutoSizing, label.fontSize,
			label.enableWordWrapping);

		label.overflowMode = TextOverflowModes.Overflow;

		// The label is a box built for one short line. With wrapping on, "AT 00:51.685" does not
		// fit and TMP breaks it after the label - which is what put the medal and its time on
		// separate lines and threw the whole block off centre. Off, the line stays a line and
		// spills sideways, which is what the overflow change above is already for.
		label.enableWordWrapping = false;

		// Switching auto-sizing off freezes fontSize at whatever is on screen right now, and
		// right now is still the game's own single line at its normal size - Borrow runs
		// before the three lines are written. So the size is already correct and must not be
		// touched. Setting it to fontSizeMax here is what blew the timer up: that is the
		// ceiling the auto-sizer was allowed to reach, not the size it was actually using.
		label.enableAutoSizing = false;
	}

	/// <summary>Hands the label back exactly as it was found.</summary>
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
		}

		_borrowed.Clear();
	}

	/// <summary>What a label looked like before ATH took it over.</summary>
	private readonly struct LabelState
	{
		public LabelState(TextOverflowModes overflow, bool autoSizing, float fontSize, bool wordWrapping)
		{
			Overflow = overflow;
			AutoSizing = autoSizing;
			FontSize = fontSize;
			WordWrapping = wordWrapping;
		}

		public TextOverflowModes Overflow { get; }
		public bool AutoSizing { get; }
		public float FontSize { get; }
		public bool WordWrapping { get; }
	}

	/// <summary>
	///     The frame hook. Separate because RaceTimeDisplay is a plain service and only a
	///     MonoBehaviour gets a LateUpdate - the same split AthStateMachine uses.
	/// </summary>
	private sealed class RaceTimeBehaviour : MonoBehaviour
	{
		private RaceTimeDisplay _owner;

		private void LateUpdate()
		{
			_owner?.LateUpdate();
		}

		public void Bind(RaceTimeDisplay owner)
		{
			_owner = owner;
		}
	}
}
