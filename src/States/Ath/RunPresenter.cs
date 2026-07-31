using System;
using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Util;
using Crosstales;
using ZeepkistClient;
using ZeepkistNetworking;

namespace AuthorTimeHunting.States.Ath;

/// <summary>
///     Builds every chat message ATH sends. Reads the run through <see cref="AthCtx" /> and
///     returns strings - it never mutates run state and never sends anything itself, that is
///     ChatMessageService's job.
///     Reachable as <see cref="AthCtx.Messages" />, so call sites read Ctx.Messages.End().
/// </summary>
public class RunPresenter
{
	private readonly AthCtx _ctx;

	public RunPresenter(AthCtx ctx)
	{
		_ctx = ctx;
	}

	private static bool IsMinimalist => Plugin.Instance.MyConfig.Minimalist.Value;

	private Level CurrentLevel => _ctx.CurrentLevel;

	/// <summary>
	///     Welcome message with the run settings, sent at the start of a run.
	/// </summary>
	public string Starting()
	{
		Message.Builder message = new();
		message.ClearLines().AddLine("<#FFD700>Welcome to Author-Time-Hunting!</color>").AddBreakSpace();

		if (!IsMinimalist)
		{
			AddDetailedWelcomeInfo(message);
		}
		else
		{
			AddMinimalistWelcomeInfo(message);
		}

		message.AddBreakSpace().AddLine("<#FFD700>Good luck & have fun!</color>");
		return message.Build().ToString();
	}

	private void AddDetailedWelcomeInfo(Message.Builder message)
	{
		message.AddSeperator("<#B336A3>Author Time Hunting</color>").AddBreakSpace()
			.AddLine("<#E0E0E0>Collect Author Medals within the time limit!</color>").AddBreakSpace()
			.AddSeperator("<#B336A3>Settings</color>").AddBreakSpace()
			.AddKeyValue("<#7FDBFF>Duration</color>",
				$"<#FFFFFF>{TimeSpan.FromMilliseconds(_ctx.Duration).ToFormattedString()}</color>").AddBreakSpace()
			.AddKeyValue("<#FF7A7A>Skip Penalty</color>",
				$"<#FF4040>{TimeSpan.FromMilliseconds(_ctx.PenaltyTimeInMilliseconds).ToFormattedString()}</color>")
			.AddBreakSpace().AddSeperator("<#50E451>Commands</color>").AddBreakSpace()
			.AddKeyValue("<#7AFF7A>/fs</color>", "<#E0E0E0>Skip level</color>").AddBreakSpace()
			.AddKeyValue("<#7AFF7A>/ath broken</color>", "<#E0E0E0>Skip unbeatable map</color>");
	}

	private void AddMinimalistWelcomeInfo(Message.Builder message)
	{
		message.AddSeperator("<#50E451>Commands</color>").AddBreakSpace()
			.AddKeyValue("<#7AFF7A>/fs</color>", "<#E0E0E0>Skip level (free)</color>").AddBreakSpace()
			.AddKeyValue("<#7AFF7A>/ath broken</color>", "<#E0E0E0>Skip unbeatable map</color>")
			.AddBreakSpace().AddKeyValue("<#7AFF7A>/ath restart</color>", "<#E0E0E0>Restart the hunt</color>")
			.AddBreakSpace().AddKeyValue("<#7AFF7A>/ath stop</color>", "<#E0E0E0>End the hunt</color>");
	}

	/// <summary>
	///     Medal targets and the current split, sent when a run on a level starts.
	/// </summary>
	public string OnARun()
	{
		PlayerBase.Result currentResult = ZeepkistNetwork.LocalPlayer.CurrentResult;
		// Initialize default values
		double result = 0;
		double positiveResult = 0;
		string diffDisplay = " --:--.---";
		Message.Builder message = new Message.Builder().ClearLines()
			.AddLine($"<#64D2FF>{CurrentLevel.Name}</color> by <#FFD700>{CurrentLevel.Author}</color>").AddBreakSpace()
			.AddSeperator("<#B336A3>Medals</color>").AddBreakSpace()
			.AddKeyValue($"<#{ColorDefinitions.Author.CTToHexRGB()}>AT</color>",
				$"<#FFFFFF>{CurrentLevel.AuthorTime.GetFormattedTime()}</color>");

		// Show gold time if gold skip isn't unlocked yet
		if (!CurrentLevel.GoldMedalAcquired)
		{
			message.AddBreakSpace().AddKeyValue($"<#{ColorDefinitions.Gold.CTToHexRGB()}>Gold</color>",
				$"<#FFFFFF>{CurrentLevel.GoldTime.GetFormattedTime()}</color>");
		}

		// Add current result if available
		if (currentResult != null)
		{
			result = currentResult.Time - CurrentLevel.AuthorTime;
			positiveResult = Math.Abs(result);
			diffDisplay = $"{StringUtils.GetSign(result)}{positiveResult.GetFormattedTime()}";
			string diffColor =
				$"#{(result <= 0 ? ColorDefinitions.GreenSplit.CTToHexRGB() : ColorDefinitions.YellowSplit.CTToHexRGB())}"; // Green if better, red if worse
			message.AddBreakSpace().AddKeyValue(
				$"{(CurrentLevel.AuthorTimeAcquired ? $"{(CurrentLevel.GoldMedalAcquired ? "" : $"<#{ColorDefinitions.Author.CTToHexRGB()}>AT</color> ")}<#50E451>Beaten by</color>" : $"{(CurrentLevel.GoldMedalAcquired ? "" : $"<#{ColorDefinitions.Author.CTToHexRGB()}>AT</color> ")}<#{ColorDefinitions.YellowSplit.CTToHexRGB()}>Missed by</color>")}"
				, $"<{diffColor}>{diffDisplay}</color>");
		}

		// Add gold time if gold skip is unlocked
		if (CurrentLevel.GoldMedalAcquired)
		{
			message.AddBreakSpace().AddKeyValue($"<#{ColorDefinitions.Gold.CTToHexRGB()}>Gold</color>",
				$"<#FFFFFF>{CurrentLevel.GoldTime.GetFormattedTime()}</color>");
		}

		return message.Build().ToString();
	}

	/// <summary>
	///     Result and split, sent when the player crosses the finish line.
	/// </summary>
	public string CrossedFinishLine()
	{
		PlayerBase.Result currentResult = ZeepkistNetwork.LocalPlayer.CurrentResult;
		// Initialize default values
		double result = 0;
		double positiveResult = 0;
		string diffDisplay = " --:--.---";
		string resultDisplay = " --:--.---";

		if (currentResult != null)
		{
			result = currentResult.Time - CurrentLevel.AuthorTime;
			positiveResult = Math.Abs(result);
			diffDisplay = $"{StringUtils.GetSign(result)}{positiveResult.GetFormattedTime()}";
			resultDisplay = currentResult.Time.GetFormattedTime();
		}

		string diffColor =
			$"#{(result <= 0 ? ColorDefinitions.GreenSplit.CTToHexRGB() : ColorDefinitions.YellowSplit.CTToHexRGB())}"; // Green if better, red if worse

		Message.Builder message = new();
		message.ClearLines().AddLine($"<#64D2FF>{CurrentLevel.Name}</color> by <#FFD700>{CurrentLevel.Author}</color>")
			.AddBreakSpace().AddSeperator("<#B336A3>Result</color>").AddBreakSpace()
			.AddKeyValue($"<#{ColorDefinitions.Author.CTToHexRGB()}>AT</color>",
				$"<#FFFFFF>{CurrentLevel.AuthorTime.GetFormattedTime()}</color>");

		if (!CurrentLevel.GoldMedalAcquired)
		{
			message.AddBreakSpace().AddKeyValue($"<#{ColorDefinitions.Gold.CTToHexRGB()}>Gold</color>",
					$"<#FFFFFF>{CurrentLevel.GoldTime.GetFormattedTime()}</color>").AddBreakSpace()
				.AddKeyValue("<#7FDBFF>Your Time</color>", $"<#FFFFFF>{resultDisplay}</color>").AddBreakSpace()
				.AddKeyValue(
					$"{(CurrentLevel.AuthorTimeAcquired ? "<#50E451>AT Beaten by</color>" : $"<#{ColorDefinitions.YellowSplit.CTToHexRGB()}>AT Missed by</color>")}",
					$"<{diffColor}>{diffDisplay}</color>");
		}
		else
		{
			message.AddBreakSpace().AddKeyValue("<#7FDBFF>Your Time</color>", $"<#FFFFFF>{resultDisplay}</color>")
				.AddBreakSpace()
				.AddKeyValue(
					$"{(CurrentLevel.AuthorTimeAcquired ? "<#50E451>Beaten by</color>" : $"<#{ColorDefinitions.YellowSplit.CTToHexRGB()}>Missed by</color>")}",
					$"<{diffColor}>{diffDisplay}</color>").AddBreakSpace()
				.AddKeyValue($"<#{ColorDefinitions.Gold.CTToHexRGB()}>Gold</color>",
					$"<#FFFFFF>{CurrentLevel.GoldTime.GetFormattedTime()}</color>");
		}

		return message.Build().ToString();
	}

	/// <summary>
	///     The end-of-run summary: medal and attempt counts, time breakdown, plus up to three
	///     narrative sections about individual levels and authors.
	/// </summary>
	public string End()
	{
		RunStatistics stats = new(_ctx.Levels);
		Level biggestTimeSink = stats.BiggestTimeSink;
		Level easiestLevel = stats.EasiestBeatenLevel;

		Message.Builder builder = new Message.Builder().ClearLines()
			.AddLine("<#FFD700>Authortime Hunt finished! :party:</color>").AddBreakSpace()
			.AddSeperator("<#B336A3>Result</color>").AddBreakSpace()
			// Medal Stats
			.AddKeyValue("<#64D2FF>Total ATs</color>",
				$"<#{ColorDefinitions.Author.CTToHexRGB()}>{_ctx.AuthorMedals}</color>").AddBreakSpace()
			.AddKeyValue("<#64D2FF>ATs Oneshotted!</color>", $"<#50E451>{stats.OneShotAuthorTimes}</color>")
			.AddBreakSpace()
			// Attempt Stats
			.AddKeyValue("<#64D2FF>Total Resets</color>", $"<#FFFFFF>{stats.TotalAttempts}</color>").AddBreakSpace()
			.AddKeyValue("<#64D2FF>Total Skips</color>", $"<#FFFFFF>{stats.PenaltySkipCount}</color>").AddBreakSpace()
			.AddKeyValue("<#64D2FF>Attempts/AT</color>", $"<#FFFFFF>{stats.AverageAttemptsPerAuthorTime:F2}</color>")
			.AddBreakSpace()
			// Time Stats
			.AddKeyValue("<#64D2FF>Avg AT</color>", $"<#FFFFFF>{stats.AverageAuthorTime.GetFormattedTime()}</color>")
			.AddBreakSpace()
			.AddKeyValue("<#64D2FF>Time Wasted</color>",
				$"<#FF5A5A>{stats.TotalTimeWasted.ToFormattedString()}</color>").AddBreakSpace()
			.AddKeyValue("<#64D2FF>Time/AT</color>",
				$"<#FFFFFF>{stats.AverageTimePerAuthorTime.ToFormattedString()}</color>").AddBreakSpace();

		try
		{
			// Add the "should have skipped" section if applicable
			if (biggestTimeSink != null)
			{
				AddShouldHaveSkippedSection(builder, biggestTimeSink);
			}

			// Add the easiest level section if applicable
			if (easiestLevel != null)
			{
				builder.AddSeperator("<#50E451>Easiest Level</color>").AddBreakSpace()
					.AddLine($"<#64D2FF>{easiestLevel.Name}</color> by <#FFD700>{easiestLevel.Author}</color>")
					.AddBreakSpace()
					.AddKeyValue("<#7FDBFF>Attempts</color>", $"<#FFFFFF>{easiestLevel.Attempt}</color>")
					.AddBreakSpace().AddKeyValue("<#7FDBFF>PlayDuration</color>",
						$"<#FFFFFF>{easiestLevel.GetPlayDuration().ToFormattedString()}</color>")
					.AddBreakSpace();
			}
		}
		catch (Exception e)
		{
			Logger.LogError(e);
			// Handle the exception or continue execution
		}

		// Add the "liked author" section if applicable
		(string Author, List<Level> Levels) likedAuthor = stats.MostBeatenAuthor;

		if (likedAuthor.Levels is { Count: > 0 })
		{
			AddLikedAuthorSection(builder, likedAuthor);
		}

		return builder.Build().ToString();
	}

	private void AddShouldHaveSkippedSection(Message.Builder builder, Level level)
	{
		builder.AddSeperator("<#FF5A5A>You should have skipped this :yannics:</color>").AddBreakSpace()
			.AddLine($"<#64D2FF>{level.Name}</color> by <#FFD700>{level.Author}</color>").AddBreakSpace()
			.AddKeyValue("<#7FDBFF>Status</color>", $"<#FFFFFF>{level.StatusString}</color>").AddBreakSpace()
			.AddKeyValue("<#7FDBFF>PlayDuration</color>",
				$"<#FF7A7A>{level.GetPlayDuration().ToFormattedString()}</color>").AddBreakSpace()
			.AddKeyValue("<#7FDBFF>Attempts</color>", $"<#FFFFFF>{level.Attempt}</color>").AddBreakSpace();
	}

	private void AddLikedAuthorSection(Message.Builder builder, (string Author, List<Level> Levels) likedAuthor)
	{
		builder.AddSeperator("<#50E451>This Author haunted you</color>").AddBreakSpace()
			.AddLine($"<#E0E0E0>And their name is...</color><br><#FFD700>'{likedAuthor.Author}' !</color>")
			.AddBreakSpace()
			.AddLine($"<#E0E0E0>You've beaten {likedAuthor.Levels.Count} of their levels:</color>").AddBreakSpace();

		foreach (Level level in likedAuthor.Levels) builder.AddLine($"<#64D2FF>- {level.Name}</color>").AddBreakSpace();
	}

	/// <summary>
	///     Sent when the gold medal is claimed and the level becomes free to skip.
	/// </summary>
	public string GoldMedalClaimed()
	{
		PlayerBase.Result currentResult = ZeepkistNetwork.LocalPlayer.CurrentResult;
		string timeDisplay = currentResult != null ? currentResult.Time.GetFormattedTime() : "--:--.---";

		Message.Builder message = new();
		message.ClearLines().AddLine($"<#64D2FF>{CurrentLevel.Name}</color> by <#FFD700>{CurrentLevel.Author}</color>")
			.AddBreakSpace().AddSeperator($"<#{ColorDefinitions.Gold.CTToHexRGB()}>New Medal Claimed</color>")
			.AddBreakSpace()
			.AddKeyValue($"<#{ColorDefinitions.Gold.CTToHexRGB()}>GOLD</color>", $"<#FFFFFF>{timeDisplay}</color>")
			.AddBreakSpace().AddLine("<#AAAAAA>You can now skip this level without a time penalty!</color>");

		return message.Build().ToString();
	}

	/// <summary>
	///     Sent when the author time is claimed - the goal of the game.
	/// </summary>
	public string AuthorMedalClaimed()
	{
		PlayerBase.Result currentResult = ZeepkistNetwork.LocalPlayer.CurrentResult;
		string timeDisplay = currentResult != null ? currentResult.Time.GetFormattedTime() : "--:--.---";

		Message.Builder message = new();
		message.ClearLines().AddLine($"<#64D2FF>{CurrentLevel.Name}</color> by <#FFD700>{CurrentLevel.Author}</color>")
			.AddBreakSpace().AddSeperator($"<#{ColorDefinitions.Author.CTToHexRGB()}>New Medal Claimed</color>")
			.AddBreakSpace()
			.AddKeyValue($"<#{ColorDefinitions.Author.CTToHexRGB()}>AUTHOR TIME</color>",
				$"<#FFFFFF>{timeDisplay}</color>").AddBreakSpace()
			.AddLine("<#AAAAAA>Respawn to continue the hunt!</color>");

		return message.Build().ToString();
	}

	/// <summary>
	///     Sent when a level could not be loaded and is being swapped out.
	/// </summary>
	public string BrokenLevel(OnlineZeeplevel level)
	{
		Message.Builder message = new();
		message.ClearLines().AddSeperator("<#FF0000>Broken Level</color>").AddBreakSpace()
			.AddLine("<#FF5555>Oops! This level appears to be broken or unplayable.</color>").AddBreakSpace()
			.AddLine($"<#64D2FF>{level.Name}</color> by <#FFD700>{level.Author}</color>").AddBreakSpace()
			.AddLine("<#FFFFFF>Automatic recovery in progress</color>").AddBreakSpace()
			.AddLine($"<#AAAAAA>Retries left: <#FFFF00>{_ctx.Retries}</color></color>").AddBreakSpace()
			.AddLine("<#AAAAAA>No time penalty will be applied for this broken level</color>");

		return message.Build().ToString();
	}

	/// <summary>
	///     Sent when the same level came up too many times in a row and the run gives up.
	/// </summary>
	public string DuplicateLimitReached(int retries)
	{
		Message.Builder message = new();
		message.ClearLines().AddSeperator("<#FF0000>Run Ended</color>").AddBreakSpace()
			.AddLine("<#FF5555>The same level was duplicated too many times in a row.</color>").AddBreakSpace()
			.AddLine($"<#FFFFFF>Retries attempted: <#FFFF00>{retries}</color></color>").AddBreakSpace()
			.AddLine("<#AAAAAA>The run has been stopped automatically.</color>");

		return message.Build().ToString();
	}

	/// <summary>
	///     Per-level summary sent once a level is done, before the next one loads.
	/// </summary>
	public string LevelSummary()
	{
		PlayerBase.Result currentResult = ZeepkistNetwork.LocalPlayer.CurrentResult;
		// Initialize default values
		double result = 0;
		double positiveResult = 0;
		string diffDisplay = "--:--.---";
		string resultDisplay = "--:--.---";

		if (currentResult != null)
		{
			result = currentResult.Time - CurrentLevel.AuthorTime;
			positiveResult = Math.Abs(result);
			diffDisplay = $"{StringUtils.GetSign(result)}{positiveResult.GetFormattedTime()}";
			resultDisplay = currentResult.Time.GetFormattedTime();
		}

		string diffColor =
			$"#{(result <= 0 ? ColorDefinitions.GreenSplit.CTToHexRGB() : ColorDefinitions.YellowSplit.CTToHexRGB())}"; // Green if better, red if worse
		string statusColor = CurrentLevel.AuthorTimeAcquired
			? "#e600e6"
			: CurrentLevel.LevelBroken
				? "#999999"
				: CurrentLevel.GoldMedalAcquired
					? "#FFD600"
					: CurrentLevel.FreeSkipped
						? "#00ffff"
						: "#bf3939";

		Message.Builder message = new();
		message.ClearLines().AddLine($"<#64D2FF>{CurrentLevel.Name}</color> by <#FFD700>{CurrentLevel.Author}</color>");

		if (!IsMinimalist)
		{
			message.AddBreakSpace().AddSeperator("<#B336A3>Result</color>").AddBreakSpace()
				.AddKeyValue("<#7FDBFF>Status</color>", $"<{statusColor}>{CurrentLevel.StatusString}</color>")
				.AddBreakSpace().AddKeyValue("<#7FDBFF>Penalty</color>"
					, $"{(CurrentLevel.Status != Level.LevelStatus.FAILED ? "<#42b336>none</color>" : _ctx.IsTimeOver() ? "<#0f0f0f>End of Run</color>" : $"<#FF5A5A>{_ctx.PenaltyTimeInMilliseconds / 60 / 1000} minutes</color>")}");
		}

		message.AddBreakSpace().AddSeperator("<#B336A3>Stats</color>");

		if (!IsMinimalist)
		{
			message.AddBreakSpace().AddKeyValue($"<#{ColorDefinitions.Author.CTToHexRGB()}>AT</color>",
					$"<#FFFFFF>{CurrentLevel.AuthorTime.GetFormattedTime()}</color>").AddBreakSpace()
				.AddKeyValue("<#7FDBFF>Your Time</color>", $"<#FFFFFF>{resultDisplay}</color>");
		}

		message.AddBreakSpace()
			.AddKeyValue(
				$"{(CurrentLevel.AuthorTimeAcquired ? "<#50E451>Beaten by</color>" : $"<#{ColorDefinitions.YellowSplit.CTToHexRGB()}>Missed by</color>")}",
				$"<{diffColor}>{diffDisplay}</color>").AddBreakSpace()
			.AddKeyValue("<#7FDBFF>Attempts</color>", $"<#FFFFFF>{CurrentLevel.Attempt}</color>").AddBreakSpace()
			.AddKeyValue("<#7FDBFF>PlayDuration</color>",
				$"<#FFFFFF>{CurrentLevel.GetPlayDuration().ToFormattedString()}</color>").AddBreakSpace()
			.AddKeyValue("<#7FDBFF>Time Wasted</color>",
				$"<#FF5A5A>{CurrentLevel.TimeWasted.ToFormattedString()}</color>").AddBreakSpace()
			.AddSeperator("<#B336A3>Current Run</color>").AddBreakSpace()
			.AddKeyValue("<#7FDBFF>Total ATs</color>",
				$"<#{ColorDefinitions.Author.CTToHexRGB()}>{_ctx.AuthorMedals}</color>").AddBreakSpace();

		// Use color based on remaining time
		string timeLeftColor = _ctx.GetRemainingTime().TotalSeconds > 300
			? "#42b336"
			: // Green if > 5 minutes
			_ctx.GetRemainingTime().TotalSeconds > 120
				? "#b3b300"
				: // Yellow if > 2 minutes
				"#bf3939"; // Red if < 2 minutes

		message.AddKeyValue("<#7FDBFF>Time left</color>",
			$"<{timeLeftColor}>{TimeFormatter.FormatDuration((int)_ctx.GetRemainingTime().TotalMilliseconds)}</color>");
		return message.Build().ToString();
	}
}