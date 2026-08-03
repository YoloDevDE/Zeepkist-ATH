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

	private readonly List<Entry> _entries = [];

	private float _contentHeight;
	private bool _mouseOverWindow;

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
		float yourTime = YourTime(ctx);

		Build(ctx, yourTime);

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
				DrawRow(gui, _entries[i], i + 1, yourTime);
			}

			// While the window's layout frame is still open, so it can report what it holds.
			_contentHeight = UiMetrics.ContentHeight(gui);
		}
		finally
		{
			gui.EndWindow();
		}
	}

	private static void DrawRow(ImGui gui, Entry entry, int rank, float yourTime)
	{
		ImRect row = gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), gui.GetRowHeight());
		float size = gui.Style.Layout.TextSize;

		// The player's own line is the one being looked for, so it is the one that is not grey.
		Color32 rankColour = entry.IsLocal ? HudPalette.White : HudPalette.Muted;

		UiText.Draw(gui, rank.ToString(), rankColour, Cell(row, 0), size * 0.9f, 0f);
		UiText.Draw(gui, entry.Name, entry.Colour, Cell(row, 1), size, 0f);
		UiText.Draw(gui, TimeText(entry, yourTime), entry.Colour, Cell(row, 2), size, 1f);
	}

	private static ImRect Cell(ImRect row, int column)
	{
		float[] weights = [0.1f, 0.45f, 0.45f];
		float offset = 0f;

		for (int i = 0; i < column; i++)
		{
			offset += weights[i];
		}

		return new ImRect(row.X + row.W * offset, row.Y, row.W * weights[column], row.H);
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
	///     Everyone with a time, the two medals among them, fastest first. Rebuilt into the same
	///     list every frame rather than allocated fresh - this runs in a GUI pass.
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

				_entries.Add(new Entry(player.Username, time, local ? HudPalette.White : HudPalette.Default, false,
					local));
			}
		}

		_entries.Sort((left, right) => left.Time.CompareTo(right.Time));
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

	private float Height(ImGui gui)
	{
		float content = _contentHeight > 0f ? _contentHeight : gui.GetRowHeight() * 4f;

		return content + UiMetrics.WindowChrome(gui) + UiMetrics.Slack(gui);
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
}
