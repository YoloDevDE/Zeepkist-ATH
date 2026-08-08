using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.UI.Views;
using AuthorTimeHunting.Util;
using Imui.Core;
using UnityEngine;

namespace AuthorTimeHunting.UI.Screens;

/// <summary>
///     Past runs as a table of five columns, one row per run, clickable.
///     Lives on its own because the history is asked for from two places now - the report that
///     goes up when a run ends, and the menu, where it is looked up between runs. The columns
///     have to line up in both, and they only do that if there is one method drawing them.
/// </summary>
public static class HistoryTable
{
	private static readonly float[] _weights = [0.26f, 0.24f, 0.24f, 0.11f, 0.15f];

	public static void Header(ImGui gui)
	{
		ImRect row = UiMetrics.Row(gui, 0.9f);
		float size = gui.Style.Layout.TextSize * 0.8f;

		UiText.Draw(gui, "WHEN", Color.Style.Text.Muted, Cell(row, 0), size, 0f);
		UiText.Draw(gui, "MODE", Color.Style.Text.Muted, Cell(row, 1), size, 0f);
		UiText.Draw(gui, "AT / GOLD / PEN", Color.Style.Text.Muted, Cell(row, 2), size, 0f);
		UiText.Draw(gui, "LEVELS", Color.Style.Text.Muted, Cell(row, 3), size, 1f);
		UiText.Draw(gui, "DRIVEN", Color.Style.Text.Muted, Cell(row, 4), size, 1f);
	}

	public static bool Row(ImGui gui, HistoryRow record)
	{
		ImRect row = UiMetrics.Row(gui, 1f);
		bool clicked = UiWidgets.Clickable(gui, row);

		Color32 when = record.IsCurrent ? Color.Style.Status.Positive : Color.Style.Text.Muted;

		UiText.Left(gui, record.When, when, Cell(row, 0));
		UiText.Left(gui, record.Gamemode, Color.Style.Text.Default, Cell(row, 1));
		UiText.Left(gui, record.Medals, Color.Zeepkist.Medal.Author, Cell(row, 2));
		UiText.Right(gui, record.Levels, Color.Style.Text.Default, Cell(row, 3), gui.Style.Layout.TextSize);
		UiText.Right(gui, record.Driven, Color.Style.Text.Default, Cell(row, 4), gui.Style.Layout.TextSize);

		return clicked;
	}

	private static ImRect Cell(ImRect row, int column)
	{
		return UiWidgets.Cell(row, _weights, column);
	}
}
