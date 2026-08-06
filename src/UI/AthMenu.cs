using System;
using System.Collections.Generic;
using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.Util;
using BepInEx.Configuration;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepkistClient;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     The mod's front door: what /ath and the top bar open, and the only screen a player has
///     to know about.
///     Everything ATH can do used to be a chat command or a window somebody had to find -
///     starting a hunt, looking up an old one, changing what the run is worth. This is the one
///     place all of it is listed, and it is a menu rather than a panel because a menu is what a
///     game puts up before a round, not while one is being played.
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

	private const string ClassicId = "classic";

	private const float WidthFraction = 0.3f;
	private const float HeightFraction = 0.62f;
	private const float MinWidth = 400f;
	private const float MinHeight = 440f;

	private const float TitleSize = 1.8f;

	private const int MinMinutes = 1;
	private const int MaxMinutes = 240;

	private const ImWindowFlag WindowFlags = ImWindowFlag.NoResizing;

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
			using (UiScale.Push(gui))
			{
				Draw(gui);
			}
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
			DrawHeadline(gui);
			DrawPage(gui);
		}
		finally
		{
			gui.EndWindow();
		}

		if (!open)
		{
			Visible = false;
		}
	}

	private void DrawHeadline(ImGui gui)
	{
		float text = gui.Style.Layout.TextSize;

		UiText.Centre(gui, "AUTHOR TIME HUNTING", Color.Style.Surface.White, Row(gui, TitleSize * 1.2f),
			text * TitleSize);
		UiText.Centre(gui, Subtitle(), Color.Zeepkist.Medal.Author, Row(gui, 1.2f), text);

		gui.AddSpacing();
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

	private void DrawRoot(ImGui gui)
	{
		bool idle = Plugin.Instance.Services.Control.ActiveRun == null;

		if (UiWidgets.IconButton(gui, ButtonRow(gui), UiIcon.Play, "Quickstart", Color.Style.Action.Resume, idle))
		{
			Quickstart();
		}

		Caption(gui, "Classic ATH, on the settings it has always had.");

		if (UiWidgets.IconButton(gui, ButtonRow(gui), UiIcon.Skip, "Play", Color.Style.Action.Skip, idle))
		{
			_page = AthMenuPage.Play;
		}

		Caption(gui, "Pick the gamemode and what the run is worth first.");

		if (UiWidgets.IconButton(gui, ButtonRow(gui), UiIcon.Info, "Challenge History", Color.Style.Action.Restart))
		{
			OpenHistory();
		}

		Caption(gui, "Every hunt this machine has recorded.");

		if (UiWidgets.IconButton(gui, ButtonRow(gui), UiIcon.Info, "Status", Color.Style.Action.Restart))
		{
			Plugin.Instance.Services.Status.Visible = true;
		}

		Caption(gui, "Whether the backends are up, and where this level came from.");

		if (UiWidgets.IconButton(gui, ButtonRow(gui), UiIcon.Restart, "Settings", Color.Style.Action.Restart))
		{
			_page = AthMenuPage.Settings;
		}

		Caption(gui, "What the mod draws and how loud it is.");

		if (UiWidgets.IconButton(gui, ButtonRow(gui), UiIcon.Stop, "Quit", Color.Style.Action.Stop))
		{
			Plugin.Instance.Services.HideUi();
		}

		Caption(gui, "Puts every ATH window away. /ath brings this one back.");
	}

	private void DrawPlay(ImGui gui)
	{
		if (Back(gui))
		{
			return;
		}

		DrawGamemode(gui);
		DrawRunSettings(gui);

		if (UiWidgets.IconButton(gui, ButtonRow(gui), UiIcon.Play, "Start Hunt", Color.Style.Action.Resume,
			    Plugin.Instance.Services.Control.ActiveRun == null))
		{
			Start();
		}
	}

	private static void DrawGamemode(ImGui gui)
	{
		GamemodeRegistry registry = Plugin.Instance.Services.Gamemodes;

		UiWidgets.Heading(gui, Row(gui, 0.85f), "GAMEMODE");

		int selected = SelectedIndex(registry);

		if (gui.Dropdown(ref selected, Names(registry), (gui.GetLayoutWidth(), UiMetrics.ButtonHeight(gui))))
		{
			registry.Selected = registry.All[selected];
		}

		UiText.Paragraph(gui, registry.Selected.Description, Color.Style.Text.Muted);
		gui.AddSpacing();
	}

	private static void DrawRunSettings(ImGui gui)
	{
		PluginConfig config = Plugin.Instance.MyConfig;

		UiWidgets.Heading(gui, Row(gui, 0.85f), "RULES OF THE RUN");

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
		if (Back(gui))
		{
			return;
		}

		if (_history.Count == 0)
		{
			UiText.Left(gui, "No runs recorded yet.", Color.Style.Text.Muted, Row(gui, 1f));
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

	private void DrawSettings(ImGui gui)
	{
		if (Back(gui))
		{
			return;
		}

		PluginConfig config = Plugin.Instance.MyConfig;

		DrawWindowToggles(gui);

		UiWidgets.Heading(gui, Row(gui, 0.85f), "WHAT ATH DRAWS");
		Toggle(gui, "Run HUD as a window", config.InGameHud);
		Toggle(gui, "Start lights", config.StartLights);
		Toggle(gui, "Medals in the leaderboard", config.LeaderboardMedals);

		gui.AddSpacing();
		UiWidgets.Heading(gui, Row(gui, 0.85f), "THE REST");
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

		UiWidgets.Heading(gui, Row(gui, 0.85f), "WINDOWS");
		Window(gui, "Run HUD", () => services.RunOverlay.Visible, value => services.RunOverlay.Visible = value);
		Window(gui, "Current Level", () => services.LevelStats.Visible, value => services.LevelStats.Visible = value);
		Window(gui, "Controls", () => services.Control.Visible, value => services.Control.Visible = value);
		Window(gui, "Leaderboard", () => services.Leaderboard.Visible, value => services.Leaderboard.Visible = value);
		Window(gui, "Status", () => services.Status.Visible, value => services.Status.Visible = value);

		gui.AddSpacing();
	}

	private static void Window(ImGui gui, string label, Func<bool> get, Action<bool> set)
	{
		bool value = get();

		if (gui.Checkbox(ref value, label.AsSpan(), Row(gui, 1.1f)))
		{
			set(value);
		}
	}

	private static void DrawHelpButtons(ImGui gui)
	{
		ImRect row = ButtonRow(gui);

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

	private bool Back(ImGui gui)
	{
		if (!UiWidgets.IconButton(gui, ButtonRow(gui), UiIcon.Restart, "Back", Color.Style.Action.Restart))
		{
			return false;
		}

		_page = AthMenuPage.Root;

		return true;
	}

	private static void Toggle(ImGui gui, string label, ConfigEntry<bool> entry)
	{
		bool value = entry.Value;

		if (gui.Checkbox(ref value, label.AsSpan(), Row(gui, 1.1f)))
		{
			entry.Value = value;
		}
	}

	private static int Minutes(ImGui gui, string label, int seconds, int step)
	{
		ImRect row = ButtonRow(gui);
		ImRect labelRect = row.TakeLeft(UiMetrics.LabelWidth(row.W), out ImRect valueRect);

		UiText.Left(gui, $"{label} (min)", Color.Style.Text.Muted, labelRect);

		int minutes = Mathf.Clamp(seconds / 60, MinMinutes, MaxMinutes);
		ImNumericEdit.NumericEdit(gui, ref minutes, valueRect, "0".AsSpan(), step, MinMinutes, MaxMinutes);

		return minutes;
	}

	private static void Caption(ImGui gui, string text)
	{
		UiText.Draw(gui, text, Color.Style.Text.Muted, Row(gui, 0.9f), gui.Style.Layout.TextSize * 0.85f, 0f);
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

	private static string[] Names(GamemodeRegistry registry)
	{
		string[] names = new string[registry.All.Count];

		for (int i = 0; i < names.Length; i++)
		{
			names[i] = registry.All[i].DisplayName;
		}

		return names;
	}

	private static ImRect Row(ImGui gui, float scale)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), gui.GetRowHeight() * scale);
	}

	private static ImRect ButtonRow(ImGui gui)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), UiMetrics.ButtonHeight(gui));
	}
}
