using System;
using System.Collections.Generic;
using AuthorTimeHunting.Util;
using TMPro;
using UnityEngine;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.UI;

/// <summary>
///     Rewrites the game's own running-time display into three aligned lines: the time itself,
///     how much room is left to the gold time, and how much to the author time.
///     <code>
///     00:12.480
///       +02.130
///       -00.940
///     </code>
///     A plus means headroom - that much of the target is still unspent. It flips to a minus
///     the moment the target goes past, which is the reading a hunter actually wants: not "how
///     far off am I" but "do I still have it".
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

	/// <summary>
	///     Indent on the delta lines, in monospaced characters. Two, so a delta sits under the
	///     seconds of the time above it rather than under its minutes.
	/// </summary>
	private const string DeltaIndent = "  ";

	private readonly RaceTimeBehaviour _behaviour;

	/// <summary>Restored when we stop writing, because we did not own them.</summary>
	private readonly Dictionary<TMP_Text, TextOverflowModes> _originalOverflow = new();

	public RaceTimeDisplay()
	{
		GameObject host = new(nameof(RaceTimeDisplay))
		{
			hideFlags = HideFlags.HideAndDontSave
		};

		Object.DontDestroyOnLoad(host);
		_behaviour = host.AddComponent<RaceTimeBehaviour>();
		_behaviour.Bind(this);
	}

	/// <summary>
	///     Set while a hunt is on. Outside one the game keeps its own single-line display -
	///     the extra lines are about beating a medal, which is what a hunt is.
	/// </summary>
	public bool Enabled
	{
		get;
		set
		{
			if (field == value)
			{
				return;
			}

			field = value;

			if (!value)
			{
				RestoreOverflow();
			}
		}
	}

	public void Dispose()
	{
		RestoreOverflow();

		if (_behaviour != null)
		{
			Object.Destroy(_behaviour.gameObject);
		}
	}

	private void LateUpdate()
	{
		if (!Enabled)
		{
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
			Enabled = false;
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

		float elapsed = player.ticker.GetTicker();

		RememberOverflow(label);
		label.text = Compose(elapsed, level.TimeGold, level.TimeAuthor);
	}

	/// <summary>
	///     Builds the block. Public and static so the formatting can be reasoned about - and
	///     one day tested - without a running game.
	/// </summary>
	public static string Compose(double elapsed, double goldTime, double authorTime)
	{
		PluginConfig config = Plugin.Instance.MyConfig;

		string colour = config.RaceTimeColorChange.Value
			? Hex(TimeColour(elapsed, goldTime, authorTime))
			: Hex(HudPalette.White);

		string block = Line(TimeFormatter.FormatTime(elapsed), colour);

		if (config.RaceTimeShowGold.Value)
		{
			block += "\n" + Delta(goldTime - elapsed, HudPalette.Gold);
		}

		if (config.RaceTimeShowAuthor.Value)
		{
			block += "\n" + Delta(authorTime - elapsed, HudPalette.Author);
		}

		return block;
	}

	/// <summary>
	///     Headroom, not lateness: positive while the target is still ahead, negative once it
	///     has gone by. The sign is always written, so the column never shifts.
	/// </summary>
	private static string Delta(double headroom, Color32 colour)
	{
		string sign = headroom >= 0 ? "+" : "-";
		TimeSpan span = TimeSpan.FromSeconds(Math.Abs(headroom));

		// Seconds, not minutes: a delta of over a minute is not a delta any more, and the
		// two-character indent is what puts these under the seconds of the line above.
		string magnitude = $"{(int)span.TotalSeconds:D2}.{span.Milliseconds:D3}";

		return Line($"{DeltaIndent}{sign}{magnitude}", Hex(colour));
	}

	private static string Line(string text, string colour)
	{
		return $"<mspace={MonoSpace}><color={colour}>{text}</color></mspace>";
	}

	/// <summary>
	///     Author while the author time is still ahead, gold while only the gold time is, red
	///     once both are gone.
	/// </summary>
	private static Color32 TimeColour(double elapsed, double goldTime, double authorTime)
	{
		if (elapsed < authorTime)
		{
			return HudPalette.Author;
		}

		return elapsed < goldTime ? HudPalette.Gold : HudPalette.Danger;
	}

	private static string Hex(Color32 colour)
	{
		return $"#{colour.r:X2}{colour.g:X2}{colour.b:X2}";
	}

	/// <summary>
	///     The label is sized for one line. Without this the extra two are clipped away and the
	///     whole feature looks like it does nothing.
	/// </summary>
	private void RememberOverflow(TMP_Text label)
	{
		if (_originalOverflow.ContainsKey(label))
		{
			return;
		}

		_originalOverflow[label] = label.overflowMode;
		label.overflowMode = TextOverflowModes.Overflow;
	}

	private void RestoreOverflow()
	{
		foreach (KeyValuePair<TMP_Text, TextOverflowModes> entry in _originalOverflow)
			if (entry.Key != null)
			{
				entry.Key.overflowMode = entry.Value;
			}

		_originalOverflow.Clear();
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