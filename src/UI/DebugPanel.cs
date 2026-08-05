using System;
using System.Threading.Tasks;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using ZeepkistClient;
using ZeepkistNetworking;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;
using Random = UnityEngine.Random;

namespace AuthorTimeHunting.UI;

/// <summary>
///     The developer panel: what the run and the level pool actually think is going on, and the
///     shortcuts that make testing a change survivable.
///     ATH is close to untestable by playing it - a single run is an hour, and half the paths
///     only open on a medal that takes twenty attempts to earn. So the panel hands them out.
///     Off by default and opened with /athdebug, because a button that awards an author time is
///     the last thing a real hunt needs within reach.
/// </summary>
public class DebugPanel : IZeepGUIDrawer
{
	private const string WindowTitle = "ATH Debug";

	private const float WidthFraction = 0.16f;
	private const float MinWidth = 230f;
	private const float MaxWidth = 330f;

	private const ImWindowFlag WindowFlags = ImWindowFlag.NoCloseButton | ImWindowFlag.NoResizing;

	/// <summary>
	///     How far off the target a granted time lands, in seconds. Never exactly on it: a run
	///     that is always decided by a hair is not the run the medal logic has to survive.
	/// </summary>
	private const float Spread = 1.5f;

	private float _contentHeight;
	private bool _mouseOverWindow;

	/// <summary>The run currently in progress, or null when ATH is idle. Set by StateMasterOn.</summary>
	public AthStateMachine ActiveRun { get; set; }

	/// <summary>Toggled by /athdebug. Starts hidden and is never shown on its own.</summary>
	public bool Visible { get; set; }

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
			// Inside the game's shared GUI pass - a throwing drawer would throw every frame.
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

	/// <summary>Where the lobby thinks it is - the inputs every start decision is made on.</summary>
	private static void DrawGameState(ImGui gui)
	{
		UiWidgets.Heading(gui, Row(gui, 0.85f), "GAME");

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

	/// <summary>
	///     Which state the run is in, and whether its clock is actually moving. The second one is
	///     the question worth a panel of its own: a stopped clock looks exactly like a running
	///     one until a minute has gone by.
	/// </summary>
	private static void DrawRunState(ImGui gui, AthStateMachine run)
	{
		UiWidgets.Heading(gui, Row(gui, 0.85f), "RUN");

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

	/// <summary>
	///     Whether level sourcing is working, which is otherwise only visible in the log and
	///     only after it has already gone wrong.
	/// </summary>
	private static void DrawLevelPool(ImGui gui, AthStateMachine run)
	{
		UiWidgets.Heading(gui, Row(gui, 0.85f), "LEVEL POOL");

		if (run == null)
		{
			Line(gui, "Pool", "no run");
			gui.AddSpacing();
			return;
		}

		RandomLevelService pool = run.RandomLevels;

		Line(gui, "Source", pool.LastSource ?? "not fetched yet");
		Line(gui, "Cached", UiNumbers.Text(pool.CachedCount));
		Line(gui, "Drawn", UiNumbers.Text(pool.PlayedCount));
		Line(gui, "Seen", UiNumbers.Text(pool.FetchedCount));
		Line(gui, "Last", pool.LastDrawn ?? "none");

		if (UiWidgets.Button(gui, ButtonRow(gui), "Fetch a level"))
		{
			_ = TestDrawAsync(pool);
		}

		gui.AddSpacing();
	}

	/// <summary>
	///     Draws one level and says what came back, without touching the playlist. This is the
	///     whole of level sourcing - GraphQL, the local fallback, the exclusion set - exercised
	///     from a button instead of from an hour of play.
	/// </summary>
	private static async Task TestDrawAsync(RandomLevelService pool)
	{
		try
		{
			OnlineZeeplevel level = await pool.DrawRandomLevelAsync();
			ToastNotification.Success($"Drew '{level.Name}' by {level.Author}");
		}
		catch (Exception e)
		{
			// Started without awaiting, so nothing above can catch this.
			ToastNotification.Error($"Draw failed: {e.Message}");
			Logger.LogError($"DebugPanel: Test draw failed: {e.Message}\n{e.StackTrace}");
		}
	}

	/// <summary>
	///     The medals, handed out. Times land near the target rather than on it, so the value
	///     that reaches the HUD is a plausible one and the formatting is exercised too.
	/// </summary>
	private static void DrawActions(ImGui gui, AthStateMachine run)
	{
		UiWidgets.Heading(gui, Row(gui, 0.85f), "GRANT A TIME");

		if (run?.Ctx.CurrentLevel == null)
		{
			UiText.Left(gui, "No level loaded.", HudPalette.Muted, Row(gui, 1f));
			return;
		}

		Level level = run.Ctx.CurrentLevel;
		ImRect row = ButtonRow(gui);

		if (UiWidgets.IconButton(gui, UiWidgets.Column(gui, row, 0, 2), UiIcon.None, "Author", HudPalette.Author))
		{
			Grant(run, (float)level.AuthorTime - Random.Range(0.05f, Spread));
		}

		if (UiWidgets.IconButton(gui, UiWidgets.Column(gui, row, 1, 2), UiIcon.None, "Gold", HudPalette.Gold))
		{
			Grant(run, (float)level.GoldTime - Random.Range(0.05f, Spread));
		}

		if (UiWidgets.IconButton(gui, ButtonRow(gui), UiIcon.None, "Missed", HudPalette.ActionBroken))
		{
			Grant(run, (float)level.GoldTime + Random.Range(0.05f, Spread));
		}

		// PersonalBestTime only ever improves - that is the point of a personal best - so a
		// worse time granted after a better one is silently ignored. Said out loud here rather
		// than left to look like a broken button.
		UiText.Left(gui, "Only improvements stick.", HudPalette.Muted, Row(gui, 0.9f));
	}

	/// <summary>
	///     Writes a finish time into the run the same way a real one arrives, so everything
	///     downstream - the medal counters, the skip type, the level panel - sees what it would
	///     have seen. Below the author time it also nudges the clamp: a granted author time on a
	///     level whose gold was already beaten still has to register as an author time.
	/// </summary>
	private static void Grant(AthStateMachine run, float time)
	{
		Level level = run.Ctx.CurrentLevel;
		float clamped = Math.Max(0.001f, time);

		level.PersonalBestTime = clamped;
		run.Ctx.LastRunTime = clamped;
		run.Ctx.LastRunMedalStatus = clamped <= level.AuthorTime ? LevelStatus.AUTHOR :
			clamped <= level.GoldTime ? LevelStatus.GOLD : LevelStatus.UNKNOWN;

		ToastNotification.Info($"Granted {TimeFormatter.FormatTime(clamped)} -> {level.StatusString}");
	}

	private static void Line(ImGui gui, string label, string value)
	{
		UiWidgets.Row(gui, Row(gui, 1f), label, value, HudPalette.Default);
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
		float content = _contentHeight > 0f ? _contentHeight : gui.GetRowHeight() * 22f;

		return content + UiMetrics.WindowChrome(gui) + UiMetrics.Slack(gui);
	}
}
