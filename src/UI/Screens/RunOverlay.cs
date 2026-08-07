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
///     What is left of the hour, across the top of the screen: the clock, the budget behind it
///     as a bar, and the medals earned so far.
///     It had a title bar reading "Run" and a footer repeating the skip type, and both were
///     there for the wrong reason - the title bar because an earlier attempt at drawing text
///     outside a window drew nothing, the footer because the panel it grew out of had room for
///     it. A strip of chrome across the top of the screen is the one place a player is looking
///     while driving, and neither line was worth the height. Imui draws text fine inside a
///     window without a title bar, so that is what this is now, and the skip type lives where
///     the skip button is.
///     One number is large, everything else is small. Nothing here is read on purpose - it is
///     read out of the corner of an eye, mid-air, and the layout has to survive that.
/// </summary>
public class RunOverlay : IZeepGUIDrawer
{
	private const string WindowTitle = "ATH Run";

	private const float WidthFraction = 0.16f;

	private const float MinWidth = 200f;
	private const float MaxWidth = 300f;

	private const float ClockSize = 2.4f;
	private const float MedalRowSize = 1.4f;
	private const float PenaltySize = 0.85f;

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

		RunHudView view = RunHudView.ForFrame(run.Ctx);

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
		using (UiScale.Push(gui))
		{
			DrawScaled(gui, view);
		}
	}

	private void DrawScaled(ImGui gui, RunHudView view)
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

	private float Height(ImGui gui)
	{
		float content = _contentHeight > 0f ? _contentHeight : gui.GetRowHeight() * 6f;

		return content + UiMetrics.ContentPadding(gui) + UiMetrics.Slack(gui);
	}
}
