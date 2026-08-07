using System;
using System.Collections.Generic;
using AuthorTimeHunting.UI.Hud;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.UI.Views;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI.Screens;

/// <summary>
///     The end-of-run report: a large results screen with tabs, in the shape a party game
///     puts up when the round is over.
///     Deliberately a step coarser than the rest of the mod's UI. The panels beside it are
///     read at a glance mid-run and are sized to stay out of the way; this is read once, when
///     nothing else is happening, and it is the only place the whole run is visible at all.
///     It holds a snapshot rather than the run, because the run's state machine is destroyed
///     the moment it stops.
///     Nothing in here opens a scrollable of its own, and that is load-bearing. Imui's
///     BeginScrollable takes the whole of the layout frame it is in, not the part still free,
///     so one opened after a header or a Back button is laid out on top of it - which is what
///     made the level list and the history unusable. Both the window and every tab already
///     bring their own, so the pages just add rows and let those do the scrolling.
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

	private const float ThumbnailMaxWidth = 360f;

	private const ImWindowFlag WindowFlags = ImWindowFlag.NoResizing;

	private static readonly float[] LevelWeights = [0.06f, 0.44f, 0.2f, 0.12f, 0.18f];

	private LevelRow _level;

	private bool _mouseOverWindow;
	private RunReportView _report;

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

		if (!open)
		{
			Close();
		}
	}

	private static void DrawHeadline(ImGui gui, RunReportView report)
	{
		float text = gui.Style.Layout.TextSize;

		UiText.Centre(gui, report.Kicker, Color.Style.Text.Muted, UiMetrics.Row(gui, 1f), text * 0.9f);
		UiText.Centre(gui, report.PlayerName, Color.Style.Surface.White, UiMetrics.Row(gui, HeadlineSize * 1.2f),
			text * HeadlineSize);
		UiText.Centre(gui, report.Headline, Color.Zeepkist.Medal.Author, UiMetrics.Row(gui, 1.4f), text * 1.2f);

		ImRect medals = UiMetrics.Row(gui, MedalRowSize);

		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, medals, 0, 3), GameSprites.AuthorMedal, report.AuthorMedals,
			Color.Zeepkist.Medal.Author);
		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, medals, 1, 3), GameSprites.GoldMedal, report.GoldMedals,
			Color.Zeepkist.Medal.Gold);
		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, medals, 2, 3), GameSprites.YouTriedMedal, report.Penalties,
			Color.Style.Status.Penalty);

		gui.AddSpacing();
	}

	private void DrawTabs(ImGui gui, RunReportView report)
	{
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
			UiWidgets.Row(gui, UiMetrics.Row(gui, 1f), row.Label, row.Value, row.ValueColour);
		}
	}

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
			UiText.Left(gui, "No levels were played.", Color.Style.Text.Muted, UiMetrics.Row(gui, 1f));
			return;
		}

		DrawLevelHeader(gui);

		foreach (LevelRow level in levels)
		{
			if (DrawLevelRow(gui, level))
			{
				_level = level;
			}
		}
	}

	private void DrawLevelDetail(ImGui gui, LevelRow level)
	{
		float text = gui.Style.Layout.TextSize;

		if (UiWidgets.IconButton(gui, UiMetrics.ButtonRow(gui), UiIcon.Restart, "Back to the list", Color.Style.Action.Restart))
		{
			_level = null;
		}

		DrawThumbnail(gui, level);

		UiText.Centre(gui, level.Name, Color.Style.Text.LevelName, UiMetrics.Row(gui, 1.6f), text * 1.4f);
		UiText.Centre(gui, level.ByAuthor, Color.Style.Text.AuthorName, UiMetrics.Row(gui, 1f), text * 0.9f);
		UiText.Centre(gui, level.StatusUpper, level.StatusColour, UiMetrics.Row(gui, 1.8f), text * 1.5f);
		gui.AddSpacing();

		DrawMedalTime(gui, GameSprites.AuthorMedal, "Author Time", level.AuthorTime, Color.Zeepkist.Medal.Author);
		DrawMedalTime(gui, GameSprites.GoldMedal, "Gold Time", level.GoldTime, Color.Zeepkist.Medal.Gold);

		UiWidgets.Row(gui, UiMetrics.Row(gui, 1f), "Your Best", level.BestWithDelta ?? "never finished",
			level.BestWithDelta == null ? Color.Style.Text.Muted : level.StatusColour);

		gui.AddSpacing();
		UiWidgets.Heading(gui, UiMetrics.Row(gui, 0.85f), "EFFORT");
		UiWidgets.Row(gui, UiMetrics.Row(gui, 1f), "Attempts", level.Attempts, Color.Style.Text.Default);
		UiWidgets.Row(gui, UiMetrics.Row(gui, 1f), "Crashes", level.Crashes, Color.Style.Text.Default);
		UiWidgets.Row(gui, UiMetrics.Row(gui, 1f), "Wheels Lost", level.WheelsLost, Color.Style.Text.Default);
		UiWidgets.Row(gui, UiMetrics.Row(gui, 1f), "Time Spent", level.Duration, Color.Style.Text.Default);
	}

	private static void DrawThumbnail(ImGui gui, LevelRow level)
	{
		float width = Mathf.Min(gui.GetLayoutWidth(), ThumbnailMaxWidth);
		ImRect row = gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), width * 9f / 16f);
		ImRect box = new(row.X + (row.W - width) * 0.5f, row.Y, width, row.H);

		Texture2D thumbnail = LevelThumbnails.Get(level.Uid);

		if (thumbnail == null)
		{
			gui.Canvas.Rect(box, Color.Style.Surface.Track, box.H * 0.05f);
			UiText.Centre(gui, "no thumbnail", Color.Style.Text.Muted, box, gui.Style.Layout.TextSize);
			return;
		}

		gui.Image(thumbnail, box);
	}

	private static void DrawMedalTime(ImGui gui, Sprite sprite, string label, string time, Color32 colour)
	{
		ImRect row = UiMetrics.Row(gui, 1.4f);
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
			UiText.Left(gui, "No runs recorded yet. This one is the first.", Color.Style.Text.Muted, UiMetrics.Row(gui, 1f));
			return;
		}

		HistoryTable.Header(gui);

		foreach (HistoryRow record in history)
		{
			if (HistoryTable.Row(gui, record))
			{
				_run = RunReportView.FromRecord(record.Record);
				_level = null;
			}
		}
	}

	private void DrawRunDetail(ImGui gui, RunReportView run)
	{
		if (_level != null)
		{
			DrawLevelDetail(gui, _level);
			return;
		}

		if (UiWidgets.IconButton(gui, UiMetrics.ButtonRow(gui), UiIcon.Restart, "Back to the history",
			    Color.Style.Action.Restart))
		{
			_run = null;
			return;
		}

		DrawHeadline(gui, run);
		DrawRows(gui, run.Summary);

		gui.AddSpacing();
		UiWidgets.Heading(gui, UiMetrics.Row(gui, 0.85f), "LEVELS");

		if (run.Levels.Count == 0)
		{
			UiText.Left(gui, "This run was recorded before levels were kept.", Color.Style.Text.Muted, UiMetrics.Row(gui, 1f));

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

	private static void DrawLevelHeader(ImGui gui)
	{
		ImRect row = UiMetrics.Row(gui, 0.9f);
		float size = gui.Style.Layout.TextSize * 0.8f;

		UiText.Draw(gui, "#", Color.Style.Text.Muted, Cell(row, 0), size, 0f);
		UiText.Draw(gui, "LEVEL", Color.Style.Text.Muted, Cell(row, 1), size, 0f);
		UiText.Draw(gui, "RESULT", Color.Style.Text.Muted, Cell(row, 2), size, 0f);
		UiText.Draw(gui, "TRIES", Color.Style.Text.Muted, Cell(row, 3), size, 1f);
		UiText.Draw(gui, "TIME", Color.Style.Text.Muted, Cell(row, 4), size, 1f);
	}

	private static bool DrawLevelRow(ImGui gui, LevelRow level)
	{
		ImRect row = UiMetrics.Row(gui, 1f);
		bool clicked = UiWidgets.Clickable(gui, row);

		UiText.Left(gui, UiNumbers.Text(level.Index), Color.Style.Text.Muted, Cell(row, 0));
		UiText.Left(gui, level.Title, Color.Style.Text.LevelName, Cell(row, 1));
		UiText.Left(gui, level.Status, level.StatusColour, Cell(row, 2));
		UiText.Right(gui, level.Attempts, Color.Style.Text.Default, Cell(row, 3), gui.Style.Layout.TextSize);
		UiText.Right(gui, level.Duration, Color.Style.Text.Default, Cell(row, 4), gui.Style.Layout.TextSize);

		return clicked;
	}

	private static ImRect Cell(ImRect row, int column)
	{
		return UiWidgets.Cell(row, LevelWeights, column);
	}
}
