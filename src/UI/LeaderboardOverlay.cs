using System;
using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepkistClient;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     ATH's own leaderboard, with the author and gold times standing in it as if they were two
///     more players in the lobby.
///     <code>
///     1  AT         +00:03.231
///     2  Hi Im Yolo  00:56.345
///     3  GOLD       -00:00.344
///     </code>
///     A hunt is a race against two times, and a leaderboard is the widget for "who is ahead of
///     whom" - it simply does not know that the medals are competitors. Sorting them in turns
///     the question into the one the board was built to answer, and the medal rows carry the gap
///     rather than the time, because the gap is what is actually being read.
///     Its own window rather than a rewrite of the game's own rows. The first version took over
///     the small leaderboard's labels in a LateUpdate, which meant living inside somebody else's
///     layout and handing every row back on the way out. Drawn here, it can be moved, and it can
///     later be put exactly over the game's board instead of inside it.
/// </summary>
public class LeaderboardOverlay : IZeepGUIDrawer
{
	private const string WindowTitle = "Leaderboard";

	private const string AuthorLabel = "AT";
	private const string GoldLabel = "GOLD";

	private const float WidthFraction = 0.17f;
	private const float MinWidth = 220f;
	private const float MaxWidth = 320f;

	/// <summary>
	///     Most rows the board will draw. A lobby can hold a lot of people and this is a HUD
	///     element, not the tab screen - what matters is the medals and whoever is next to you.
	/// </summary>
	private const int MaxRows = 8;

	private const ImWindowFlag WindowFlags = ImWindowFlag.NoCloseButton | ImWindowFlag.NoResizing;

	/// <summary>
	///     How often the board is rebuilt, in seconds. Lap times land at human speed, so a tenth
	///     of a second is already faster than anything on here can change.
	/// </summary>
	private const float RefreshInterval = 0.1f;

	/// <summary>Rank, name, time - as shares of the row.</summary>
	private static readonly float[] Weights = [0.1f, 0.45f, 0.45f];

	/// <summary>Fastest first. A field rather than a lambda at the call site, so it is made once.</summary>
	private static readonly Comparison<LeaderboardEntry> ByTime = (left, right) => left.Time.CompareTo(right.Time);

	private readonly List<LeaderboardEntry> _entries = [];

	/// <summary>The level the board was last built for, to notice one being swapped underneath it.</summary>
	private Level _builtLevel;

	/// <summary>The local player's time the board was last built with. See <see cref="Refresh" />.</summary>
	private float _builtYourTime;

	private float _contentHeight;
	private bool _mouseOverWindow;

	/// <summary>Unscaled time at which the board may be rebuilt again.</summary>
	private float _nextRefresh;

	/// <summary>The run currently in progress, or null when ATH is idle. Set by StateMasterOn.</summary>
	public AthStateMachine ActiveRun { get; set; }

	/// <summary>Toggled from the toolbar. Off with the config switch as well.</summary>
	public bool Visible { get; set; } = true;

	public void OnZeepGUI(ImGui gui)
	{
		AthStateMachine run = ActiveRun;

		// A level in the context is the moment the first run begins - before that there is
		// nothing to compare against and the lobby is still sitting on somebody else's map.
		if (!Visible || run?.Ctx.CurrentLevel == null || !Plugin.Instance.MyConfig.LeaderboardMedals.Value)
		{
			return;
		}

		try
		{
			using (UiScale.Push(gui))
			{
				Draw(gui, run.Ctx);
			}
		}
		catch (Exception e)
		{
			// Inside the game's shared GUI pass - a throwing drawer would throw every frame.
			Logger.LogError($"LeaderboardOverlay: Draw failed, hiding it: {e.Message}\n{e.StackTrace}");
			Visible = false;
		}
	}

	private void Draw(ImGui gui, AthCtx ctx)
	{
		Refresh(ctx);

		float width = UiMetrics.Width(gui, WidthFraction, MinWidth, MaxWidth);

		ImRect rect = ImWindowPlacement.PlaceAutoSized(gui, WindowTitle.AsSpan(), width, Height(gui),
			ImWindowAnchor.MiddleRight);

		bool open = true;

		if (!gui.BeginWindow(WindowTitle, ref open, ref _mouseOverWindow, rect, WindowFlags))
		{
			return;
		}

		try
		{
			int first = FirstVisible();
			int last = Mathf.Min(first + MaxRows, _entries.Count);

			for (int i = first; i < last; i++)
			{
				DrawRow(gui, _entries[i], i + 1);
			}

			// While the window's layout frame is still open, so it can report what it holds.
			_contentHeight = UiMetrics.ContentHeight(gui);
		}
		finally
		{
			gui.EndWindow();
		}
	}

	private static void DrawRow(ImGui gui, LeaderboardEntry entry, int rank)
	{
		ImRect row = gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), gui.GetRowHeight());
		float size = gui.Style.Layout.TextSize;

		// The player's own line is the one being looked for, so it is the one that is not grey.
		Color32 rankColour = entry.IsLocal ? HudPalette.White : HudPalette.Muted;

		UiText.Draw(gui, UiNumbers.Text(rank), rankColour, Cell(row, 0), size * 0.9f, 0f);
		UiText.Draw(gui, entry.Name, entry.Colour, Cell(row, 1), size, 0f);
		UiText.Draw(gui, entry.Text, entry.Colour, Cell(row, 2), size, 1f);
	}

	private static ImRect Cell(ImRect row, int column)
	{
		return UiWidgets.Cell(row, Weights, column);
	}

	/// <summary>
	///     What the local player has on this level right now: this round's result if they have
	///     finished, otherwise their best from an earlier attempt, otherwise nothing. The round
	///     result wins because the board is about the round, and a personal best from three
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
	///     Rebuilds the board, but not on every frame.
	///     Nothing on it moves at frame rate: a lap time appears when somebody crosses the line
	///     and then stands still. Rebuilding regardless meant formatting a fresh string for every
	///     row of a full lobby, a hundred and forty times a second, for the length of a hunt -
	///     garbage the collector then had to walk, in the middle of a race.
	///     The two things that do want to be immediate are handled as they happen: your own time
	///     landing, and the level changing under the board.
	/// </summary>
	private void Refresh(AthCtx ctx)
	{
		float yourTime = YourTime(ctx);
		Level level = ctx.CurrentLevel;

		// _builtLevel is null until the first build, so the first frame always falls through.
		if (ReferenceEquals(level, _builtLevel)
		    && Mathf.Approximately(yourTime, _builtYourTime)
		    && Time.unscaledTime < _nextRefresh)
		{
			return;
		}

		_builtLevel = level;
		_builtYourTime = yourTime;
		_nextRefresh = Time.unscaledTime + RefreshInterval;

		Build(ctx, yourTime);
	}

	/// <summary>
	///     Everyone with a time, the two medals among them, fastest first. Rebuilt into the same
	///     list rather than allocated fresh - this runs in a GUI pass.
	/// </summary>
	private void Build(AthCtx ctx, float yourTime)
	{
		Level level = ctx.CurrentLevel;

		_entries.Clear();
		_entries.Add(Medal(AuthorLabel, (float)level.AuthorTime, HudPalette.Author, yourTime));
		_entries.Add(Medal(GoldLabel, (float)level.GoldTime, HudPalette.Gold, yourTime));

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

				_entries.Add(new LeaderboardEntry(player.Username, time, local ? HudPalette.White : HudPalette.Default, local,
					TimeFormatter.FormatTime(time)));
			}
		}

		_entries.Sort(ByTime);
	}

	/// <summary>
	///     A medal standing in the board. It carries the gap to the local player rather than its
	///     own time: the absolute time is already on the level panel and does not change, and the
	///     gap is signed the way a split is - ahead is negative, so a leading minus means the
	///     medal is still in hand. Without a time of your own there is no gap, so it shows the
	///     time it wants instead.
	/// </summary>
	private static LeaderboardEntry Medal(string name, float time, Color32 colour, float yourTime)
	{
		string text = yourTime > 0f ? TimeFormatter.FormatDelta(yourTime - time) : TimeFormatter.FormatTime(time);

		return new LeaderboardEntry(name, time, colour, false, text);
	}

	/// <summary>
	///     Which entry the visible window starts at. Scrolled so the local player stays on the
	///     board: in a full lobby the two medals push two people off the bottom, and the person
	///     they must never push off is the one reading it.
	/// </summary>
	private int FirstVisible()
	{
		if (_entries.Count <= MaxRows)
		{
			return 0;
		}

		int local = _entries.FindIndex(entry => entry.IsLocal);

		if (local < 0)
		{
			return 0;
		}

		return Mathf.Clamp(local - MaxRows / 2, 0, _entries.Count - MaxRows);
	}

	private float Height(ImGui gui)
	{
		float content = _contentHeight > 0f ? _contentHeight : gui.GetRowHeight() * 4f;

		return content + UiMetrics.WindowChrome(gui) + UiMetrics.Slack(gui);
	}
}
