using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.UI.Views;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.UI.Screens;

/// <summary>
///     The buttons: skip, level is broken, pause, restart, stop, and the Start button when
///     nothing is running.
///     It is not a window any more. It used to own a corner of the screen and hold it all hour
///     for five buttons that get pressed a handful of times, so it moved into the drawer under
///     <see cref="RunOverlay" /> and is drawn into whatever rect the bar hands it. That is why
///     there is no Visible here and no placement: the bar decides when and where, this decides
///     what.
///     While a hunt is on, the five are symbols in a row. Their labels were the only thing
///     making the panel as wide as it was, and none of them said anything the shape did not -
///     except the skip, whose price changes, so that one line is written above the row instead.
///     With no hunt on it stays words. "Start Hunt" and the gamemode picker are choices rather
///     than transport controls, and there is no shape for either.
/// </summary>
public class ControlPanel
{
	private const int Buttons = 5;

	/// <summary>
	///     Measured separately, because the two states are nothing like the same height and the
	///     drawer is shut across the moment one becomes the other. One shared number would mean
	///     every hunt started by opening a drawer sized for the idle screen, and ended by opening
	///     one sized for five buttons.
	/// </summary>
	private float _idleHeight;

	private float _runningHeight;

	/// <summary>
	///     How far the drawer has to open to show all of this. Taken off the last frame that drew
	///     this state, because the bar has to size the drawer before anything is in it - the guess
	///     in rows only ever stands until the first measurement.
	/// </summary>
	public float Height(ImGui gui, bool idle)
	{
		float measured = idle ? _idleHeight : _runningHeight;
		float fallback = gui.GetRowHeight() * (idle ? 9f : 3.4f);

		return (measured > 0f ? measured : fallback) + UiMetrics.Margin(gui);
	}

	public void Draw(ImGui gui, ImRect rect, AthStateMachine run, RunHudView view)
	{
		gui.Layout.Push(ImAxis.Vertical, rect);

		try
		{
			DrawBody(gui, run, view);

			Measure(gui, view == null);
		}
		finally
		{
			gui.Layout.Pop();
		}
	}

	private void Measure(ImGui gui, bool idle)
	{
		if (idle)
		{
			_idleHeight = UiMetrics.ContentHeight(gui);

			return;
		}

		_runningHeight = UiMetrics.ContentHeight(gui);
	}

	private static void DrawBody(ImGui gui, AthStateMachine run, RunHudView view)
	{
		if (view == null)
		{
			DrawIdle(gui, run != null);

			return;
		}

		DrawControls(gui, run, view);
	}

	private static void TogglePause(AthStateMachine run, RunHudView view)
	{
		if (view.Paused)
		{
			run.ResumeRun();

			return;
		}

		run.PauseRun();
	}

	private static void DrawIdle(ImGui gui, bool starting)
	{
		UiText.Left(gui, starting ? "Waiting for the level to load..." : "No hunt running.", Color.Style.Text.Muted,
			UiMetrics.Row(gui, 1f));
		gui.AddSpacing();

		if (starting)
		{
			if (UiWidgets.IconButton(gui, UiMetrics.ButtonRow(gui), UiIcon.Stop, "Stop", Color.Style.Action.Stop))
			{
				AthRequests.Stop();
			}

			return;
		}

		DrawGamemodePicker(gui);

		if (UiWidgets.IconButton(gui, UiMetrics.ButtonRow(gui), UiIcon.Play, "Start Hunt", Color.Style.Action.Resume))
		{
			AthRequests.Start();
		}
	}

	private static void DrawGamemodePicker(ImGui gui)
	{
		GamemodeRegistry registry = Plugin.Instance.Services.Gamemodes;

		UiWidgets.Heading(gui, UiMetrics.Row(gui, 0.85f), "GAMEMODE");
		UiText.Left(gui, registry.Selected.DisplayName, Color.Zeepkist.Medal.Author, UiMetrics.Row(gui, 1f));
		UiText.Draw(gui, registry.Selected.Description, Color.Style.Text.Muted, UiMetrics.Row(gui, 0.9f),
			gui.Style.Layout.TextSize * 0.85f, 0f);

		if (registry.All.Count < 2)
		{
			gui.AddSpacing();
			return;
		}

		if (UiWidgets.Button(gui, UiMetrics.ButtonRow(gui), "Next Gamemode"))
		{
			registry.SelectNext();
		}

		gui.AddSpacing();
	}

	/// <summary>
	///     The price of a skip above, the five symbols below. The price is the one thing here that
	///     changes hour to hour - free, gold, penalty, fatal - and the one thing no icon can carry.
	/// </summary>
	private static void DrawControls(ImGui gui, AthStateMachine run, RunHudView view)
	{
		UiText.Centre(gui, view.SkipType, view.SkipColour, UiMetrics.Row(gui, 1f), gui.Style.Layout.TextSize * 0.95f);

		bool racing = GameStateObserver.IsRacing;
		ImRect row = Strip(gui);

		if (UiWidgets.IconOnlyButton(gui, Button(gui, row, 0), UiIcon.Skip, Color.Style.Action.Skip, racing))
		{
			ChatApi.SendMessage("/fs");
		}

		if (UiWidgets.IconOnlyButton(gui, Button(gui, row, 1), UiIcon.Warning, Color.Style.Action.Broken, racing))
		{
			AthRequests.SkipBroken();
		}

		if (UiWidgets.IconOnlyButton(gui, Button(gui, row, 2), view.Paused ? UiIcon.Play : UiIcon.Pause,
			    view.Paused ? Color.Style.Action.Resume : Color.Style.Action.Pause, true))
		{
			TogglePause(run, view);
		}

		if (UiWidgets.IconOnlyButton(gui, Button(gui, row, 3), UiIcon.Restart, Color.Style.Action.Restart, true))
		{
			AthRequests.Restart();
		}

		if (UiWidgets.IconOnlyButton(gui, Button(gui, row, 4), UiIcon.Stop, Color.Style.Action.Stop, true))
		{
			AthRequests.Stop();
		}
	}

	/// <summary>
	///     Five squares in the middle of the bar rather than five stretched across it. The bar is
	///     as wide as the screen, and a stop button an ultrawide wide is not a button anybody aims
	///     at - it is a region you fall into.
	/// </summary>
	private static ImRect Strip(ImGui gui)
	{
		float side = UiMetrics.ButtonHeight(gui);
		float gap = gui.Style.Layout.InnerSpacing;
		float width = side * Buttons + gap * (Buttons - 1);

		ImRect row = UiMetrics.ButtonRow(gui);

		return new ImRect(row.X + (row.W - width) * 0.5f, row.Y, width, side);
	}

	private static ImRect Button(ImGui gui, ImRect strip, int index)
	{
		float side = strip.H;
		float gap = gui.Style.Layout.InnerSpacing;

		return new ImRect(strip.X + index * (side + gap), strip.Y, side, side);
	}
}
