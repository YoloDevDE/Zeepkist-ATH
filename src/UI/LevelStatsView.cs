using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.UI;

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

	/// <summary>The author, already written the way the panel says it.</summary>
	public string ByAuthor { get; private set; }

	public string AuthorTime { get; private set; }
	public string GoldTime { get; private set; }

	/// <summary>
	///     Best time on this level so far and how far off the author time it is, or null when the
	///     level has never been finished. Composed here rather than at the draw, like the two
	///     sibling view models: the panel is redrawn every frame, the sentence is not new every
	///     frame.
	/// </summary>
	public string BestWithDelta { get; private set; }

	public Color32 PersonalBestColour { get; private set; }

	public string Attempts { get; private set; }
	public string Crashes { get; private set; }
	public string WheelsLost { get; private set; }
	public string TimeOnLevel { get; private set; }

	public LevelPace Pace { get; private set; }

	/// <summary>
	///     True while the attempt counter is about to move. The counter is bumped on the frame
	///     the player respawns into a running level, so between attempts it names the attempt
	///     that has just ended rather than the one about to start - which read as the panel
	///     having missed the crash. Saying "(+1)" is the honest version: the number is right,
	///     it is simply one respawn behind.
	/// </summary>
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
		view.PersonalBestColour = level.AuthorTimeAcquired ? HudPalette.Author
			: level.GoldMedalAcquired ? HudPalette.Gold
			: HudPalette.Default;

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
			LevelPace.Good => HudPalette.Good,
			LevelPace.Okay => HudPalette.Warning,
			LevelPace.Bad => HudPalette.Danger,
			_ => HudPalette.Muted
		};
	}
}
