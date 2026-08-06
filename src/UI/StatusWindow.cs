using System;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     Whether the things a hunt depends on are actually there: the two backends, and where
///     the level under the wheels came from.
///     Both questions used to be answerable only by reading the log after the fact. A run that
///     silently fell back to local playlists looks exactly like one that did not, and a
///     backend that went down mid-run looks like a mod that broke. This says which it is.
/// </summary>
public class StatusWindow : IZeepGUIDrawer
{
	private const string WindowTitle = "ATH Status";

	private const float WidthFraction = 0.2f;

	private const float MinWidth = 280f;
	private const float MaxWidth = 400f;

	private const string LobbySource = "Lobby playlist";

	private const ImWindowFlag WindowFlags = ImWindowFlag.NoResizing;

	private float _contentHeight;

	private bool _mouseOverWindow;

	public AthStateMachine ActiveRun { get; set; }

	public bool Visible
	{
		get;
		set
		{
			bool wasVisible = field;
			field = value;

			if (value && !wasVisible)
			{
				Plugin.Instance.Services.Health.Refresh();
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
			Logger.LogError($"StatusWindow: Draw failed, closing it: {e.Message}\n{e.StackTrace}");
			Visible = false;
		}
	}

	public void Toggle()
	{
		Visible = !Visible;
	}

	private void Draw(ImGui gui)
	{
		float width = UiMetrics.Width(gui, WidthFraction, MinWidth, MaxWidth);

		ImRect rect = ImWindowPlacement.PlaceAutoSized(gui, WindowTitle.AsSpan(), width, Height(gui),
			ImWindowAnchor.BottomLeft);

		bool open = true;

		if (!gui.BeginWindow(WindowTitle, ref open, ref _mouseOverWindow, rect, WindowFlags))
		{
			return;
		}

		try
		{
			DrawBackends(gui);
			DrawLevel(gui);

			_contentHeight = UiMetrics.ContentHeight(gui);
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

	private static void DrawBackends(ImGui gui)
	{
		HealthService health = Plugin.Instance.Services.Health;

		UiWidgets.Heading(gui, Row(gui, 0.85f), "BACKENDS");

		DrawHealth(gui, health.GraphQl);
		DrawHealth(gui, health.Gtr);

		if (UiWidgets.Button(gui, ButtonRow(gui), health.IsChecking ? "Checking..." : "Check again"))
		{
			health.Refresh();
		}

		gui.AddSpacing();
	}

	private static void DrawHealth(ImGui gui, HealthStatus status)
	{
		ImRect row = Row(gui, 1.2f);

		float dotSize = row.H;
		ImRect dot = row.TakeLeft(dotSize, gui.Style.Layout.InnerSpacing, out ImRect rest);

		gui.Canvas.Circle(dot.Center, dotSize * 0.28f, DotColour(status));
		UiWidgets.Row(gui, rest, status.Name, Verdict(status), DotColour(status));

		UiText.Draw(gui, status.Detail ?? "", Color.Style.Text.Muted, Row(gui, 0.9f),
			gui.Style.Layout.TextSize * 0.85f, 0f);
	}

	private static Color32 DotColour(HealthStatus status)
	{
		if (!status.Checked)
		{
			return Color.Style.Text.Muted;
		}

		return status.IsUp ? Color.Style.Status.Good : Color.Style.Status.Danger;
	}

	private static string Verdict(HealthStatus status)
	{
		if (!status.Checked)
		{
			return "not checked yet";
		}

		return status.IsUp ? $"up ({status.LatencyMs} ms)" : "down";
	}

	private void DrawLevel(ImGui gui)
	{
		UiWidgets.Heading(gui, Row(gui, 0.85f), "CURRENT LEVEL");

		AthStateMachine run = ActiveRun;
		Level level = run?.Ctx.CurrentLevel;

		if (level == null)
		{
			UiText.Left(gui, "No hunt running.", Color.Style.Text.Muted, Row(gui, 1f));

			return;
		}

		string source = run.RandomLevels.SourceOf(level.LevelUid) ?? LobbySource;

		UiWidgets.Row(gui, Row(gui, 1f), "Source", source, SourceColour(source));
		UiWidgets.Row(gui, Row(gui, 1f), "Level", level.Name, Color.Style.Text.LevelName);
		UiWidgets.Row(gui, Row(gui, 1f), "Author", level.Author, Color.Style.Text.AuthorName);
	}

	private static Color32 SourceColour(string source)
	{
		return source == LobbySource ? Color.Style.Text.Muted : Color.Zeepkist.Medal.Author;
	}

	private static ImRect Row(ImGui gui, float scale)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), gui.GetRowHeight() * scale);
	}

	private static ImRect ButtonRow(ImGui gui)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), UiMetrics.ButtonHeight(gui));
	}

	private float Height(ImGui gui)
	{
		float content = _contentHeight > 0f ? _contentHeight : gui.GetRowHeight() * 14f;

		return content + UiMetrics.WindowChrome(gui) + UiMetrics.Slack(gui);
	}
}
