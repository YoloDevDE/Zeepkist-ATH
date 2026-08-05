using System;
using AuthorTimeHunting.States.Ath.StateMachine;
using Imui.Controls;
using Imui.Core;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     The run itself - clock, budget bar and score - across the top centre of the screen.
///     It used to sit in the corner as part of the control panel, which put the one thing a
///     player reads every few seconds in the one place they are not looking. Centred above the
///     track it is on the way to everything else.
///     Built as an ordinary panel, the same way <see cref="LevelStatsPanel" /> and
///     <see cref="ControlPanel" /> are, down to the title bar. Two cleverer versions came first
///     and neither drew a single character: shapes painted straight onto the canvas outside a
///     window, and then a borderless window with its height counted out by hand. Both put the
///     backdrop and the medal sprites on screen and left the text off it. The recipe that works
///     three times over in this mod is a window with a title bar, rows measured in
///     <c>GetRowHeight</c>, and a height read back from the layout - so this is that, and the
///     title bar is the price of the clock being visible.
/// </summary>
public class RunOverlay : IZeepGUIDrawer
{
	private const string WindowTitle = "Run";

	/// <summary>Share of the screen width, before the clamp below.</summary>
	private const float WidthFraction = 0.2f;

	private const float MinWidth = 240f;
	private const float MaxWidth = 380f;

	// Multiples of the theme's text size, which UiScale has already turned down.
	private const float ClockSize = 2.2f;
	private const float MedalRowSize = 1.5f;
	private const float FooterSize = 0.85f;

	private const ImWindowFlag WindowFlags = ImWindowFlag.NoCloseButton | ImWindowFlag.NoResizing;

	/// <summary>Height the content came to last frame, or 0 before the first one.</summary>
	private float _contentHeight;

	private bool _mouseOverWindow;

	/// <summary>The run currently in progress, or null when ATH is idle. Set by StateMasterOn.</summary>
	public AthStateMachine ActiveRun { get; set; }

	/// <summary>Toggled by /ath along with the panels.</summary>
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
			// Inside the game's shared GUI pass - a throwing drawer would throw every frame.
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
			float text = gui.Style.Layout.TextSize;

			UiText.Centre(gui, view.TimeLeft, view.TimeColour, Row(gui, ClockSize * 1.2f), text * ClockSize);
			UiWidgets.Bar(gui, Row(gui, 0.35f), view.RemainingFraction, view.TimeColour);
			DrawMedals(gui, Row(gui, MedalRowSize), view);
			DrawFooter(gui, Row(gui, 1f), view, text * FooterSize);

			// Only ever present once a penalty has been taken - what the run would still have, and
			// what the skipping has cost. Kept here rather than dropped, because the whole point of
			// showing them is that the cost of a skip does not quietly vanish into the one clock.
			foreach (HudRow detail in view.Details)
			{
				UiWidgets.Row(gui, Row(gui, 1f), detail.Label, detail.Value, detail.ValueColour);
			}

			// While the window's layout frame is still open, so it can report what it holds.
			_contentHeight = UiMetrics.ContentHeight(gui);
		}
		finally
		{
			gui.EndWindow();
		}
	}

	/// <summary>
	///     The score, as the game's own medals. Sprites rather than words because this is the
	///     line a player checks mid-run, and three shapes are read faster than three labels.
	/// </summary>
	private static void DrawMedals(ImGui gui, ImRect row, RunHudView view)
	{
		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, row, 0, 3), GameSprites.AuthorMedal, view.AuthorMedals,
			HudPalette.Author);
		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, row, 1, 3), GameSprites.GoldMedal, view.GoldMedals,
			HudPalette.Gold);
		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, row, 2, 3), GameSprites.YouTriedMedal, view.Penalties,
			HudPalette.Penalty);
	}

	/// <summary>
	///     What the run was given on the left of centre, what a skip costs right of it. Both are
	///     pushed against the middle rather than the edges, so the pair stays a pair on a wide
	///     screen instead of drifting apart into two unrelated notes.
	/// </summary>
	private static void DrawFooter(ImGui gui, ImRect row, RunHudView view, float size)
	{
		float gutter = gui.Style.Layout.InnerSpacing;
		float half = row.W * 0.5f;
		ImRect left = new(row.X, row.Y, half - gutter, row.H);
		ImRect right = new(row.X + half + gutter, row.Y, half - gutter, row.H);

		UiText.Right(gui, $"of {view.Duration}", HudPalette.Muted, left, size);

		if (view.Paused)
		{
			UiText.Draw(gui, "PAUSED", HudPalette.Warning, right, size, 0f);
			return;
		}

		UiText.Draw(gui, view.SkipType, view.SkipColour, right, size, 0f);
	}

	/// <summary>A layout row <paramref name="scale" /> times the theme's row height tall.</summary>
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
		float content = _contentHeight > 0f ? _contentHeight : gui.GetRowHeight() * 8f;

		return content + UiMetrics.WindowChrome(gui) + UiMetrics.Slack(gui);
	}
}
