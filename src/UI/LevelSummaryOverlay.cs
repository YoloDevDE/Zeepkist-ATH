using System;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.Util;
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

	private const float VerticalAnchor = 0.62f;

	private const ImWindowFlag WindowFlags = ImWindowFlag.NoCloseButton | ImWindowFlag.NoResizing;

	private static readonly float[] Weights = [0.07f, 0.42f, 0.24f, 0.09f, 0.18f];

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
			Logger.LogError($"LevelSummaryOverlay: Draw failed, closing it: {e.Message}\n{e.StackTrace}");
			Hide();
		}
	}

	public void Show(AthCtx ctx)
	{
		_view = LevelSummaryView.From(ctx);
	}

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

		UiText.Centre(gui, view.Name, Color.Style.Text.LevelName, Row(gui, TitleSize * 1.2f), text * TitleSize);
		UiText.Centre(gui, view.ByAuthor, Color.Style.Text.AuthorName, Row(gui, 0.9f), text * 0.85f);
		UiText.Centre(gui, view.StatusUpper, view.StatusColour, Row(gui, StatusSize * 1.2f), text * StatusSize);

		UiWidgets.Row(gui, Row(gui, 1f), "Your Time", view.BestWithDelta ?? "never finished",
			view.BestWithDelta == null ? Color.Style.Text.Muted : view.StatusColour);

		UiWidgets.Row(gui, Row(gui, 1f), "Author Time", view.AuthorTime, Color.Zeepkist.Medal.Author);
		UiWidgets.Row(gui, Row(gui, 1f), "Attempts", view.Attempts, Color.Style.Text.Default);
		UiWidgets.Row(gui, Row(gui, 1f), "Crashes", view.Crashes, Color.Style.Text.Default);
		UiWidgets.Row(gui, Row(gui, 1f), "Time Here", view.TimeHere, Color.Style.Text.Default);
		gui.AddSpacing();
	}

	private static void DrawRecent(ImGui gui, LevelSummaryView view)
	{
		UiWidgets.Heading(gui, Row(gui, 0.85f), "RUN SO FAR");
		UiWidgets.Row(gui, Row(gui, 1f), view.Score, view.TimeLeft, Color.Style.Status.Good);

		foreach (LevelRow level in view.Recent)
		{
			DrawRecentRow(gui, level);
		}
	}

	private static void DrawRecentRow(ImGui gui, LevelRow level)
	{
		ImRect row = Row(gui, 1f);
		float size = gui.Style.Layout.TextSize * 0.9f;

		UiText.Draw(gui, UiNumbers.Text(level.Index), Color.Style.Text.Muted, Cell(row, 0), size, 0f);
		UiText.Draw(gui, level.Name, Color.Style.Text.LevelName, Cell(row, 1), size, 0f);
		UiText.Draw(gui, level.Status, level.StatusColour, Cell(row, 2), size, 0f);
		UiText.Draw(gui, level.Attempts, Color.Style.Text.Muted, Cell(row, 3), size, 1f);
		UiText.Draw(gui, level.Duration, Color.Style.Text.Muted, Cell(row, 4), size, 1f);
	}

	private static ImRect Cell(ImRect row, int column)
	{
		return UiWidgets.Cell(row, Weights, column);
	}

	private static ImRect Row(ImGui gui, float scale)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), gui.GetRowHeight() * scale);
	}

	private float Height(ImGui gui)
	{
		float content = _contentHeight > 0f ? _contentHeight : gui.GetRowHeight() * 16f;

		return content + UiMetrics.WindowChrome(gui) + UiMetrics.Slack(gui);
	}
}
