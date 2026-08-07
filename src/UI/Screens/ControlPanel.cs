using System;
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
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI.Screens;

/// <summary>
///     The buttons: skip, pause, restart, stop, and the Start button when nothing is running.
///     Two things used to be wrong with it. It carried the title "Author Time Hunting", which
///     is the ATH menu's title, and Imui keys a window's position, size and stacking order off
///     that string - so the two windows shared one slot and fought over it whenever both were
///     up. And it placed itself under the level panel by reading that panel's rect back, which
///     put a window's position in another window's hands and drifted the moment either changed
///     height. It has its own title and its own corner now.
///     Session-scoped, registered once for the whole game session: it is also what a player
///     sees when nothing is running, and that is where the Start button lives.
/// </summary>
public class ControlPanel : IZeepGUIDrawer
{
	private const string WindowTitle = "ATH Controls";

	private const float WidthFraction = 0.21f;

	private const float MinWidth = 320f;
	private const float MaxWidth = 440f;

	private const ImWindowFlag WindowFlags = ImWindowFlag.NoCloseButton | ImWindowFlag.NoResizing;

	private float _contentHeight;

	private bool _mouseOverWindow;

	public AthStateMachine ActiveRun
	{
		get;
		set
		{
			field = value;

			if (value != null)
			{
				Visible = true;
			}
		}
	}

	public bool Visible { get; set; }

	public void OnZeepGUI(ImGui gui)
	{
		if (!Visible)
		{
			return;
		}

		try
		{
			Draw(gui);
		}
		catch (Exception e)
		{
			Logger.LogError($"ControlPanel: Draw failed, hiding the panel: {e.Message}\n{e.StackTrace}");
			Visible = false;
		}
	}

	private void Draw(ImGui gui)
	{
		AthStateMachine run = ActiveRun;
		RunHudView view = RunHudView.ForFrame(run);

		float width = UiMetrics.Width(gui, WidthFraction, MinWidth, MaxWidth);

		ImRect rect = ImWindowPlacement.PlaceAutoSized(gui, WindowTitle.AsSpan(), width, Height(gui),
			ImWindowAnchor.BottomRight);

		bool open = true;

		if (!gui.BeginWindow(WindowTitle, ref open, ref _mouseOverWindow, rect, WindowFlags))
		{
			return;
		}

		try
		{
			DrawBody(gui, run, view);

			_contentHeight = UiMetrics.ContentHeight(gui);
		}
		finally
		{
			gui.EndWindow();
		}
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

	private static void DrawControls(ImGui gui, AthStateMachine run, RunHudView view)
	{
		DrawSkipSection(gui, view);
		DrawRunSection(gui, run, view);
	}

	private static void DrawSkipSection(ImGui gui, RunHudView view)
	{
		UiWidgets.Heading(gui, UiMetrics.Row(gui, 0.85f), "SKIP");

		bool racing = GameStateObserver.IsRacing;

		if (UiWidgets.IconButton(gui, UiMetrics.ButtonRow(gui), UiIcon.Skip, view.SkipType, view.SkipColour, racing))
		{
			ChatApi.SendMessage("/fs");
		}

		if (UiWidgets.IconButton(gui, UiMetrics.ButtonRow(gui), UiIcon.Warning, "Level is Broken", Color.Style.Action.Broken,
			    racing))
		{
			AthRequests.SkipBroken();
		}

		gui.AddSpacing();
	}

	private static void DrawRunSection(ImGui gui, AthStateMachine run, RunHudView view)
	{
		UiWidgets.Heading(gui, UiMetrics.Row(gui, 0.85f), "RUN");

		ImRect row = UiMetrics.ButtonRow(gui);

		if (UiWidgets.IconButton(gui, UiWidgets.Column(gui, row, 0, 2), view.Paused ? UiIcon.Play : UiIcon.Pause,
			    view.Paused ? "Resume" : "Pause", view.Paused ? Color.Style.Action.Resume : Color.Style.Action.Pause))
		{
			TogglePause(run, view);
		}

		if (UiWidgets.IconButton(gui, UiWidgets.Column(gui, row, 1, 2), UiIcon.Restart, "Restart",
			    Color.Style.Action.Restart))
		{
			AthRequests.Restart();
		}

		if (UiWidgets.IconButton(gui, UiMetrics.ButtonRow(gui), UiIcon.Stop, "Stop Hunt", Color.Style.Action.Stop))
		{
			AthRequests.Stop();
		}
	}

	private float Height(ImGui gui)
	{
		return UiMetrics.WindowHeight(gui, _contentHeight, 8f);
	}
}
