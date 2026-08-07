using System;
using System.Threading.Tasks;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepkistClient;
using ZeepkistNetworking;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;
using Random = UnityEngine.Random;

namespace AuthorTimeHunting.UI.Screens;

/// <summary>
///     The developer panel: what the run and the level pool actually think is going on, and the
///     shortcuts that make testing a change survivable.
///     ATH is close to untestable by playing it - a single run is an hour, and half the paths
///     only open on a medal that takes twenty attempts to earn. So the panel hands them out.
///     Off by default and opened from the top bar's own "ATH Debug" entry, because a button that
///     awards an author time is the last thing a real hunt needs within reach.
/// </summary>
public class DebugPanel : IZeepGUIDrawer
{
	private const string WindowTitle = "ATH Debug";

	private const float WidthFraction = 0.23f;
	private const float MinWidth = 345f;
	private const float MaxWidth = 495f;

	private const ImWindowFlag WindowFlags = ImWindowFlag.NoCloseButton | ImWindowFlag.NoResizing;

	private const float Spread = 1.5f;

	private float _contentHeight;
	private bool _mouseOverWindow;

	public AthStateMachine ActiveRun { get; set; }

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
			Logger.LogError($"DebugPanel: Draw failed, hiding the panel: {e.Message}\n{e.StackTrace}");
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
			ImWindowAnchor.MiddleLeft);

		bool open = true;

		if (!gui.BeginWindow(WindowTitle, ref open, ref _mouseOverWindow, rect, WindowFlags))
		{
			return;
		}

		try
		{
			AthStateMachine run = ActiveRun;

			DrawGameState(gui);
			DrawRunState(gui, run);
			DrawLevelPool(gui, run);
			DrawActions(gui, run);

			_contentHeight = UiMetrics.ContentHeight(gui);
		}
		finally
		{
			gui.EndWindow();
		}
	}

	private static void DrawGameState(ImGui gui)
	{
		UiWidgets.Heading(gui, UiMetrics.Row(gui, 0.85f), "GAME");

		Line(gui, "Lobby", GameStateObserver.LobbyState.ToString());
		Line(gui, "Online", GameStateObserver.IsInOnlineLobby.ToString());
		Line(gui, "Racing", GameStateObserver.IsRacing.ToString());
		Line(gui, "Level ready", GameStateObserver.IsLevelReady.ToString());
		Line(gui, "Playlist", PlaylistPosition());
		gui.AddSpacing();
	}

	private static string PlaylistPosition()
	{
		ZeepkistLobby lobby = ZeepkistNetwork.CurrentLobby;

		if (lobby?.Playlist == null)
		{
			return "no lobby";
		}

		return $"{lobby.CurrentPlaylistIndex + 1} / {lobby.Playlist.Count}";
	}

	private static void DrawRunState(ImGui gui, AthStateMachine run)
	{
		UiWidgets.Heading(gui, UiMetrics.Row(gui, 0.85f), "RUN");

		if (run == null)
		{
			Line(gui, "State", "idle");
			gui.AddSpacing();
			return;
		}

		AthCtx ctx = run.Ctx;

		Line(gui, "State", run.CurrentState?.GetType().Name ?? "none");
		Line(gui, "Gamemode", run.Gamemode.DisplayName);
		Line(gui, "Paused", ctx.IsPaused.ToString());
		Line(gui, "Clock", ctx.CurrentLevel is { IsTiming: true } ? "running" : "stopped");
		Line(gui, "Remaining", TimeFormatter.FormatDuration((int)ctx.GetRemainingTime().TotalMilliseconds));
		Line(gui, "Levels", UiNumbers.Text(ctx.Levels.Count));
		Line(gui, "AT/Gold/Pen", $"{ctx.AuthorMedals} / {ctx.GoldMedals} / {ctx.Penalties}");
		Line(gui, "Free skips", UiNumbers.Text(ctx.AvaiableFreeSkips));
		Line(gui, "Duplicates", UiNumbers.Text(ctx.ConsecutiveDuplicateCount));
		Line(gui, "Broken in a row", UiNumbers.Text(ctx.ConsecutiveBrokenCount));
		gui.AddSpacing();
	}

	private static void DrawLevelPool(ImGui gui, AthStateMachine run)
	{
		UiWidgets.Heading(gui, UiMetrics.Row(gui, 0.85f), "LEVEL POOL");

		if (run == null)
		{
			Line(gui, "Pool", "no run");
			gui.AddSpacing();
			return;
		}

		RandomLevelService pool = run.RandomLevels;

		Line(gui, "This level", pool.SourceOf(run.Ctx.CurrentLevel?.LevelUid) ?? "lobby playlist");
		Line(gui, "Last batch", pool.LastSource ?? "not fetched yet");
		Line(gui, "Cached", UiNumbers.Text(pool.CachedCount));
		Line(gui, "Drawn", UiNumbers.Text(pool.PlayedCount));
		Line(gui, "Seen", UiNumbers.Text(pool.FetchedCount));
		Line(gui, "Last", pool.LastDrawn ?? "none");

		if (UiWidgets.Button(gui, UiMetrics.ButtonRow(gui), "Fetch a level"))
		{
			_ = TestDrawAsync(pool);
		}

		gui.AddSpacing();
	}

	private static async Task TestDrawAsync(RandomLevelService pool)
	{
		try
		{
			OnlineZeeplevel level = await pool.DrawRandomLevelAsync();
			FrogNotification.Success($"Drew '{level.Name}' by {level.Author}");
		}
		catch (Exception e)
		{
			FrogNotification.Error($"Draw failed: {e.Message}");
			Logger.LogError($"DebugPanel: Test draw failed: {e.Message}\n{e.StackTrace}");
		}
	}

	private static void DrawActions(ImGui gui, AthStateMachine run)
	{
		UiWidgets.Heading(gui, UiMetrics.Row(gui, 0.85f), "GRANT A TIME");

		if (run?.Ctx.CurrentLevel == null)
		{
			UiText.Left(gui, "No level loaded.", Color.Style.Text.Muted, UiMetrics.Row(gui, 1f));
			return;
		}

		Level level = run.Ctx.CurrentLevel;
		ImRect row = UiMetrics.ButtonRow(gui);

		if (UiWidgets.IconButton(gui, UiWidgets.Column(gui, row, 0, 2), UiIcon.None, "Author",
			    Color.Zeepkist.Medal.Author))
		{
			Grant(run, (float)level.AuthorTime - Random.Range(0.05f, Spread));
		}

		if (UiWidgets.IconButton(gui, UiWidgets.Column(gui, row, 1, 2), UiIcon.None, "Gold", Color.Zeepkist.Medal.Gold))
		{
			Grant(run, (float)level.GoldTime - Random.Range(0.05f, Spread));
		}

		if (UiWidgets.IconButton(gui, UiMetrics.ButtonRow(gui), UiIcon.None, "Missed", Color.Style.Action.Broken))
		{
			Grant(run, (float)level.GoldTime + Random.Range(0.05f, Spread));
		}

		UiText.Left(gui, "Only improvements stick.", Color.Style.Text.Muted, UiMetrics.Row(gui, 0.9f));
	}

	private static void Grant(AthStateMachine run, float time)
	{
		Level level = run.Ctx.CurrentLevel;
		float clamped = Math.Max(0.001f, time);

		level.PersonalBestTime = clamped;
		run.Ctx.LastRunTime = clamped;
		run.Ctx.LastRunMedalStatus = clamped <= level.AuthorTime ? LevelStatus.AUTHOR :
			clamped <= level.GoldTime ? LevelStatus.GOLD : LevelStatus.UNKNOWN;

		FrogNotification.Info($"Granted {TimeFormatter.FormatTime(clamped)} -> {level.StatusString}");
	}

	private static void Line(ImGui gui, string label, string value)
	{
		UiWidgets.Row(gui, UiMetrics.Row(gui, 1f), label, value, Color.Style.Text.Default);
	}

	private float Height(ImGui gui)
	{
		return UiMetrics.WindowHeight(gui, _contentHeight, 22f);
	}
}
