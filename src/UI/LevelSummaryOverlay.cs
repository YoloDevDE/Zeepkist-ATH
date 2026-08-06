using System;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.Util;
using Imui.Core;
using UnityEngine;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     The screen that comes up between levels: what the level just played came to, and the run
///     so far beside it.
///     It exists because the gap between two levels was the one moment in a run with nothing to
///     read and everything worth reading. A level ends, the screen says "press Y", and whatever
///     the last ten minutes bought was already gone from the panels by the time the next level
///     loaded. This holds it up until the player is driving again, and then gets out of the way
///     on its own - there is nothing to close and nothing to remember to close.
///     It is painted straight onto the canvas rather than into a window, and that is the point.
///     An Imui window the size of the screen would take every click on it for the whole time the
///     level is loading, and the game is still under there with a podium the player has to get
///     past. Paint takes nothing.
///     What paint does not get for free is being on top: Imui draws each window above the plain
///     canvas, so the mod's own run panels would float over a fullscreen card drawn at the
///     default order. Hence <see cref="UiScreen.Order" />.
/// </summary>
public class LevelSummaryOverlay : IZeepGUIDrawer
{
	private const float TitleSize = 2.1f;
	private const float StatusSize = 2.9f;
	private const float LineHeight = 1.8f;

	private const float ColumnGap = 0.06f;
	private const float RecentRow = 1.1f;

	private static readonly float[] Weights = [0.08f, 0.44f, 0.24f, 0.1f, 0.14f];

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
			Draw(gui, view);
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

	private static void Draw(ImGui gui, LevelSummaryView view)
	{
		gui.Canvas.PushOrder(UiScreen.Order);

		try
		{
			Paint(gui, view);
		}
		finally
		{
			gui.Canvas.PopOrder();
		}
	}

	private static void Paint(ImGui gui, LevelSummaryView view)
	{
		ImRect screen = UiScreen.Full(gui);
		float text = gui.Style.Layout.TextSize;

		gui.Canvas.Rect(screen, Color.Style.Surface.Shade);

		UiColumn body = new(UiScreen.Column(screen), text * LineHeight);

		DrawHeadline(gui, body, view, text);
		DrawColumns(gui, body, view, text);
	}

	private static void DrawHeadline(ImGui gui, UiColumn body, LevelSummaryView view, float text)
	{
		body.Space(0.6f);

		Centre(gui, view.Name, Color.Style.Text.LevelName, body.Row(TitleSize * 1.3f), text * TitleSize);
		Centre(gui, view.ByAuthor, Color.Style.Text.AuthorName, body.Row(1.1f), text * 0.95f);

		body.Space(0.4f);

		Centre(gui, view.StatusUpper, view.StatusColour, body.Row(StatusSize * 1.2f), text * StatusSize);

		UiScreen.Rule(gui, body.Row(1f), view.StatusColour);

		body.Space(0.8f);
	}

	private static void DrawColumns(ImGui gui, UiColumn body, LevelSummaryView view, float text)
	{
		ImRect rest = body.Rest();
		float gap = rest.W * ColumnGap;
		float width = (rest.W - gap) * 0.5f;

		DrawLevel(gui, new UiColumn(new ImRect(rest.X, rest.Y, width, rest.H), text * LineHeight), view, text);

		DrawRun(gui, new UiColumn(new ImRect(rest.Right - width, rest.Y, width, rest.H), text * LineHeight), view,
			text);
	}

	private static void DrawLevel(ImGui gui, UiColumn column, LevelSummaryView view, float text)
	{
		Heading(gui, column.Row(1f), "THIS LEVEL", text);

		Stat(gui, column.Row(1.2f), "Your Time", view.BestWithDelta ?? "never finished",
			view.BestWithDelta == null ? Color.Style.Text.Muted : view.StatusColour, text);

		Stat(gui, column.Row(1.2f), "Author Time", view.AuthorTime, Color.Zeepkist.Medal.Author, text);
		Stat(gui, column.Row(1.2f), "Attempts", view.Attempts, Color.Style.Text.Default, text);
		Stat(gui, column.Row(1.2f), "Crashes", view.Crashes, Color.Style.Text.Default, text);
		Stat(gui, column.Row(1.2f), "Time Here", view.TimeHere, Color.Style.Text.Default, text);
	}

	private static void DrawRun(ImGui gui, UiColumn column, LevelSummaryView view, float text)
	{
		Heading(gui, column.Row(1f), "RUN SO FAR", text);
		DrawScore(gui, column.Row(1.5f), view, text);

		column.Space(0.4f);
		DrawRecentHeader(gui, column.Row(1f), text);
		DrawRecent(gui, column, view, text);
	}

	private static void DrawScore(ImGui gui, ImRect row, LevelSummaryView view, float text)
	{
		gui.Canvas.Text(view.Score.AsSpan(), Color.Style.Surface.White, row, text * 1.05f, 0f);
		gui.Canvas.Text(view.TimeLeft.AsSpan(), Color.Style.Status.Good, row, text * 1.3f, 1f);
	}

	/// <summary>
	///     As much of the run as the column still has room for. A row drawn past the bottom of the
	///     screen is not clipped by anything - there is no window here to clip it.
	/// </summary>
	private static void DrawRecent(ImGui gui, UiColumn column, LevelSummaryView view, float text)
	{
		float line = text * LineHeight * RecentRow;

		foreach (LevelRow level in view.Recent)
		{
			if (column.Rest().H < line)
			{
				continue;
			}

			DrawRecentRow(gui, column.Row(RecentRow), level, text);
		}
	}

	private static void DrawRecentHeader(ImGui gui, ImRect row, float text)
	{
		float size = text * 0.8f;

		Cell(gui, row, 0, "#", Color.Style.Text.Muted, size, 0f);
		Cell(gui, row, 1, "LEVEL", Color.Style.Text.Muted, size, 0f);
		Cell(gui, row, 2, "RESULT", Color.Style.Text.Muted, size, 0f);
		Cell(gui, row, 3, "TRIES", Color.Style.Text.Muted, size, 1f);
		Cell(gui, row, 4, "TIME", Color.Style.Text.Muted, size, 1f);
	}

	private static void DrawRecentRow(ImGui gui, ImRect row, LevelRow level, float text)
	{
		float size = text * 0.9f;

		Cell(gui, row, 0, UiNumbers.Text(level.Index), Color.Style.Text.Muted, size, 0f);
		Cell(gui, row, 1, level.Name, Color.Style.Text.LevelName, size, 0f);
		Cell(gui, row, 2, level.Status, level.StatusColour, size, 0f);
		Cell(gui, row, 3, level.Attempts, Color.Style.Text.Muted, size, 1f);
		Cell(gui, row, 4, level.Duration, Color.Style.Text.Muted, size, 1f);
	}

	private static void Cell(ImGui gui, ImRect row, int column, string value, Color32 colour, float size, float alignX)
	{
		ImRect rect = UiWidgets.Cell(row, Weights, column);

		gui.Canvas.PushClipRect(rect);

		try
		{
			gui.Canvas.Text(value.AsSpan(), colour, rect, size, alignX);
		}
		finally
		{
			gui.Canvas.PopClipRect();
		}
	}

	private static void Heading(ImGui gui, ImRect row, string text, float size)
	{
		gui.Canvas.Text(text.AsSpan(), Color.Style.Text.Section, row, size * 0.85f, 0f);
	}

	private static void Stat(ImGui gui, ImRect row, string label, string value, Color32 colour, float size)
	{
		gui.Canvas.Text(label.AsSpan(), Color.Style.Text.Muted, row, size, 0f);
		gui.Canvas.Text(value.AsSpan(), colour, row, size, 1f);
	}

	private static void Centre(ImGui gui, string value, Color32 colour, ImRect row, float size)
	{
		if (string.IsNullOrEmpty(value))
		{
			return;
		}

		gui.Canvas.Text(value.AsSpan(), colour, row, size);
	}
}
