using System;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     Everything about the level currently being played: what it is, what it wants, and how
///     badly it is going.
///     Separate from <see cref="ControlPanel" /> because it answers a different question and
///     is looked at at a different time - the control panel is glanced at mid-run, this is
///     read between attempts. Keeping them apart also means the control panel stays short
///     enough to sit in a corner.
/// </summary>
public class LevelStatsPanel : IZeepGUIDrawer
{
	private const string WindowTitle = "Current Level";

	private const float WidthFraction = 0.14f;

	private const float MinWidth = 200f;
	private const float MaxWidth = 280f;

	private const float TitleSize = 1.25f;
	private const float MedalRowSize = 1.4f;

	private const ImWindowFlag WindowFlags = ImWindowFlag.NoCloseButton | ImWindowFlag.NoResizing;

	private float _contentHeight;

	private bool _mouseOverWindow;

	public AthStateMachine ActiveRun { get; set; }

	public bool Visible { get; set; }

	public void OnZeepGUI(ImGui gui)
	{
		AthStateMachine run = ActiveRun;

		if (!Visible || run == null)
		{
			return;
		}

		LevelStatsView view = LevelStatsView.From(run.Ctx, run.IsBetweenAttempts);

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
			Logger.LogError($"LevelStatsPanel: Draw failed, hiding the panel: {e.Message}\n{e.StackTrace}");
			Visible = false;
		}
	}

	public void Toggle()
	{
		Visible = !Visible;
	}

	private void Draw(ImGui gui, LevelStatsView view)
	{
		using (UiScale.Push(gui))
		{
			DrawScaled(gui, view);
		}
	}

	private void DrawScaled(ImGui gui, LevelStatsView view)
	{
		float width = UiMetrics.Width(gui, WidthFraction, MinWidth, MaxWidth);

		ImRect rect = ImWindowPlacement.PlaceAutoSized(gui, WindowTitle.AsSpan(), width, Height(gui),
			ImWindowAnchor.TopRight);

		bool open = true;

		if (!gui.BeginWindow(WindowTitle, ref open, ref _mouseOverWindow, rect, WindowFlags))
		{
			return;
		}

		try
		{
			DrawIdentity(gui, view);
			DrawTargets(gui, view);
			DrawEffort(gui, view);
			DrawPace(gui, view);

			_contentHeight = UiMetrics.ContentHeight(gui);
		}
		finally
		{
			gui.EndWindow();
		}
	}

	private static void DrawIdentity(ImGui gui, LevelStatsView view)
	{
		float text = gui.Style.Layout.TextSize;

		UiText.Draw(gui, view.Name, Color.Style.Text.LevelName, Row(gui, TitleSize * 1.2f), text * TitleSize, 0f);
		UiText.Draw(gui, view.ByAuthor, Color.Style.Text.AuthorName, Row(gui, 0.9f), text * 0.85f, 0f);
		gui.AddSpacing();
	}

	private static void DrawTargets(ImGui gui, LevelStatsView view)
	{
		DrawMedalTime(gui, GameSprites.AuthorMedal, "Author", view.AuthorTime, Color.Zeepkist.Medal.Author);
		DrawMedalTime(gui, GameSprites.GoldMedal, "Gold", view.GoldTime, Color.Zeepkist.Medal.Gold);

		if (view.BestWithDelta == null)
		{
			UiWidgets.Row(gui, Row(gui, 1f), "Your Best", "not finished yet", Color.Style.Text.Muted);
			gui.AddSpacing();
			return;
		}

		UiWidgets.Row(gui, Row(gui, 1f), "Your Best", view.BestWithDelta, view.PersonalBestColour);
		gui.AddSpacing();
	}

	private static void DrawMedalTime(ImGui gui, Sprite sprite, string label, string time, Color32 colour)
	{
		ImRect row = Row(gui, MedalRowSize);
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

	private static void DrawEffort(ImGui gui, LevelStatsView view)
	{
		UiWidgets.Heading(gui, Row(gui, 0.85f), "EFFORT");

		UiWidgets.Row(gui, Row(gui, 1f), "Attempts", view.Attempts,
			view.AttemptPending ? Color.Style.Pace.Close : Color.Style.Text.Default);
		UiWidgets.Row(gui, Row(gui, 1f), "Crashes", view.Crashes, Color.Style.Text.Default);
		UiWidgets.Row(gui, Row(gui, 1f), "Wheels Lost", view.WheelsLost, Color.Style.Text.Default);
		UiWidgets.Row(gui, Row(gui, 1f), "Time Here", view.TimeOnLevel, Color.Style.Text.Default);
		gui.AddSpacing();
	}

	private static void DrawPace(ImGui gui, LevelStatsView view)
	{
		ImRect row = Row(gui, 1.2f);
		Color32 colour = LevelStatsView.PaceColour(view.Pace);

		float dotSize = row.H;
		ImRect dot = row.TakeLeft(dotSize, gui.Style.Layout.InnerSpacing, out ImRect rest);

		gui.Canvas.Circle(dot.Center, dotSize * 0.28f, colour);
		UiText.Left(gui, LevelStatsView.PaceLabel(view.Pace), colour, rest);
	}

	private static ImRect Row(ImGui gui, float scale)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), gui.GetRowHeight() * scale);
	}

	private float Height(ImGui gui)
	{
		float content = _contentHeight > 0f ? _contentHeight : gui.GetRowHeight() * 14f;

		return content + UiMetrics.WindowChrome(gui) + UiMetrics.Slack(gui);
	}
}
