using System;
using AuthorTimeHunting.States.Ath;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     The card that comes up between levels: what the level just played came to, and the run
///     so far under it.
///     It exists because the gap between two levels was the one moment in a run with nothing to
///     read and everything worth reading. A level ends, the screen says "press Y", and whatever
///     the last ten minutes bought was already gone from the panels by the time the next level
///     loaded. This holds it up until the player is driving again, and then gets out of the way
///     on its own - there is nothing to close and nothing to remember to close.
/// </summary>
public class LevelSummaryOverlay : IZeepGUIDrawer
{
	private const string WindowTitle = "Level Summary";

	private const float WidthFraction = 0.3f;
	private const float MinWidth = 320f;
	private const float MaxWidth = 520f;

	private const float TitleSize = 1.4f;
	private const float StatusSize = 1.8f;

	/// <summary>
	///     Where the card sits vertically, as a share of the screen measured from the bottom.
	///     Above the middle rather than on it: the game puts its own round-over text across the
	///     centre, and two things in the same place is one thing nobody reads.
	/// </summary>
	private const float VerticalAnchor = 0.62f;

	// Title bar and measured height, like every other panel here. The borderless, hand-counted
	// variant is what left the top overlay without a single character on screen.
	private const ImWindowFlag WindowFlags = ImWindowFlag.NoCloseButton | ImWindowFlag.NoResizing;

	private float _contentHeight;

	private bool _mouseOverWindow;

	private LevelSummaryView _view;

	public bool Visible => _view != null;

	public void OnZeepGUI(ImGui gui)
	{
		LevelSummaryView view = _view;

		if (view == null)
		{
			return;
		}

		try
		{
			using (UiScale.Push(gui))
			{
				Draw(gui, view);
			}
		}
		catch (Exception e)
		{
			// Inside the game's shared GUI pass - a throwing drawer would throw every frame.
			Logger.LogError($"LevelSummaryOverlay: Draw failed, closing it: {e.Message}\n{e.StackTrace}");
			Hide();
		}
	}

	/// <summary>
	///     Takes the snapshot. Called as the level ends, because a frame later the run has moved
	///     on to the next one and there is nothing left to summarise.
	/// </summary>
	public void Show(AthCtx ctx)
	{
		_view = LevelSummaryView.From(ctx);
	}

	/// <summary>Called when the next level actually starts. Also safe to call when nothing is up.</summary>
	public void Hide()
	{
		_view = null;
	}

	private void Draw(ImGui gui, LevelSummaryView view)
	{
		ImRect screen = gui.Canvas.SafeScreenRect;
		float width = UiMetrics.Width(gui, WidthFraction, MinWidth, MaxWidth);
		float height = Height(gui);

		ImRect rect = new(screen.X + (screen.W - width) * 0.5f,
			Mathf.Clamp(screen.Y + screen.H * VerticalAnchor - height, screen.Y, screen.Top - height),
			width,
			height);

		bool open = true;

		if (!gui.BeginWindow(WindowTitle, ref open, ref _mouseOverWindow, rect, WindowFlags))
		{
			return;
		}

		try
		{
			DrawLevel(gui, view);
			DrawRecent(gui, view);

			// While the window's layout frame is still open, so it can report what it holds.
			_contentHeight = UiMetrics.ContentHeight(gui);
		}
		finally
		{
			gui.EndWindow();
		}
	}

	private static void DrawLevel(ImGui gui, LevelSummaryView view)
	{
		float text = gui.Style.Layout.TextSize;

		UiText.Centre(gui, view.Name, HudPalette.LevelName, Row(gui, TitleSize * 1.2f), text * TitleSize);
		UiText.Centre(gui, $"by {view.Author}", HudPalette.AuthorName, Row(gui, 0.9f), text * 0.85f);
		UiText.Centre(gui, view.Status.ToUpperInvariant(), view.StatusColour, Row(gui, StatusSize * 1.2f),
			text * StatusSize);

		if (view.YourBest == null)
		{
			UiWidgets.Row(gui, Row(gui, 1f), "Your Time", "never finished", HudPalette.Muted);
		}
		else
		{
			UiWidgets.Row(gui, Row(gui, 1f), "Your Time", $"{view.YourBest}   ({view.AuthorDelta})", view.StatusColour);
		}

		UiWidgets.Row(gui, Row(gui, 1f), "Author Time", view.AuthorTime, HudPalette.Author);
		UiWidgets.Row(gui, Row(gui, 1f), "Attempts", view.Attempts, HudPalette.Default);
		UiWidgets.Row(gui, Row(gui, 1f), "Crashes", view.Crashes, HudPalette.Default);
		UiWidgets.Row(gui, Row(gui, 1f), "Time Here", view.TimeHere, HudPalette.Default);
		gui.AddSpacing();
	}

	/// <summary>
	///     The run so far, in the shape the report's level list uses. Deliberately the same
	///     columns: this is a preview of the page the player will read at the end, and learning
	///     it twice is learning it twice.
	/// </summary>
	private static void DrawRecent(ImGui gui, LevelSummaryView view)
	{
		UiWidgets.Heading(gui, Row(gui, 0.85f), "RUN SO FAR");
		UiWidgets.Row(gui, Row(gui, 1f), view.Score, view.TimeLeft, HudPalette.Good);

		foreach (RunReportView.LevelRow level in view.Recent)
		{
			DrawRecentRow(gui, level);
		}
	}

	private static void DrawRecentRow(ImGui gui, RunReportView.LevelRow level)
	{
		ImRect row = Row(gui, 1f);
		float size = gui.Style.Layout.TextSize * 0.9f;

		UiText.Draw(gui, level.Index.ToString(), HudPalette.Muted, Cell(row, 0), size, 0f);
		UiText.Draw(gui, level.Name, HudPalette.LevelName, Cell(row, 1), size, 0f);
		UiText.Draw(gui, level.Status, level.StatusColour, Cell(row, 2), size, 0f);
		UiText.Draw(gui, level.Attempts, HudPalette.Muted, Cell(row, 3), size, 1f);
		UiText.Draw(gui, level.Duration, HudPalette.Muted, Cell(row, 4), size, 1f);
	}

	private static ImRect Cell(ImRect row, int column)
	{
		float[] weights = [0.07f, 0.42f, 0.24f, 0.09f, 0.18f];
		float offset = 0f;

		for (int i = 0; i < column; i++)
		{
			offset += weights[i];
		}

		return new ImRect(row.X + row.W * offset, row.Y, row.W * weights[column], row.H);
	}

	/// <summary>A layout row <paramref name="scale" /> times the theme's row height tall.</summary>
	private static ImRect Row(ImGui gui, float scale)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), gui.GetRowHeight() * scale);
	}

	/// <summary>
	///     Last frame's content plus the window's own chrome. The card is up for a few seconds,
	///     so the one frame of lag on a size change is the whole cost.
	/// </summary>
	private float Height(ImGui gui)
	{
		float content = _contentHeight > 0f ? _contentHeight : gui.GetRowHeight() * 16f;

		return content + UiMetrics.WindowChrome(gui) + UiMetrics.Slack(gui);
	}
}
