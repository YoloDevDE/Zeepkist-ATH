using System;
using System.Collections.Generic;
using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.UI.Views;
using AuthorTimeHunting.Util;
using BepInEx.Configuration;
using Imui.Controls;
using Imui.Core;
using Imui.Style;
using UnityEngine;
using ZeepkistClient;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI.Screens;

/// <summary>
///     The mod's front door: what /ath and the top bar open, and the only screen a player has
///     to know about.
///     Everything ATH can do used to be a chat command or a window somebody had to find -
///     starting a hunt, looking up an old one, changing what the run is worth. This is the one
///     place all of it is listed, and it is a menu rather than a panel because a menu is what a
///     game puts up before a round, not while one is being played.
///     Which is why it takes the whole screen. Nothing is happening behind it that the player
///     is watching - a hunt has not started yet, or the one that did is between levels - so the
///     screen is free, and a menu that owns the screen is a menu nobody has to hunt for. The
///     window itself is fullscreen and its side padding is what holds the content to a column in
///     the middle; a choice spread across an ultrawide is not a choice anyone aims at.
///     One window with pages instead of five windows: a player picking a gamemode is not
///     reading the history at the same time, and a page that replaces the one before it cannot
///     end up behind it.
///     No page opens a scrollable of its own. Imui's BeginScrollable claims the whole layout
///     frame it sits in rather than the part still free, so one opened after a header or a Back
///     button lands on top of them - which is what made the history page unreadable. The window
///     already brings one, and it covers the whole page.
/// </summary>
public class AthMenu : IZeepGUIDrawer
{
	private const string WindowTitle = "Author Time Hunting";

	private const string Title = "AUTHOR TIME HUNTING";

	private const string Footer = "/ath, or the ATH button in the top bar, opens and closes this screen.";

	private const string ClassicId = "classic";

	private const float TitleSize = 2f;

	/// <summary>
	///     Tall enough for a title line, a rule and two lines of caption. Under that the caption
	///     loses its second line to an ellipsis, which is where "and where this" came from.
	/// </summary>
	private const float TileRows = 4.4f;

	private const int TileColumns = 2;
	private const int TileRowCount = 3;

	private const int MinMinutes = 1;
	private const int MaxMinutes = 240;

	private const ImWindowFlag WindowFlags = ImWindowFlag.NoResizing | ImWindowFlag.NoMoving |
	                                         ImWindowFlag.NoTitleBar | ImWindowFlag.NoCloseButton;

	private IReadOnlyList<HistoryRow> _history = [];

	private bool _mouseOverWindow;

	private AthMenuPage _page;

	public bool Visible
	{
		get;
		set
		{
			bool wasVisible = field;
			field = value;

			if (value && !wasVisible)
			{
				_page = AthMenuPage.Root;
				Plugin.Instance.Services.LevelSummary.Hide();
			}
		}
	}

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
			Logger.LogError($"AthMenu: Draw failed, closing it: {e.Message}\n{e.StackTrace}");
			Visible = false;
		}
	}

	public void Toggle()
	{
		Visible = !Visible;
	}

	private void Draw(ImGui gui)
	{
		ImRect screen = UiScreen.Full(gui);
		ImStyleWindow previous = gui.Style.Window;

		Fullscreen(gui, screen);

		try
		{
			DrawWindow(gui, screen);
		}
		finally
		{
			gui.Style.Window = previous;
		}
	}

	/// <summary>
	///     Turns the window into the screen: no border, no rounded corners, the mod's backdrop
	///     behind it, and side padding wide enough to leave a column in the middle. Imui hands the
	///     content rect out after the padding, so every row laid out inside is already centred and
	///     no control has to be told how wide the screen is.
	/// </summary>
	private static void Fullscreen(ImGui gui, ImRect screen)
	{
		float side = (screen.W - UiScreen.Width(screen)) * 0.5f;
		float top = UiMetrics.Margin(gui) * 2f;

		gui.Style.Window.Box.BackColor = Color.Style.Surface.Backdrop;
		gui.Style.Window.Box.BorderThickness = 0f;
		gui.Style.Window.Box.BorderRadius = 0f;
		gui.Style.Window.ContentPadding.Left = side;
		gui.Style.Window.ContentPadding.Right = side;
		gui.Style.Window.ContentPadding.Top = top;
		gui.Style.Window.ContentPadding.Bottom = top;
	}

	private void DrawWindow(ImGui gui, ImRect screen)
	{
		bool open = true;

		if (!gui.BeginWindow(WindowTitle, ref open, ref _mouseOverWindow, screen, WindowFlags))
		{
			return;
		}

		try
		{
			if (DrawHeadline(gui))
			{
				return;
			}

			DrawPage(gui);
		}
		finally
		{
			gui.EndWindow();
		}
	}

	/// <summary>
	///     The title band, and the Back button parked in it. Back lives up here rather than as a
	///     row of its own because a full-width button above a page reads as part of the page.
	/// </summary>
	/// <returns>True when Back was pressed, which leaves nothing under the band worth drawing.</returns>
	private bool DrawHeadline(ImGui gui)
	{
		float text = gui.Style.Layout.TextSize;
		ImRect band = UiMetrics.Row(gui, TitleSize * 1.6f);

		UiText.Centre(gui, UiScreen.Spaced(Title), Color.Zeepkist.Medal.Author, band, text * TitleSize);
		UiText.Centre(gui, Subtitle(), Color.Style.Text.Muted, UiMetrics.Row(gui, 1.5f), text * 1.05f);
		UiScreen.Rule(gui, UiMetrics.Row(gui, 1f), Color.Zeepkist.Medal.Author);

		gui.AddSpacing();

		return Back(gui, band);
	}

	private string Subtitle()
	{
		switch (_page)
		{
			case AthMenuPage.Play:
				return "Pre-Game Setup";
			case AthMenuPage.History:
				return "Challenge History";
			case AthMenuPage.Settings:
				return "Settings";
			default:
				return "Beat the author's time. Then do it again, until the hour is gone.";
		}
	}

	private void DrawPage(ImGui gui)
	{
		switch (_page)
		{
			case AthMenuPage.Play:
				DrawPlay(gui);
				return;
			case AthMenuPage.History:
				DrawHistory(gui);
				return;
			case AthMenuPage.Settings:
				DrawSettings(gui);
				return;
			default:
				DrawRoot(gui);
				return;
		}
	}

	/// <summary>
	///     Six tiles in two columns, sitting in the middle of whatever height is left, with the
	///     line about /ath at the bottom of it.
	/// </summary>
	private void DrawRoot(ImGui gui)
	{
		bool idle = Plugin.Instance.Services.RunOverlay.ActiveRun == null;

		Spacer(gui, (gui.GetLayoutHeight() - GridHeight(gui) - FooterHeight(gui)) * 0.5f);

		ImRect first = TileRow(gui);

		if (UiWidgets.Card(gui, Tile(gui, first, 0), "Quickstart",
			    "Classic ATH, on the settings it has always had.", Color.Style.Action.Resume, idle))
		{
			Quickstart();
		}

		if (UiWidgets.Card(gui, Tile(gui, first, 1), "Play",
			    "Pick the gamemode and what the run is worth first.", Color.Style.Action.Skip, idle))
		{
			_page = AthMenuPage.Play;
		}

		ImRect second = TileRow(gui);

		if (UiWidgets.Card(gui, Tile(gui, second, 0), "Challenge History",
			    "Every hunt this machine has recorded.", Color.Style.Action.Restart, true))
		{
			OpenHistory();
		}

		if (UiWidgets.Card(gui, Tile(gui, second, 1), "Status",
			    "Whether the backends are up, and where this level came from.", Color.Style.Action.Restart, true))
		{
			Plugin.Instance.Services.Status.Visible = true;
		}

		ImRect third = TileRow(gui);

		if (UiWidgets.Card(gui, Tile(gui, third, 0), "Settings",
			    "What the mod draws and how loud it is.", Color.Style.Action.Restart, true))
		{
			_page = AthMenuPage.Settings;
		}

		if (UiWidgets.Card(gui, Tile(gui, third, 1), "Quit",
			    "Puts every ATH window away, this one included.", Color.Style.Action.Stop, true))
		{
			Plugin.Instance.Services.HideUi();
		}

		DrawFooter(gui);
	}

	private static void DrawFooter(ImGui gui)
	{
		Spacer(gui, gui.GetLayoutHeight() - FooterHeight(gui));

		UiText.Centre(gui, Footer, Color.Style.Text.Muted, UiMetrics.Row(gui, 1.4f), gui.Style.Layout.TextSize * 0.9f);
	}

	private void DrawPlay(ImGui gui)
	{
		DrawGamemode(gui);
		DrawRunSettings(gui);

		if (UiWidgets.IconButton(gui, UiMetrics.ButtonRow(gui), UiIcon.Play, "Start Hunt", Color.Style.Action.Resume,
			    Plugin.Instance.Services.RunOverlay.ActiveRun == null))
		{
			Start();
		}
	}

	private static void DrawGamemode(ImGui gui)
	{
		GamemodeRegistry registry = Plugin.Instance.Services.Gamemodes;

		UiWidgets.Heading(gui, UiMetrics.Row(gui, 0.85f), "GAMEMODE");

		int selected = SelectedIndex(registry);

		if (gui.Dropdown(ref selected, registry.DisplayNames, (gui.GetLayoutWidth(), UiMetrics.ButtonHeight(gui))))
		{
			registry.Selected = registry.All[selected];
		}

		UiText.Paragraph(gui, registry.Selected.Description, Color.Style.Text.Muted);
		gui.AddSpacing();
	}

	private static void DrawRunSettings(ImGui gui)
	{
		PluginConfig config = Plugin.Instance.MyConfig;

		UiWidgets.Heading(gui, UiMetrics.Row(gui, 0.85f), "RULES OF THE RUN");

		config.Duration.Value = Minutes(gui, "Duration", config.Duration.Value, 5) * 60;
		config.PenaltyTime.Value = Minutes(gui, "Skip Penalty", config.PenaltyTime.Value, 1) * 60;

		Toggle(gui, "Random workshop levels", config.RandomPlaylist);

		UiText.Paragraph(gui,
			"Later gamemodes bring their own rules here - Classic is the only one that reads yours.",
			Color.Style.Text.Muted);

		gui.AddSpacing();
	}

	private void DrawHistory(ImGui gui)
	{
		if (_history.Count == 0)
		{
			UiText.Left(gui, "No runs recorded yet.", Color.Style.Text.Muted, UiMetrics.Row(gui, 1f));

			return;
		}

		HistoryTable.Header(gui);
		DrawHistoryRows(gui);
	}

	private void DrawHistoryRows(ImGui gui)
	{
		foreach (HistoryRow record in _history)
		{
			if (!HistoryTable.Row(gui, record))
			{
				continue;
			}

			Plugin.Instance.Services.Results.Show(RunReportView.FromRecord(record.Record));
		}
	}

	private static void DrawSettings(ImGui gui)
	{
		PluginConfig config = Plugin.Instance.MyConfig;

		DrawWindowToggles(gui);

		UiWidgets.Heading(gui, UiMetrics.Row(gui, 0.85f), "WHAT ATH DRAWS");
		Toggle(gui, "Run HUD as a window", config.InGameHud);
		Toggle(gui, "Start lights", config.StartLights);
		Toggle(gui, "Medals in the leaderboard", config.LeaderboardMedals);

		gui.AddSpacing();
		UiWidgets.Heading(gui, UiMetrics.Row(gui, 0.85f), "THE REST");
		Toggle(gui, "Less chat text", config.Minimalist);
		Toggle(gui, "Save the playlist when a run ends", config.SavePlaylistOnRunEnd);

		gui.AddSpacing();
		DrawHelpButtons(gui);
	}

	/// <summary>
	///     The windows used to be one checkbox each in the top bar. The top bar is a single
	///     switch now, so this is where they moved - same list, one screen further in.
	/// </summary>
	private static void DrawWindowToggles(ImGui gui)
	{
		ModServices services = Plugin.Instance.Services;

		UiWidgets.Heading(gui, UiMetrics.Row(gui, 0.85f), "WINDOWS");
		services.RunOverlay.Visible = Window(gui, "Run bar", services.RunOverlay.Visible);
		services.LevelCard.Visible = Window(gui, "Level card", services.LevelCard.Visible);
		services.Leaderboard.Visible = Window(gui, "Leaderboard", services.Leaderboard.Visible);
		services.Status.Visible = Window(gui, "Status", services.Status.Visible);

		gui.AddSpacing();
	}

	/// <returns>What the checkbox says now, which is what it said before unless it was clicked.</returns>
	private static bool Window(ImGui gui, string label, bool visible)
	{
		gui.Checkbox(ref visible, label.AsSpan(), UiMetrics.Row(gui, 1.1f));

		return visible;
	}

	private static void DrawHelpButtons(ImGui gui)
	{
		ImRect row = UiMetrics.ButtonRow(gui);

		if (UiWidgets.IconButton(gui, UiWidgets.Column(gui, row, 0, 2), UiIcon.Info, "What is ATH?",
			    Color.Style.Action.Restart))
		{
			Plugin.Instance.Services.Welcome.Visible = true;
		}

		if (UiWidgets.IconButton(gui, UiWidgets.Column(gui, row, 1, 2), UiIcon.Info, "How do I use it?",
			    Color.Style.Action.Restart))
		{
			Plugin.Instance.Services.Help.Visible = true;
		}
	}

	private void OpenHistory()
	{
		_history = RunReportView.HistoryOnly(ZeepkistNetwork.LocalPlayer?.Username,
			Plugin.Instance.Services.History.Records).History;

		_page = AthMenuPage.History;
	}

	private void Quickstart()
	{
		GamemodeRegistry registry = Plugin.Instance.Services.Gamemodes;
		IGamemode classic = registry.Resolve(ClassicId);

		if (classic != null)
		{
			registry.Selected = classic;
		}

		Start();
	}

	private void Start()
	{
		Visible = false;
		AthRequests.Start();
	}

	private bool Back(ImGui gui, ImRect band)
	{
		if (_page == AthMenuPage.Root)
		{
			return false;
		}

		float height = UiMetrics.ButtonHeight(gui);
		ImRect rect = new(band.X, band.Y + (band.H - height) * 0.5f, height * 3.6f, height);

		if (!UiWidgets.IconButton(gui, rect, UiIcon.Restart, "Back", Color.Style.Action.Restart))
		{
			return false;
		}

		_page = AthMenuPage.Root;

		return true;
	}

	private static void Toggle(ImGui gui, string label, ConfigEntry<bool> entry)
	{
		bool value = entry.Value;

		if (gui.Checkbox(ref value, label.AsSpan(), UiMetrics.Row(gui, 1.1f)))
		{
			entry.Value = value;
		}
	}

	private static int Minutes(ImGui gui, string label, int seconds, int step)
	{
		ImRect row = UiMetrics.ButtonRow(gui);
		ImRect labelRect = row.TakeLeft(UiMetrics.LabelWidth(row.W), out ImRect valueRect);

		UiText.Left(gui, $"{label} (min)", Color.Style.Text.Muted, labelRect);

		int minutes = Mathf.Clamp(seconds / 60, MinMinutes, MaxMinutes);
		ImNumericEdit.NumericEdit(gui, ref minutes, valueRect, "0".AsSpan(), step, MinMinutes, MaxMinutes);

		return minutes;
	}

	private static int SelectedIndex(GamemodeRegistry registry)
	{
		for (int i = 0; i < registry.All.Count; i++)
		{
			if (registry.All[i] == registry.Selected)
			{
				return i;
			}
		}

		return 0;
	}

	private static ImRect Tile(ImGui gui, ImRect row, int column)
	{
		return UiWidgets.Column(gui, row, column, TileColumns);
	}

	private static ImRect TileRow(ImGui gui)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), TileHeight(gui));
	}

	private static float TileHeight(ImGui gui)
	{
		return gui.GetRowHeight() * TileRows;
	}

	private static float GridHeight(ImGui gui)
	{
		return (TileHeight(gui) + gui.Style.Layout.Spacing) * TileRowCount;
	}

	private static float FooterHeight(ImGui gui)
	{
		return gui.GetRowHeight() * 2.4f;
	}

	private static void Spacer(ImGui gui, float height)
	{
		if (height <= 0f)
		{
			return;
		}

		gui.AddLayoutRect(gui.GetLayoutWidth(), height);
	}
}
