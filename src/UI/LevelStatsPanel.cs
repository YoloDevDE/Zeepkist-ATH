using System;
using AuthorTimeHunting.Run;
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

	/// <summary>Share of the screen width, before the clamp below.</summary>
	private const float WidthFraction = 0.21f;

	private const float MinWidth = 300f;
	private const float MaxWidth = 420f;

	private const float TitleSize = 1.25f;
	private const float MedalRowSize = 1.4f;

	private const ImWindowFlag WindowFlags = ImWindowFlag.NoCloseButton | ImWindowFlag.NoResizing;

	/// <summary>Height the content came to last frame, or 0 before the first one.</summary>
	private float _contentHeight;

	private bool _mouseOverWindow;

	/// <summary>The run currently in progress, or null when ATH is idle. Set by AthMod.</summary>
	public AthRunner ActiveRun { get; set; }

	/// <summary>
	///     Toggled by /ath together with the control panel. There is nothing to show without a
	///     level, so an idle hunt hides this rather than drawing an empty frame.
	/// </summary>
	public bool Visible { get; set; }

	public void OnZeepGUI(ImGui gui)
	{
		AthRunner run = ActiveRun;

		if (!Visible || run == null)
		{
			return;
		}

		LevelStatsView view = LevelStatsView.From(run.Ctx);

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
			// Inside the game's shared GUI pass - a throwing drawer would throw every frame.
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

			// While the window's layout frame is still open, so it can report what it holds.
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

		UiText.Draw(gui, view.Name, HudPalette.LevelName, Row(gui, TitleSize * 1.2f), text * TitleSize, 0f);
		UiText.Draw(gui, $"by {view.Author}", HudPalette.AuthorName, Row(gui, 0.9f), text * 0.85f, 0f);
		gui.AddSpacing();
	}

	/// <summary>
	///     The two times that matter, with the game's own medals beside them, and the best we
	///     have managed so far measured against the author time.
	/// </summary>
	private static void DrawTargets(ImGui gui, LevelStatsView view)
	{
		DrawMedalTime(gui, GameSprites.AuthorMedal, "Author", view.AuthorTime, HudPalette.Author);
		DrawMedalTime(gui, GameSprites.GoldMedal, "Gold", view.GoldTime, HudPalette.Gold);

		if (view.PersonalBest == null)
		{
			UiWidgets.Row(gui, Row(gui, 1f), "Your Best", "not finished yet", HudPalette.Muted);
			gui.AddSpacing();
			return;
		}

		UiWidgets.Row(gui, Row(gui, 1f), "Your Best", $"{view.PersonalBest}   ({view.AuthorDelta})",
			view.PersonalBestColour);
		gui.AddSpacing();
	}

	private static void DrawMedalTime(ImGui gui, Sprite sprite, string label, string time, Color32 colour)
	{
		ImRect row = Row(gui, MedalRowSize);
		float iconSize = row.H;

		ImRect icon = row.TakeLeft(iconSize, gui.Style.Layout.InnerSpacing, out ImRect rest);

		if (sprite != null)
		{
			gui.Image(sprite, icon, true);
		}
		else
		{
			gui.Canvas.Circle(icon.Center, iconSize * 0.3f, colour);
		}

		UiWidgets.Row(gui, rest, label, time, colour);
	}

	/// <summary>What the level has cost so far. The numbers that feed the traffic light.</summary>
	private static void DrawEffort(ImGui gui, LevelStatsView view)
	{
		UiWidgets.Heading(gui, Row(gui, 0.85f), "EFFORT");

		UiWidgets.Row(gui, Row(gui, 1f), "Attempts", view.Attempts, HudPalette.Default);
		UiWidgets.Row(gui, Row(gui, 1f), "Crashes", view.Crashes, HudPalette.Default);
		UiWidgets.Row(gui, Row(gui, 1f), "Wheels Lost", view.WheelsLost, HudPalette.Default);
		UiWidgets.Row(gui, Row(gui, 1f), "Time Here", view.TimeOnLevel, HudPalette.Default);
		gui.AddSpacing();
	}

	/// <summary>
	///     The traffic light. There is no absolute answer to "am I doing badly here" - a two
	///     minute level is not a bad level - so it is measured against the levels this run has
	///     already beaten, and stays grey until there are enough of them to mean anything.
	/// </summary>
	private static void DrawPace(ImGui gui, LevelStatsView view)
	{
		ImRect row = Row(gui, 1.2f);
		Color32 colour = LevelStatsView.PaceColour(view.Pace);

		float dotSize = row.H;
		ImRect dot = row.TakeLeft(dotSize, gui.Style.Layout.InnerSpacing, out ImRect rest);

		gui.Canvas.Circle(dot.Center, dotSize * 0.28f, colour);
		UiText.Left(gui, LevelStatsView.PaceLabel(view.Pace), colour, rest);
	}

	/// <summary>A layout row <paramref name="scale" /> times the theme's text size tall.</summary>
	private static ImRect Row(ImGui gui, float scale)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), gui.GetRowHeight() * scale);
	}

	/// <summary>
	///     Last frame's content plus the window's own chrome. See <see cref="UiMetrics.ContentHeight" />
	///     for why this is measured rather than counted.
	/// </summary>
	private float Height(ImGui gui)
	{
		float content = _contentHeight > 0f ? _contentHeight : gui.GetRowHeight() * 14f;

		return content + UiMetrics.WindowChrome(gui) + UiMetrics.Slack(gui);
	}
}