using System;
using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.UI;
using AuthorTimeHunting.Util;
using UnityEngine;
using ZeepkistClient;
using ZeepkistNetworking;

namespace AuthorTimeHunting.States.Ath;

/// <summary>
///     Builds everything ATH says, as <see cref="PanelView" /> rather than as formatted
///     strings. Reads the run through <see cref="AthCtx" />; never mutates it and never
///     displays anything itself - AthStateMachine decides whether a panel becomes an in-game
///     window or a chat message.
///     Reachable as <see cref="AthCtx.Messages" />, so call sites read Ctx.Messages.End().
/// </summary>
public class RunPresenter
{
	private const string NoTime = "--:--.---";

	/// <summary>Panels about a single moment fade out; the run report stays up.</summary>
	private const float TransientSeconds = 8f;

	private readonly AthCtx _ctx;

	public RunPresenter(AthCtx ctx)
	{
		_ctx = ctx;
	}

	private static bool IsMinimalist => Plugin.Instance.MyConfig.Minimalist.Value;

	private Level CurrentLevel => _ctx.CurrentLevel;

	/// <summary>Welcome panel with the run settings, shown at the start of a run.</summary>
	public PanelView Starting()
	{
		PanelBuilder panel = PanelView.Build("Welcome to Author-Time-Hunting!", HudPalette.AuthorName);

		if (!IsMinimalist)
		{
			panel.Line("Collect Author Medals within the time limit!")
				.Heading("Settings")
				.Row("Duration", TimeSpan.FromMilliseconds(_ctx.Duration).ToFormattedString())
				.Row("Skip Penalty", TimeSpan.FromMilliseconds(_ctx.PenaltyTimeInMilliseconds).ToFormattedString(),
					HudPalette.Bad);
		}

		panel.Heading("Commands")
			.Row("/fs", HudPalette.Command, "Skip level", HudPalette.Default)
			.Row("/ath broken", HudPalette.Command, "Skip unbeatable map", HudPalette.Default);

		if (IsMinimalist)
		{
			panel.Row("/ath restart", HudPalette.Command, "Restart the hunt", HudPalette.Default)
				.Row("/ath stop", HudPalette.Command, "End the hunt", HudPalette.Default);
		}

		return panel.Line("Good luck & have fun!", HudPalette.AuthorName).ShowFor(TransientSeconds).Done();
	}

	/// <summary>Medal targets and the current split, shown when a run on a level starts.</summary>
	public PanelView OnARun()
	{
		Split split = CurrentSplit();

		PanelBuilder panel = PanelView.Build(LevelHeadline(CurrentLevel), HudPalette.LevelName)
			.Heading("Medals")
			.Row("AT", HudPalette.Author, CurrentLevel.AuthorTime.GetFormattedTime(), HudPalette.White);

		panel.Row("Gold", HudPalette.Gold, CurrentLevel.GoldTime.GetFormattedTime(), HudPalette.White);

		if (split.HasResult)
		{
			panel.Row(CurrentLevel.AuthorTimeAcquired ? "AT Beaten by" : "AT Missed by",
				CurrentLevel.AuthorTimeAcquired ? HudPalette.Positive : HudPalette.Negative,
				split.Difference, split.DifferenceColour);
		}

		return panel.ShowFor(TransientSeconds).Done();
	}

	/// <summary>Result and split, shown when the player crosses the finish line.</summary>
	public PanelView CrossedFinishLine()
	{
		Split split = CurrentSplit();

		return PanelView.Build(LevelHeadline(CurrentLevel), HudPalette.LevelName)
			.Heading("Result")
			.Row("AT", HudPalette.Author, CurrentLevel.AuthorTime.GetFormattedTime(), HudPalette.White)
			.Row("Gold", HudPalette.Gold, CurrentLevel.GoldTime.GetFormattedTime(), HudPalette.White)
			.Row("Your Time", split.Result)
			.Row(CurrentLevel.AuthorTimeAcquired ? "Beaten by" : "Missed by",
				CurrentLevel.AuthorTimeAcquired ? HudPalette.Positive : HudPalette.Negative,
				split.Difference, split.DifferenceColour)
			.ShowFor(TransientSeconds)
			.Done();
	}

	/// <summary>
	///     The end-of-run report: medal and attempt counts, time breakdown, plus up to three
	///     narrative sections about individual levels and authors. Stays up until dismissed.
	/// </summary>
	public PanelView End()
	{
		RunStatistics stats = new(_ctx.Levels);

		PanelBuilder panel = PanelView.Build("Authortime Hunt finished! :party:", HudPalette.AuthorName)
			.Heading("Result")
			.Row("Total ATs", _ctx.AuthorMedals.ToString(), HudPalette.Author)
			.Row("ATs Oneshotted!", stats.OneShotAuthorTimes.ToString(), HudPalette.Positive)
			.Row("Total Resets", stats.TotalAttempts.ToString())
			.Row("Total Skips", stats.PenaltySkipCount.ToString())
			.Row("Attempts/AT", stats.AverageAttemptsPerAuthorTime.ToString("F2"))
			.Row("Avg AT", stats.AverageAuthorTime.GetFormattedTime())
			.Row("Time Wasted", stats.TotalTimeWasted.ToFormattedString(), HudPalette.Bad)
			.Row("Time/AT", stats.AverageTimePerAuthorTime.ToFormattedString());

		Level biggestTimeSink = stats.BiggestTimeSink;

		if (biggestTimeSink != null)
		{
			panel.Heading("You should have skipped this :yannics:")
				.Line(LevelHeadline(biggestTimeSink), HudPalette.LevelName)
				.Row("Status", biggestTimeSink.StatusString)
				.Row("PlayDuration", biggestTimeSink.GetPlayDuration().ToFormattedString(), HudPalette.Bad)
				.Row("Attempts", biggestTimeSink.Attempt.ToString());
		}

		Level easiest = stats.EasiestBeatenLevel;

		if (easiest != null)
		{
			panel.Heading("Easiest Level")
				.Line(LevelHeadline(easiest), HudPalette.LevelName)
				.Row("Attempts", easiest.Attempt.ToString())
				.Row("PlayDuration", easiest.GetPlayDuration().ToFormattedString());
		}

		(string Author, List<Level> Levels) haunting = stats.MostBeatenAuthor;

		if (haunting.Levels is { Count: > 0 })
		{
			panel.Heading("This Author haunted you")
				.Line("And their name is...", HudPalette.Default)
				.Line($"'{haunting.Author}' !", HudPalette.AuthorName)
				.Line($"You've beaten {haunting.Levels.Count} of their levels:", HudPalette.Default);

			foreach (Level level in haunting.Levels) panel.Line($"- {level.Name}", HudPalette.LevelName);
		}

		return panel.Done();
	}

	/// <summary>Shown when the gold medal is claimed and the level becomes free to skip.</summary>
	public PanelView GoldMedalClaimed()
	{
		return PanelView.Build(LevelHeadline(CurrentLevel), HudPalette.LevelName)
			.Heading("New Medal Claimed")
			.Row("GOLD", HudPalette.Gold, ResultTime(), HudPalette.White)
			.Line("You can now skip this level without a time penalty!", HudPalette.Info)
			.ShowFor(TransientSeconds)
			.Done();
	}

	/// <summary>Shown when the author time is claimed - the goal of the game.</summary>
	public PanelView AuthorMedalClaimed()
	{
		return PanelView.Build(LevelHeadline(CurrentLevel), HudPalette.LevelName)
			.Heading("New Medal Claimed")
			.Row("AUTHOR TIME", HudPalette.Author, ResultTime(), HudPalette.White)
			.Line("Respawn to continue the hunt!", HudPalette.Info)
			.ShowFor(TransientSeconds)
			.Done();
	}

	/// <summary>Shown when a level could not be loaded and is being swapped out.</summary>
	public PanelView BrokenLevel(OnlineZeeplevel level)
	{
		return PanelView.Build("Broken Level", HudPalette.Alert)
			.Line("Oops! This level appears to be broken or unplayable.", HudPalette.Bad)
			.Line($"{level.Name} by {level.Author}", HudPalette.LevelName)
			.Line("Automatic recovery in progress", HudPalette.White)
			.Row("Retries left", _ctx.Retries.ToString(), HudPalette.Gold)
			.Line("No time penalty will be applied for this broken level", HudPalette.Info)
			.ShowFor(TransientSeconds)
			.Done();
	}

	/// <summary>Shown when the same level came up too many times in a row and the run gives up.</summary>
	public PanelView DuplicateLimitReached(int retries)
	{
		return PanelView.Build("Run Ended", HudPalette.Alert)
			.Line("The same level was duplicated too many times in a row.", HudPalette.Bad)
			.Row("Retries attempted", retries.ToString(), HudPalette.Gold)
			.Line("The run has been stopped automatically.", HudPalette.Info)
			.Done();
	}

	/// <summary>Per-level summary shown once a level is done, before the next one loads.</summary>
	public PanelView LevelSummary()
	{
		Split split = CurrentSplit();

		PanelBuilder panel = PanelView.Build(LevelHeadline(CurrentLevel), HudPalette.LevelName);

		if (!IsMinimalist)
		{
			panel.Heading("Result")
				.Row("Status", CurrentLevel.StatusString, StatusColour())
				.Row("Penalty", PenaltyLabel(), PenaltyColour());
		}

		panel.Heading("Stats");

		if (!IsMinimalist)
		{
			panel.Row("AT", HudPalette.Author, CurrentLevel.AuthorTime.GetFormattedTime(), HudPalette.White)
				.Row("Your Time", split.Result);
		}

		panel.Row(CurrentLevel.AuthorTimeAcquired ? "Beaten by" : "Missed by",
				CurrentLevel.AuthorTimeAcquired ? HudPalette.Positive : HudPalette.Negative,
				split.Difference, split.DifferenceColour)
			.Row("Attempts", CurrentLevel.Attempt.ToString())
			.Row("PlayDuration", CurrentLevel.GetPlayDuration().ToFormattedString())
			.Row("Time Wasted", CurrentLevel.TimeWasted.ToFormattedString(), HudPalette.Bad)
			.Heading("Current Run")
			.Row("Total ATs", _ctx.AuthorMedals.ToString(), HudPalette.Author)
			.Row("Time left", TimeFormatter.FormatDuration((int)_ctx.GetRemainingTime().TotalMilliseconds),
				TimeLeftColour());

		return panel.ShowFor(TransientSeconds).Done();
	}

	private static string LevelHeadline(Level level)
	{
		return $"{level.Name} by {level.Author}";
	}

	private static string ResultTime()
	{
		PlayerBase.Result result = ZeepkistNetwork.LocalPlayer?.CurrentResult;
		return result != null ? result.Time.GetFormattedTime() : NoTime;
	}

	private Split CurrentSplit()
	{
		PlayerBase.Result result = ZeepkistNetwork.LocalPlayer?.CurrentResult;

		if (result == null)
		{
			return new Split(false, NoTime, NoTime, HudPalette.White);
		}

		double difference = result.Time - CurrentLevel.AuthorTime;
		string display = $"{StringUtils.GetSign(difference)}{Math.Abs(difference).GetFormattedTime()}";

		return new Split(true, result.Time.GetFormattedTime(), display,
			difference <= 0 ? HudPalette.Positive : HudPalette.Negative);
	}

	private Color32 StatusColour()
	{
		if (CurrentLevel.AuthorTimeAcquired)
		{
			return HudPalette.Author;
		}

		if (CurrentLevel.LevelBroken)
		{
			return HudPalette.Muted;
		}

		if (CurrentLevel.GoldMedalAcquired)
		{
			return HudPalette.Gold;
		}

		return CurrentLevel.FreeSkipped ? HudPalette.FreeSkip : HudPalette.Penalty;
	}

	private string PenaltyLabel()
	{
		if (CurrentLevel.Status != Level.LevelStatus.FAILED)
		{
			return "none";
		}

		return _ctx.IsTimeOver() ? "End of Run" : $"{_ctx.PenaltyTimeInMilliseconds / 60 / 1000} minutes";
	}

	private Color32 PenaltyColour()
	{
		if (CurrentLevel.Status != Level.LevelStatus.FAILED)
		{
			return HudPalette.Good;
		}

		return _ctx.IsTimeOver() ? HudPalette.Fatal : HudPalette.Bad;
	}

	private Color32 TimeLeftColour()
	{
		double secondsLeft = _ctx.GetRemainingTime().TotalSeconds;

		if (secondsLeft > 300)
		{
			return HudPalette.Good;
		}

		return secondsLeft > 120 ? HudPalette.Warning : HudPalette.Danger;
	}

	/// <summary>The player's time on this level and how far off the author time it is.</summary>
	private readonly struct Split
	{
		public Split(bool hasResult, string result, string difference, Color32 differenceColour)
		{
			HasResult = hasResult;
			Result = result;
			Difference = difference;
			DifferenceColour = differenceColour;
		}

		public bool HasResult { get; }
		public string Result { get; }
		public string Difference { get; }
		public Color32 DifferenceColour { get; }
	}
}