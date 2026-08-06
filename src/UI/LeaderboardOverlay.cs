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

	private const int MaxRows = 8;

	private const ImWindowFlag WindowFlags = ImWindowFlag.NoCloseButton | ImWindowFlag.NoResizing;

	private const float RefreshInterval = 0.1f;

	private static readonly float[] Weights = [0.1f, 0.45f, 0.45f];

	private static readonly Comparison<LeaderboardEntry> ByTime = (left, right) => left.Time.CompareTo(right.Time);

	private readonly List<LeaderboardEntry> _entries = [];

	private Level _builtLevel;

	private float _builtYourTime;

	private float _contentHeight;
	private bool _mouseOverWindow;

	private float _nextRefresh;

	public AthStateMachine ActiveRun { get; set; }

	public bool Visible { get; set; } = true;

	public void OnZeepGUI(ImGui gui)
	{
		AthStateMachine run = ActiveRun;

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

		Color32 rankColour = entry.IsLocal ? Color.Style.Surface.White : Color.Style.Text.Muted;

		UiText.Draw(gui, UiNumbers.Text(rank), rankColour, Cell(row, 0), size * 0.9f, 0f);
		UiText.Draw(gui, entry.Name, entry.Colour, Cell(row, 1), size, 0f);
		UiText.Draw(gui, entry.Text, entry.Colour, Cell(row, 2), size, 1f);
	}

	private static ImRect Cell(ImRect row, int column)
	{
		return UiWidgets.Cell(row, Weights, column);
	}

	private static float YourTime(AthCtx ctx)
	{
		float round = ZeepkistNetwork.LocalPlayer?.CurrentResult?.Time ?? -1f;

		if (round > 0f)
		{
			return round;
		}

		return ctx.CurrentLevel.PersonalBestTime > 0f ? ctx.CurrentLevel.PersonalBestTime : -1f;
	}

	private void Refresh(AthCtx ctx)
	{
		float yourTime = YourTime(ctx);
		Level level = ctx.CurrentLevel;

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

	private void Build(AthCtx ctx, float yourTime)
	{
		Level level = ctx.CurrentLevel;

		_entries.Clear();
		_entries.Add(Medal(AuthorLabel, (float)level.AuthorTime, Color.Zeepkist.Medal.Author, yourTime));
		_entries.Add(Medal(GoldLabel, (float)level.GoldTime, Color.Zeepkist.Medal.Gold, yourTime));

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

				_entries.Add(new LeaderboardEntry(player.Username, time,
					local ? Color.Style.Surface.White : Color.Style.Text.Default, local,
					TimeFormatter.FormatTime(time)));
			}
		}

		_entries.Sort(ByTime);
	}

	private static LeaderboardEntry Medal(string name, float time, Color32 colour, float yourTime)
	{
		string text = yourTime > 0f ? TimeFormatter.FormatDelta(yourTime - time) : TimeFormatter.FormatTime(time);

		return new LeaderboardEntry(name, time, colour, false, text);
	}

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
