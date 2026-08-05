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
	///     How wide a level thumbnail is allowed to get. It is a 16:9 picture on a screen that
	///     is mostly a list, and letting it fill the width makes the numbers under it a footnote.
	/// </summary>
	private const float ThumbnailMaxWidth = 360f;

	private const ImWindowFlag WindowFlags = ImWindowFlag.NoResizing;

	/// <summary>
	///     Column layout for the level list, as shares of the row. Fixed widths would either
	///     crush the level name or waste half the screen on a two digit attempt count.
	/// </summary>
	private static readonly float[] LevelWeights = [0.06f, 0.44f, 0.2f, 0.12f, 0.18f];

	/// <summary>The same, for the run history: when, how long, the medals, the score.</summary>
	private static readonly float[] HistoryWeights = [0.26f, 0.24f, 0.24f, 0.11f, 0.15f];

	/// <summary>The level whose detail view is open, or null while the list is showing.</summary>
	private LevelRow _level;

	private bool _mouseOverWindow;
	private RunReportView _report;

	/// <summary>
	///     A past run opened out of the history, as its own report, or null while the history
	///     list is showing. Held as a built view rather than as the record, so it is converted
	///     once on the click rather than on every frame of the frame it is open.
	/// </summary>
	private RunReportView _run;

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
		_level = null;
		_run = null;
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

	private static void DrawRows(ImGui gui, IReadOnlyList<ReportRow> rows)
	{
		foreach (ReportRow row in rows)
		{
			UiWidgets.Row(gui, Row(gui, 1f), row.Label, row.Value, row.ValueColour);
		}
	}

	/// <summary>
	///     The level list, or the one level that was clicked. Scrolled rather than paged: the
	///     list was already inside a scrollable window, so the pager was a second way of moving
	///     through the same list that only worked twelve rows at a time.
	/// </summary>
	private void DrawLevels(ImGui gui, RunReportView report)
	{
		if (_level != null)
		{
			DrawLevelDetail(gui, _level);
			return;
		}

		IReadOnlyList<LevelRow> levels = report.Levels;

		if (levels.Count == 0)
		{
			UiText.Left(gui, "No levels were played.", HudPalette.Muted, Row(gui, 1f));
			return;
		}

		DrawLevelHeader(gui);

		gui.BeginScrollable();

		try
		{
			foreach (LevelRow level in levels)
			{
				if (DrawLevelRow(gui, level))
				{
					_level = level;
				}
			}
		}
		finally
		{
			gui.EndScrollable();
		}
	}

	/// <summary>
	///     One level, in full: what it looked like, what it wanted, and what it cost. The report
	///     lists forty of these as one line each, which answers how the run went and nothing
	///     about how any single level went.
	/// </summary>
	private void DrawLevelDetail(ImGui gui, LevelRow level)
	{
		float text = gui.Style.Layout.TextSize;

		if (UiWidgets.IconButton(gui, ButtonRow(gui), UiIcon.Restart, "Back to the list", HudPalette.ActionRestart))
		{
			_level = null;
		}

		gui.BeginScrollable();

		try
		{
			DrawThumbnail(gui, level);

			UiText.Centre(gui, level.Name, HudPalette.LevelName, Row(gui, 1.6f), text * 1.4f);
			UiText.Centre(gui, level.ByAuthor, HudPalette.AuthorName, Row(gui, 1f), text * 0.9f);
			UiText.Centre(gui, level.StatusUpper, level.StatusColour, Row(gui, 1.8f), text * 1.5f);
			gui.AddSpacing();

			DrawMedalTime(gui, GameSprites.AuthorMedal, "Author Time", level.AuthorTime, HudPalette.Author);
			DrawMedalTime(gui, GameSprites.GoldMedal, "Gold Time", level.GoldTime, HudPalette.Gold);

			UiWidgets.Row(gui, Row(gui, 1f), "Your Best", level.BestWithDelta ?? "never finished",
				level.BestWithDelta == null ? HudPalette.Muted : level.StatusColour);

			gui.AddSpacing();
			UiWidgets.Heading(gui, Row(gui, 0.85f), "EFFORT");
			UiWidgets.Row(gui, Row(gui, 1f), "Attempts", level.Attempts, HudPalette.Default);
			UiWidgets.Row(gui, Row(gui, 1f), "Crashes", level.Crashes, HudPalette.Default);
			UiWidgets.Row(gui, Row(gui, 1f), "Wheels Lost", level.WheelsLost, HudPalette.Default);
			UiWidgets.Row(gui, Row(gui, 1f), "Time Spent", level.Duration, HudPalette.Default);
		}
		finally
		{
			gui.EndScrollable();
		}
	}

	/// <summary>
	///     The level's own picture, at the game's 16:9, or a box saying there is none. Loaded
	///     asynchronously, so the first frame or two draw the placeholder - see
	///     <see cref="LevelThumbnails" />.
	/// </summary>
	private static void DrawThumbnail(ImGui gui, LevelRow level)
	{
		float width = Mathf.Min(gui.GetLayoutWidth(), ThumbnailMaxWidth);
		ImRect row = gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), width * 9f / 16f);
		ImRect box = new(row.X + (row.W - width) * 0.5f, row.Y, width, row.H);

		Texture2D thumbnail = LevelThumbnails.Get(level.Uid);

		if (thumbnail == null)
		{
			gui.Canvas.Rect(box, HudPalette.Track, box.H * 0.05f);
			UiText.Centre(gui, "no thumbnail", HudPalette.Muted, box, gui.Style.Layout.TextSize);
			return;
		}

		gui.Image(thumbnail, box);
	}

	private static void DrawMedalTime(ImGui gui, Sprite sprite, string label, string time, Color32 colour)
	{
		ImRect row = Row(gui, 1.4f);
		float iconSize = row.H;

		ImRect icon = row.TakeLeft(iconSize, gui.Style.Layout.InnerSpacing, out ImRect rest);

		DrawMedalIcon(gui, icon, sprite, iconSize, colour);
		UiWidgets.Row(gui, rest, label, time, colour);
	}

	private static void DrawMedalIcon(ImGui gui, ImRect icon, Sprite sprite, float iconSize, Color32 colour)
	{
		if (sprite == null)
		{
			gui.Canvas.Circle(icon.Center, iconSize * 0.3f, colour);

			return;
		}

		gui.Image(sprite, icon, true);
	}

	/// <summary>
	///     Every run this machine has ever finished, newest first, with the one just played
	///     marked - or the one that was clicked, opened as the report it was when it ended.
	/// </summary>
	private void DrawHistory(ImGui gui, RunReportView report)
	{
		if (_run != null)
		{
			DrawRunDetail(gui, _run);
			return;
		}

		IReadOnlyList<HistoryRow> history = report.History;

		if (history.Count == 0)
		{
			UiText.Left(gui, "No runs recorded yet. This one is the first.", HudPalette.Muted, Row(gui, 1f));
			return;
		}

		DrawHistoryHeader(gui);

		gui.BeginScrollable();

		try
		{
			foreach (HistoryRow record in history)
			{
				if (DrawHistoryRow(gui, record))
				{
					_run = RunReportView.FromRecord(record.Record);
					_level = null;
				}
			}
		}
		finally
		{
			gui.EndScrollable();
		}
	}

	/// <summary>
	///     A past run, opened as the report it was: the headline, the medals, the numbers, and
	///     its own level list - which clicks through into the same level detail as this run's.
	/// </summary>
	private void DrawRunDetail(ImGui gui, RunReportView run)
	{
		if (_level != null)
		{
			DrawLevelDetail(gui, _level);
			return;
		}

		if (UiWidgets.IconButton(gui, ButtonRow(gui), UiIcon.Restart, "Back to the history", HudPalette.ActionRestart))
		{
			_run = null;
			return;
		}

		gui.BeginScrollable();

		try
		{
			DrawHeadline(gui, run);
			DrawRows(gui, run.Summary);

			gui.AddSpacing();
			UiWidgets.Heading(gui, Row(gui, 0.85f), "LEVELS");

			if (run.Levels.Count == 0)
			{
				UiText.Left(gui, "This run was recorded before levels were kept.", HudPalette.Muted, Row(gui, 1f));
				return;
			}

			DrawLevelHeader(gui);

			foreach (LevelRow level in run.Levels)
			{
				if (DrawLevelRow(gui, level))
				{
					_level = level;
				}
			}
		}
		finally
		{
			gui.EndScrollable();
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

	/// <summary>True on the frame the row was clicked.</summary>
	private static bool DrawHistoryRow(ImGui gui, HistoryRow record)
	{
		ImRect row = Row(gui, 1f);
		bool clicked = Clickable(gui, row);

		// The run just played, picked out so it can be compared against the rest at a glance.
		Color32 when = record.IsCurrent ? HudPalette.Positive : HudPalette.Muted;

		UiText.Left(gui, record.When, when, HistoryCell(row, 0));
		UiText.Left(gui, record.Gamemode, HudPalette.Default, HistoryCell(row, 1));
		UiText.Left(gui, record.Medals, HudPalette.Author, HistoryCell(row, 2));
		UiText.Right(gui, record.Levels, HudPalette.Default, HistoryCell(row, 3), gui.Style.Layout.TextSize);
		UiText.Right(gui, record.Driven, HudPalette.Default, HistoryCell(row, 4), gui.Style.Layout.TextSize);

		return clicked;
	}

	/// <summary>
	///     Makes a row of plain text behave like a button without looking like one. The hover
	///     tint is drawn under the text rather than the row being a real button, because a
	///     button centres its own label and this row has five columns that have to line up with
	///     the header above them.
	/// </summary>
	private static bool Clickable(ImGui gui, ImRect row)
	{
		// The id is taken first so the hover can be asked about by name. Drawing the tint before
		// the text also matters: the canvas paints in call order, and a highlight painted after
		// its row would cover the row it is highlighting.
		uint id = gui.GetNextControlId();
		bool clicked = gui.InvisibleButton(id, row);

		if (gui.IsControlHovered(id))
		{
			gui.Canvas.Rect(row, HudPalette.Track, row.H * 0.2f);
		}

		return clicked;
	}

	private static ImRect HistoryCell(ImRect row, int column)
	{
		return UiWidgets.Cell(row, HistoryWeights, column);
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

	/// <summary>True on the frame the row was clicked.</summary>
	private static bool DrawLevelRow(ImGui gui, LevelRow level)
	{
		ImRect row = Row(gui, 1f);
		bool clicked = Clickable(gui, row);

		UiText.Left(gui, UiNumbers.Text(level.Index), HudPalette.Muted, Cell(row, 0));
		UiText.Left(gui, level.Title, HudPalette.LevelName, Cell(row, 1));
		UiText.Left(gui, level.Status, level.StatusColour, Cell(row, 2));
		UiText.Right(gui, level.Attempts, HudPalette.Default, Cell(row, 3), gui.Style.Layout.TextSize);
		UiText.Right(gui, level.Duration, HudPalette.Default, Cell(row, 4), gui.Style.Layout.TextSize);

		return clicked;
	}

	/// <summary>
	///     Column layout for the level list, as shares of the row. Fixed widths would either
	///     crush the level name or waste half the screen on a two digit attempt count.
	/// </summary>
	private static ImRect Cell(ImRect row, int column)
	{
		return UiWidgets.Cell(row, LevelWeights, column);
	}

	/// <summary>A layout row <paramref name="scale" /> times the theme's text size tall.</summary>
	private static ImRect Row(ImGui gui, float scale)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), gui.GetRowHeight() * scale);
	}

	private static ImRect ButtonRow(ImGui gui)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), UiMetrics.ButtonHeight(gui));
	}
}
