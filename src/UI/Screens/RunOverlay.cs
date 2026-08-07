using System;
using AuthorTimeHunting.States.Ath.StateMachine;
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
///     The run, across the top of the screen: what is left of the hour, the medals taken so far,
///     and under a rule the level being driven right now.
///     It had a title bar reading "Run" and a footer repeating the skip type, and both were
///     there for the wrong reason - the title bar because an earlier attempt at drawing text
///     outside a window drew nothing, the footer because the panel it grew out of had room for
///     it. A strip of chrome across the top of the screen is the one place a player is looking
///     while driving, and neither line was worth the height. Imui draws text fine inside a
///     window without a title bar, so that is what this is now, and the skip type lives where
///     the skip button is.
///     The level half arrived from a second window called "Current Level", which sat in the
///     opposite corner and had to be opened before it said anything. Two windows asking about
///     the same moment is one window too many, so the four lines worth reading mid-run - what
///     the level is called, whose it is, the two times to beat, which attempt this is - moved
///     in here and the other window went away.
///     One number is large, everything else is small. Nothing here is read on purpose - it is
///     read out of the corner of an eye, mid-air, and the layout has to survive that.
/// </summary>
public class RunOverlay : IZeepGUIDrawer
{
	private const string WindowTitle = "ATH Run";

	private const float WidthFraction = 0.22f;

	private const float MinWidth = 320f;
	private const float MaxWidth = 480f;

	private const float ClockSize = 2.4f;
	private const float MedalRowSize = 1.4f;
	private const float PenaltySize = 0.85f;

	private const float LevelNameSize = 1.1f;
	private const float AuthorSize = 0.85f;
	private const float MedalTimeRowSize = 1.2f;

	private const ImWindowFlag WindowFlags =
		ImWindowFlag.NoTitleBar | ImWindowFlag.NoCloseButton | ImWindowFlag.NoMovingAndResizing;

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

		RunHudView view = RunHudView.ForFrame(run);

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
			Logger.LogError($"RunOverlay: Draw failed, hiding it: {e.Message}\n{e.StackTrace}");
			Visible = false;
		}
	}

	private void Draw(ImGui gui, RunHudView view)
	{
		float width = UiMetrics.Width(gui, WidthFraction, MinWidth, MaxWidth);

		ImRect rect = ImWindowPlacement.PlaceAutoSized(gui, WindowTitle.AsSpan(), width, Height(gui),
			ImWindowAnchor.TopCenter);

		bool open = true;

		if (!gui.BeginWindow(WindowTitle, ref open, ref _mouseOverWindow, rect, WindowFlags))
		{
			return;
		}

		try
		{
			DrawClock(gui, view);
			UiWidgets.Bar(gui, UiMetrics.Row(gui, 0.3f), view.RemainingFraction, view.TimeColour);
			DrawMedals(gui, UiMetrics.Row(gui, MedalRowSize), view);
			DrawPenalties(gui, view);
			UiScreen.Rule(gui, UiMetrics.Row(gui, 0.3f), Color.Zeepkist.Medal.Author);
			DrawLevel(gui, view);

			_contentHeight = UiMetrics.ContentHeight(gui);
		}
		finally
		{
			gui.EndWindow();
		}
	}

	private static void DrawClock(ImGui gui, RunHudView view)
	{
		float size = gui.Style.Layout.TextSize * ClockSize;
		ImRect row = UiMetrics.Row(gui, ClockSize * 1.15f);

		if (view.Paused)
		{
			UiText.Centre(gui, "PAUSED", Color.Style.Status.Warning, row, size);

			return;
		}

		UiText.Centre(gui, view.TimeLeft, view.TimeColour, row, size);
	}

	private static void DrawMedals(ImGui gui, ImRect row, RunHudView view)
	{
		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, row, 0, 3), GameSprites.AuthorMedal, view.AuthorMedals,
			Color.Zeepkist.Medal.Author);
		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, row, 1, 3), GameSprites.GoldMedal, view.GoldMedals,
			Color.Zeepkist.Medal.Gold);
		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, row, 2, 3), GameSprites.YouTriedMedal, view.Penalties,
			Color.Style.Status.Penalty);
	}

	private static void DrawPenalties(ImGui gui, RunHudView view)
	{
		if (view.Penalties == 0)
		{
			return;
		}

		UiText.Centre(gui, $"-{view.TimeLostToPenalties} in penalties", Color.Style.Status.Bad, UiMetrics.Row(gui, 1f),
			gui.Style.Layout.TextSize * PenaltySize);
	}

	private static void DrawLevel(ImGui gui, RunHudView view)
	{
		float text = gui.Style.Layout.TextSize;

		UiText.Draw(gui, view.LevelName, Color.Style.Text.LevelName, UiMetrics.Row(gui, LevelNameSize * 1.2f),
			text * LevelNameSize, 0f);
		UiText.Draw(gui, view.ByAuthor, Color.Style.Text.AuthorName, UiMetrics.Row(gui, 0.9f), text * AuthorSize, 0f);

		DrawMedalTime(gui, GameSprites.AuthorMedal, "AT", view.AuthorTime, Color.Zeepkist.Medal.Author);
		DrawMedalTime(gui, GameSprites.GoldMedal, "Gold", view.GoldTime, Color.Zeepkist.Medal.Gold);

		UiWidgets.Row(gui, UiMetrics.Row(gui, 1f), "Attempt", view.Attempts, Color.Style.Text.Default);
	}

	private static void DrawMedalTime(ImGui gui, Sprite sprite, string label, string time, Color32 colour)
	{
		ImRect row = UiMetrics.Row(gui, MedalTimeRowSize);
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

	private float Height(ImGui gui)
	{
		float content = _contentHeight > 0f ? _contentHeight : gui.GetRowHeight() * 11f;

		return content + UiMetrics.ContentPadding(gui) + UiMetrics.Slack(gui);
	}
}
