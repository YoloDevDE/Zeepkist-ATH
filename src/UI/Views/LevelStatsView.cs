using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.UI.Views;

/// <summary>
///     What the level stats panel shows, as data. The counterpart to <see cref="RunHudView" />:
///     that one answers how the run is going, this one how this level is going.
/// </summary>
public class LevelStatsView
{
	private LevelStatsView()
	{
	}

	public string Name { get; private set; }

	public string ByAuthor { get; private set; }

	public string AuthorTime { get; private set; }
	public string GoldTime { get; private set; }

	public string BestWithDelta { get; private set; }

	public Color32 PersonalBestColour { get; private set; }

	public string Attempts { get; private set; }
	public string Crashes { get; private set; }
	public string WheelsLost { get; private set; }
	public string TimeOnLevel { get; private set; }

	public LevelPace Pace { get; private set; }

	public bool AttemptPending { get; private set; }

	public static LevelStatsView From(AthCtx ctx, bool attemptPending)
	{
		Level level = ctx?.CurrentLevel;

		if (level == null)
		{
			return null;
		}

		LevelStatsView view = new()
		{
			AttemptPending = attemptPending,
			Name = level.Name,
			ByAuthor = $"by {level.Author}",
			AuthorTime = TimeFormatter.FormatTime(level.AuthorTime),
			GoldTime = TimeFormatter.FormatTime(level.GoldTime),
			Attempts = attemptPending ? $"{level.Attempt} (+1)" : UiNumbers.Text(level.Attempt),
			Crashes = UiNumbers.Text(level.Crashes),
			WheelsLost = UiNumbers.Text(level.WheelsLost),
			TimeOnLevel = TimeFormatter.FormatDuration((int)level.GetPlayDuration().TotalMilliseconds),
			Pace = LevelRating.Rate(level, ctx.Levels)
		};

		if (level.PersonalBestTime < 0)
		{
			return view;
		}

		double delta = level.PersonalBestTime - level.AuthorTime;

		view.BestWithDelta =
			$"{TimeFormatter.FormatTime(level.PersonalBestTime)}   ({TimeFormatter.FormatDelta(delta)})";
		view.PersonalBestColour = level.AuthorTimeAcquired ? Color.Zeepkist.Medal.Author
			: level.GoldMedalAcquired ? Color.Zeepkist.Medal.Gold
			: Color.Style.Text.Default;

		return view;
	}

	public static string PaceLabel(LevelPace pace)
	{
		return pace switch
		{
			LevelPace.Good => "Ahead of your average",
			LevelPace.Okay => "About average",
			LevelPace.Bad => "Costing you time",
			_ => "Not enough levels yet"
		};
	}

	public static Color32 PaceColour(LevelPace pace)
	{
		return pace switch
		{
			LevelPace.Good => Color.Style.Status.Good,
			LevelPace.Okay => Color.Style.Status.Warning,
			LevelPace.Bad => Color.Style.Status.Danger,
			_ => Color.Style.Text.Muted
		};
	}
}
