using System;
using System.Collections.Generic;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     The end-of-run report: a large results screen with tabs, in the shape a party game
///     puts up when the round is over.
///     Deliberately a step coarser than the rest of the mod's UI. The panels beside it are
///     read at a glance mid-run and are sized to stay out of the way; this is read once, when
///     nothing else is happening, and it is the only place the whole run is visible at all.
///     It holds a snapshot rather than the run, because the run's state machine is destroyed
///     the moment it stops.
/// </summary>
public class ResultsScreen : IZeepGUIDrawer
{
	private const string WindowTitle = "Run Report";

	private const float WidthFraction = 0.62f;
	private const float HeightFraction = 0.7f;
	private const float MinWidth = 560f;
	private const float MinHeight = 420f;

	private const float HeadlineSize = 1.9f;
	private const float MedalRowSize = 2.2f;

	/// <summary>
	///     Rows of the level list per page. The list is paged rather than scrolled: a run can
	///     touch forty levels, and this screen is a fixed size on purpose.
	/// </summary>
	private const int LevelsPerPage = 12;

	private const ImWindowFlag WindowFlags = ImWindowFlag.NoResizing;

	private int _historyPage;
	private bool _mouseOverWindow;
	private int _page;
	private RunReportView _report;

	public bool Visible => _report != null;

	public void OnZeepGUI(ImGui gui)
	{
		RunReportView report = _report;

		if (report == null)
		{
			return;
		}

		try
		{
			Draw(gui, report);
		}
		catch (Exception e)
		{
			// Inside the game's shared GUI pass - a throwing drawer would throw every frame.
			Logger.LogError($"ResultsScreen: Draw failed, closing it: {e.Message}\n{e.StackTrace}");
			Close();
		}
	}

	public void Show(RunReportView report)
	{
		_report = report;
		_page = 0;
		_historyPage = 0;
	}

	public void Close()
	{
		_report = null;
	}

	private void Draw(ImGui gui, RunReportView report)
	{
		ImRect screen = gui.Canvas.SafeScreenRect;
		float width = Mathf.Min(Mathf.Max(screen.W * WidthFraction, MinWidth), screen.W);
		float height = Mathf.Min(Mathf.Max(screen.H * HeightFraction, MinHeight), screen.H);

		// Centred, and placed fresh each time: a report is a moment, not furniture.
		ImRect rect = new(screen.Left + (screen.W - width) * 0.5f,
			screen.Bottom + (screen.H - height) * 0.5f,
			width,
			height);

		bool open = true;

		if (!gui.BeginWindow(WindowTitle, ref open, ref _mouseOverWindow, rect, WindowFlags))
		{
			return;
		}

		try
		{
			DrawHeadline(gui, report);
			DrawTabs(gui, report);
		}
		finally
		{
			gui.EndWindow();
		}

		// The window's own close button is the way out, so the report cannot be left up.
		if (!open)
		{
			Close();
		}
	}

	/// <summary>
	///     Kicker, name, then the number. In that order because the screen appears unannounced -
	///     "RUN OVER" is what a player who looked away for a minute needs first, and the name
	///     makes the report theirs rather than a readout.
	/// </summary>
	private static void DrawHeadline(ImGui gui, RunReportView report)
	{
		float text = gui.Style.Layout.TextSize;

		UiText.Centre(gui, report.Kicker, HudPalette.Muted, Row(gui, 1f), text * 0.9f);
		UiText.Centre(gui, report.PlayerName, HudPalette.White, Row(gui, HeadlineSize * 1.2f), text * HeadlineSize);
		UiText.Centre(gui, report.Headline, HudPalette.Author, Row(gui, 1.4f), text * 1.2f);

		ImRect medals = Row(gui, MedalRowSize);

		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, medals, 0, 3), GameSprites.AuthorMedal, report.AuthorMedals,
			HudPalette.Author);
		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, medals, 1, 3), GameSprites.GoldMedal, report.GoldMedals,
			HudPalette.Gold);
		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, medals, 2, 3), GameSprites.YouTriedMedal, report.Penalties,
			HudPalette.Penalty);

		gui.AddSpacing();
	}

	private void DrawTabs(ImGui gui, RunReportView report)
	{
		// Everything left in the window after the headline. The tabs pane wants a rect
		// rather than a layout row, so it takes the remainder outright.
		ImRect pane = gui.AddLayoutRect(gui.GetLayoutWidth(), gui.GetLayoutHeight());

		gui.BeginTabsPane(pane);

		try
		{
			if (gui.BeginTab("Summary".AsSpan()))
			{
				DrawRows(gui, report.Summary);
				gui.EndTab();
			}

			if (gui.BeginTab("Levels".AsSpan()))
			{
				DrawLevels(gui, report);
				gui.EndTab();
			}

			if (gui.BeginTab("Records".AsSpan()))
			{
				DrawRows(gui, report.Records);
				gui.EndTab();
			}

			if (gui.BeginTab("History".AsSpan()))
			{
				DrawHistory(gui, report);
				gui.EndTab();
			}
		}
		finally
		{
			gui.EndTabsPane();
		}
	}

	private static void DrawRows(ImGui gui, IReadOnlyList<RunReportView.ReportRow> rows)
	{
		foreach (RunReportView.ReportRow row in rows)
		{
			UiWidgets.Row(gui, Row(gui, 1f), row.Label, row.Value, row.ValueColour);
		}
	}

	private void DrawLevels(ImGui gui, RunReportView report)
	{
		IReadOnlyList<RunReportView.LevelRow> levels = report.Levels;

		if (levels.Count == 0)
		{
			UiText.Left(gui, "No levels were played.", HudPalette.Muted, Row(gui, 1f));
			return;
		}

		int pages = Mathf.CeilToInt(levels.Count / (float)LevelsPerPage);
		_page = Mathf.Clamp(_page, 0, pages - 1);

		DrawLevelHeader(gui);

		int first = _page * LevelsPerPage;
		int last = Mathf.Min(first + LevelsPerPage, levels.Count);

		for (int i = first; i < last; i++)
		{
			DrawLevelRow(gui, levels[i]);
		}

		if (pages > 1)
		{
			DrawPager(gui, pages, ref _page);
		}
	}

	/// <summary>
	///     Every run this machine has ever finished, newest first, with the one just played
	///     marked. Paged like the level list and for the same reason: the window is a fixed size
	///     and a history is only ever going to get longer.
	/// </summary>
	private void DrawHistory(ImGui gui, RunReportView report)
	{
		IReadOnlyList<RunReportView.HistoryRow> history = report.History;

		if (history.Count == 0)
		{
			UiText.Left(gui, "No runs recorded yet. This one is the first.", HudPalette.Muted, Row(gui, 1f));
			return;
		}

		int pages = Mathf.CeilToInt(history.Count / (float)LevelsPerPage);
		_historyPage = Mathf.Clamp(_historyPage, 0, pages - 1);

		DrawHistoryHeader(gui);

		int first = _historyPage * LevelsPerPage;
		int last = Mathf.Min(first + LevelsPerPage, history.Count);

		for (int i = first; i < last; i++)
		{
			DrawHistoryRow(gui, history[i]);
		}

		if (pages > 1)
		{
			DrawPager(gui, pages, ref _historyPage);
		}
	}

	private static void DrawHistoryHeader(ImGui gui)
	{
		ImRect row = Row(gui, 0.9f);
		float size = gui.Style.Layout.TextSize * 0.8f;

		UiText.Draw(gui, "WHEN", HudPalette.Muted, HistoryCell(row, 0), size, 0f);
		UiText.Draw(gui, "MODE", HudPalette.Muted, HistoryCell(row, 1), size, 0f);
		UiText.Draw(gui, "AT / GOLD / PEN", HudPalette.Muted, HistoryCell(row, 2), size, 0f);
		UiText.Draw(gui, "LEVELS", HudPalette.Muted, HistoryCell(row, 3), size, 1f);
		UiText.Draw(gui, "DRIVEN", HudPalette.Muted, HistoryCell(row, 4), size, 1f);
	}

	private static void DrawHistoryRow(ImGui gui, RunReportView.HistoryRow record)
	{
		ImRect row = Row(gui, 1f);

		// The run just played, picked out so it can be compared against the rest at a glance.
		Color32 when = record.IsCurrent ? HudPalette.Positive : HudPalette.Muted;

		UiText.Left(gui, record.When, when, HistoryCell(row, 0));
		UiText.Left(gui, record.Gamemode, HudPalette.Default, HistoryCell(row, 1));
		UiText.Left(gui, $"{record.AuthorMedals} / {record.GoldMedals} / {record.Penalties}", HudPalette.Author,
			HistoryCell(row, 2));
		UiText.Right(gui, record.Levels, HudPalette.Default, HistoryCell(row, 3), gui.Style.Layout.TextSize);
		UiText.Right(gui, record.Driven, HudPalette.Default, HistoryCell(row, 4), gui.Style.Layout.TextSize);
	}

	private static ImRect HistoryCell(ImRect row, int column)
	{
		float[] weights = [0.26f, 0.24f, 0.24f, 0.11f, 0.15f];
		float offset = 0f;

		for (int i = 0; i < column; i++)
		{
			offset += weights[i];
		}

		return new ImRect(row.X + row.W * offset, row.Y, row.W * weights[column], row.H);
	}

	private static void DrawLevelHeader(ImGui gui)
	{
		ImRect row = Row(gui, 0.9f);
		float size = gui.Style.Layout.TextSize * 0.8f;

		UiText.Draw(gui, "#", HudPalette.Muted, Cell(row, 0), size, 0f);
		UiText.Draw(gui, "LEVEL", HudPalette.Muted, Cell(row, 1), size, 0f);
		UiText.Draw(gui, "RESULT", HudPalette.Muted, Cell(row, 2), size, 0f);
		UiText.Draw(gui, "TRIES", HudPalette.Muted, Cell(row, 3), size, 1f);
		UiText.Draw(gui, "TIME", HudPalette.Muted, Cell(row, 4), size, 1f);
	}

	private static void DrawLevelRow(ImGui gui, RunReportView.LevelRow level)
	{
		ImRect row = Row(gui, 1f);

		UiText.Left(gui, level.Index.ToString(), HudPalette.Muted, Cell(row, 0));
		UiText.Left(gui, $"{level.Name}  ({level.Author})", HudPalette.LevelName, Cell(row, 1));
		UiText.Left(gui, level.Status, level.StatusColour, Cell(row, 2));
		UiText.Right(gui, level.Attempts, HudPalette.Default, Cell(row, 3), gui.Style.Layout.TextSize);
		UiText.Right(gui, level.Duration, HudPalette.Default, Cell(row, 4), gui.Style.Layout.TextSize);
	}

	/// <summary>
	///     Column layout for the level list, as shares of the row. Fixed widths would either
	///     crush the level name or waste half the screen on a two digit attempt count.
	/// </summary>
	private static ImRect Cell(ImRect row, int column)
	{
		float[] weights = [0.06f, 0.44f, 0.2f, 0.12f, 0.18f];
		float offset = 0f;

		for (int i = 0; i < column; i++)
		{
			offset += weights[i];
		}

		return new ImRect(row.X + row.W * offset, row.Y, row.W * weights[column], row.H);
	}

	private static void DrawPager(ImGui gui, int pages, ref int page)
	{
		gui.AddSpacing();

		ImRect row = gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), UiMetrics.ButtonHeight(gui));
		ImRect previous = UiWidgets.Column(gui, row, 0, 3);
		ImRect label = UiWidgets.Column(gui, row, 1, 3);
		ImRect next = UiWidgets.Column(gui, row, 2, 3);

		if (page > 0 && UiWidgets.Button(gui, previous, "< Previous"))
		{
			page--;
		}

		UiText.Centre(gui, $"Page {page + 1} / {pages}", HudPalette.Muted, label, gui.Style.Layout.TextSize);

		if (page < pages - 1 && UiWidgets.Button(gui, next, "Next >"))
		{
			page++;
		}
	}

	/// <summary>A layout row <paramref name="scale" /> times the theme's text size tall.</summary>
	private static ImRect Row(ImGui gui, float scale)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), gui.GetRowHeight() * scale);
	}
}
