using System;
using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using TMPro;
using UnityEngine;
using ZeepkistClient;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.UI;

/// <summary>
///     Puts the author and gold times into the small in-race leaderboard as if they were two
///     more players in the lobby.
///     <code>
///     1  AT      +00:03.231
///     2  Yolo     00:56.345
///     3  GOLD    -00:00.344
///     </code>
///     A hunt is a race against two times, and the game already has a widget for "who is ahead
///     of whom" - it simply does not know that the medals are competitors. Sorting them in
///     turns the question into the one the leaderboard was built to answer, and the medal rows
///     carry the gap rather than the time, because the gap is what is actually being read.
///     Written from a LateUpdate rather than a Harmony patch, for the same reason as
///     <see cref="RaceTimeDisplay" />: Unity runs every LateUpdate after every Update, so this
///     wins the frame against the game's own redraw without patching it, and stopping is enough
///     to hand the leaderboard back.
/// </summary>
public class LeaderboardOverlay : IDisposable
{
	private const string AuthorLabel = "AT";
	private const string GoldLabel = "GOLD";

	/// <summary>Shown in the time column for an entry that has no time yet.</summary>
	private const string NoTime = "--:--.---";

	private readonly LeaderboardBehaviour _behaviour;

	/// <summary>Rows we have written into, and what they said before we did.</summary>
	private readonly Dictionary<GUI_OnlineLeaderboardPosition, RowState> _borrowed = new();

	private readonly List<Entry> _entries = [];

	private OnlineGameplayUI _ui;

	public LeaderboardOverlay()
	{
		GameObject host = new(nameof(LeaderboardOverlay)) { hideFlags = HideFlags.HideAndDontSave };

		Object.DontDestroyOnLoad(host);
		_behaviour = host.AddComponent<LeaderboardBehaviour>();
		_behaviour.Bind(this);
	}

	/// <summary>The run currently in progress, or null when ATH is idle. Set by StateMasterOn.</summary>
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
	///     A level in the context is the moment the first run begins - before that there is
	///     nothing to compare against and the lobby is still sitting on somebody else's map.
	/// </summary>
	private bool IsActive =>
		ActiveRun?.Ctx.CurrentLevel != null && Plugin.Instance.MyConfig.LeaderboardMedals.Value;

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
			// No-op once the rows have been handed back, so this costs nothing per frame.
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
			Logger.LogError($"LeaderboardOverlay: Failed, switching it off: {e.Message}\n{e.StackTrace}");
			ActiveRun = null;
		}
	}

	private void Draw()
	{
		List<GUI_OnlineLeaderboardPosition> rows = Rows();

		if (rows == null || rows.Count == 0)
		{
			return;
		}

		AthCtx ctx = ActiveRun.Ctx;
		float yourTime = YourTime(ctx);

		Build(ctx, yourTime);

		if (_entries.Count == 0)
		{
			return;
		}

		int first = FirstVisible(rows.Count);

		for (int i = 0; i < rows.Count; i++)
		{
			GUI_OnlineLeaderboardPosition row = rows[i];

			if (row == null)
			{
				continue;
			}

			int index = first + i;

			if (index >= _entries.Count)
			{
				// Past the end of our list. Blanked rather than left alone, because what is
				// left there is the game's own line for a player we have pushed off the board.
				Write(row, false, string.Empty, string.Empty, string.Empty);
				continue;
			}

			Entry entry = _entries[index];

			// A leading space in the time column: the name column runs right up against it, and
			// a name that ends in a digit and a time that starts with one read as one number.
			Write(row, true, (index + 1).ToString(), Tint(entry.Name, entry.Colour),
				Tint(" " + TimeText(entry, yourTime), entry.Colour));
		}
	}

	/// <summary>
	///     The game's row objects, found once and rechecked whenever the scene has taken them
	///     away. FindObjectOfType is not something to do every frame, and the online gameplay UI
	///     is destroyed and rebuilt on every level load.
	/// </summary>
	private List<GUI_OnlineLeaderboardPosition> Rows()
	{
		if (_ui == null)
		{
			_ui = Object.FindObjectOfType<OnlineGameplayUI>();
			_borrowed.Clear();
		}

		return _ui == null ? null : _ui.leaderboard_ingame_positions;
	}

	/// <summary>
	///     What the local player has on this level right now: this round's result if they have
	///     finished, otherwise their best from an earlier attempt, otherwise nothing. The round
	///     result wins because the leaderboard is about the round, and a personal best from three
	///     attempts ago sitting above the author time would be a lie about what just happened.
	/// </summary>
	private static float YourTime(AthCtx ctx)
	{
		float round = ZeepkistNetwork.LocalPlayer?.CurrentResult?.Time ?? -1f;

		if (round > 0f)
		{
			return round;
		}

		return ctx.CurrentLevel.PersonalBestTime > 0f ? ctx.CurrentLevel.PersonalBestTime : -1f;
	}

	/// <summary>
	///     Everyone with a time, the two medals among them, fastest first. Rebuilt into the same
	///     list every frame rather than allocated fresh - this runs in LateUpdate.
	/// </summary>
	private void Build(AthCtx ctx, float yourTime)
	{
		Level level = ctx.CurrentLevel;

		_entries.Clear();
		_entries.Add(new Entry(AuthorLabel, (float)level.AuthorTime, HudPalette.Author, true, false));
		_entries.Add(new Entry(GoldLabel, (float)level.GoldTime, HudPalette.Gold, true, false));

		List<ZeepkistNetworkPlayer> players = ZeepkistNetwork.PlayerList;

		if (players != null)
		{
			foreach (ZeepkistNetworkPlayer player in players)
			{
				if (player == null)
				{
					continue;
				}

				bool local = player == ZeepkistNetwork.LocalPlayer;
				float time = local ? yourTime : player.CurrentResult?.Time ?? -1f;

				if (time <= 0f)
				{
					continue;
				}

				_entries.Add(new Entry(player.GetTaggedUsername(), time, local ? HudPalette.White : HudPalette.Default,
					false, local));
			}
		}

		_entries.Sort((left, right) => left.Time.CompareTo(right.Time));
	}

	/// <summary>
	///     Which entry the visible window starts at. Scrolled so the local player stays on the
	///     board: on a full lobby the two medals push two people off the bottom, and the person
	///     they must never push off is the one reading it.
	/// </summary>
	private int FirstVisible(int rowCount)
	{
		if (_entries.Count <= rowCount)
		{
			return 0;
		}

		int local = _entries.FindIndex(entry => entry.IsLocal);

		if (local < 0)
		{
			return 0;
		}

		// Centred on the local player, then pulled back inside both ends of the list.
		int first = local - rowCount / 2;

		return Mathf.Clamp(first, 0, _entries.Count - rowCount);
	}

	/// <summary>
	///     The medals carry their gap to the local player, everyone else carries their time. A
	///     gap is the only reading that matters on a medal - its absolute time is already on the
	///     level panel and does not change - and it is signed the way a split is: ahead is
	///     negative, so a leading minus means the medal is still in hand.
	/// </summary>
	private static string TimeText(Entry entry, float yourTime)
	{
		if (!entry.IsMedal)
		{
			return TimeFormatter.FormatTime(entry.Time);
		}

		return yourTime > 0f ? TimeFormatter.FormatDelta(yourTime - entry.Time) : TimeFormatter.FormatTime(entry.Time);
	}

	private void Write(GUI_OnlineLeaderboardPosition row, bool active, string position, string name, string time)
	{
		Borrow(row);

		// The game only activates as many rows as it has players for, which on a solo hunt is
		// one. The medals need theirs turned on, and turned back off when we let go.
		if (row.gameObject.activeSelf != active)
		{
			row.gameObject.SetActive(active);
		}

		Set(row.position, position);
		Set(row.player_name, name);
		Set(row.time, time);
	}

	private static void Set(TMP_Text label, string text)
	{
		if (label != null && label.text != text)
		{
			label.text = text;
		}
	}

	private static string Tint(string text, Color32 colour)
	{
		return string.IsNullOrEmpty(text) ?
			text :
			$"<color=#{colour.r:X2}{colour.g:X2}{colour.b:X2}>{text}</color>";
	}

	/// <summary>Takes a row over, once, and keeps what it looked like first.</summary>
	private void Borrow(GUI_OnlineLeaderboardPosition row)
	{
		if (_borrowed.ContainsKey(row))
		{
			return;
		}

		_borrowed[row] = new RowState(row.gameObject.activeSelf, row.position?.text, row.player_name?.text,
			row.time?.text);
	}

	/// <summary>Hands every row back exactly as it was found.</summary>
	private void Restore()
	{
		foreach (KeyValuePair<GUI_OnlineLeaderboardPosition, RowState> entry in _borrowed)
		{
			GUI_OnlineLeaderboardPosition row = entry.Key;

			if (row == null)
			{
				continue;
			}

			row.gameObject.SetActive(entry.Value.Active);
			Set(row.position, entry.Value.Position);
			Set(row.player_name, entry.Value.Name);
			Set(row.time, entry.Value.Time);
		}

		_borrowed.Clear();
	}

	/// <summary>One line of the merged board. A medal is an entry like any other - that is the point.</summary>
	private readonly struct Entry
	{
		public Entry(string name, float time, Color32 colour, bool isMedal, bool isLocal)
		{
			Name = name;
			Time = time;
			Colour = colour;
			IsMedal = isMedal;
			IsLocal = isLocal;
		}

		public string Name { get; }
		public float Time { get; }
		public Color32 Colour { get; }
		public bool IsMedal { get; }
		public bool IsLocal { get; }
	}

	/// <summary>What a row looked like before ATH took it over.</summary>
	private readonly struct RowState
	{
		public RowState(bool active, string position, string name, string time)
		{
			Active = active;
			Position = position;
			Name = name;
			Time = time;
		}

		public bool Active { get; }
		public string Position { get; }
		public string Name { get; }
		public string Time { get; }
	}

	/// <summary>
	///     The frame hook. Separate because LeaderboardOverlay is a plain service and only a
	///     MonoBehaviour gets a LateUpdate - the same split RaceTimeDisplay uses.
	/// </summary>
	private sealed class LeaderboardBehaviour : MonoBehaviour
	{
		private LeaderboardOverlay _owner;

		private void LateUpdate()
		{
			_owner?.LateUpdate();
		}

		public void Bind(LeaderboardOverlay owner)
		{
			_owner = owner;
		}
	}
}
