using System;
using System.Collections.Generic;

namespace AuthorTimeHunting.Entities;

public class Level
{
	public Level(LevelScriptableObject level)
		: this(level.UID, level.Name, level.Author, level.TimeAuthor, level.TimeGold)
	{
	}

	public Level(string levelUid, string name, string author, double authorTime, double goldTime)
	{
		LevelUid = levelUid;
		Name = name;
		Author = author;
		AuthorTime = authorTime;
		GoldTime = goldTime;
	}

	public string LevelUid { get; }

	public string Name { get; }
	public string Author { get; }
	public double AuthorTime { get; }
	public double GoldTime { get; }

	public int Attempt { get; set; }

	public int Crashes { get; private set; }

	public int WheelsLost { get; private set; }

	public bool Skipped { get; set; }
	public bool FreeSkipped { get; set; }
	public bool LevelBroken { get; set; }

	public LevelStatus Status
	{
		get
		{
			if (LevelBroken)
			{
				return LevelStatus.Broken;
			}

			if (PersonalBestTime <= AuthorTime && PersonalBestTime >= 0)
			{
				return LevelStatus.Author;
			}

			if (PersonalBestTime <= GoldTime && PersonalBestTime >= 0)
			{
				return LevelStatus.Gold;
			}

			if (FreeSkipped)
			{
				return LevelStatus.Free;
			}

			if (Skipped)
			{
				return LevelStatus.Failed;
			}

			{
				return LevelStatus.Unknown;
			}
		}
	}

	public bool AuthorTimeAcquired => Status == LevelStatus.Author;
	public bool GoldMedalAcquired => Status is LevelStatus.Gold or LevelStatus.Author;
	public bool PenaltySkipped => Status == LevelStatus.Failed && Skipped;

	public float PersonalBestTime
	{
		get;
		set
		{
			if (value >= 0 && (field <= 0 || value < field))
			{
				field = value;
			}
		}
	} = -1f;

	public TimeSpan TimeWasted =>
		LevelBroken ? TimeSpan.Zero
		: AuthorTimeAcquired ? GetPlayDuration() - TimeSpan.FromSeconds(PersonalBestTime)
		: GetPlayDuration();

	public string StatusString
	{
		get
		{
			return Status switch
			{
				LevelStatus.Author => "Completed", LevelStatus.Gold => "Gold-Skipped",
				LevelStatus.Free => "Free-Skipped", LevelStatus.Broken => "Broken",
				LevelStatus.Failed => "Failed",
				_ => "Unknown"
			};
		}
	}

	public DateTime StartTime { get; set; }

	public DateTime EndTime
	{
		get => field == default ? DateTime.Now : field;
		set;
	}

	private List<DateTime> TimeStamps { get; } = [];

	public bool IsTiming => TimeStamps.Count % 2 == 1;

	public void PauseTiming()
	{
		if (TimeStamps.Count % 2 == 1)
		{
			TimeStamps.Add(DateTime.Now);
		}
	}

	public void ResumeTiming()
	{
		if (TimeStamps.Count % 2 == 0)
		{
			TimeStamps.Add(DateTime.Now);
		}
	}

	public void RegisterCrash()
	{
		Crashes++;
	}

	public void RegisterWheelLost()
	{
		WheelsLost++;
	}

	/// <summary>
	///     A fresh level has no attempts on it. They are counted when the zeepkists are actually
	///     released, so that during a countdown the attempt about to start is always
	///     <c>Attempt + 1</c> - which is what the start lights put on screen.
	/// </summary>
	public void Start()
	{
		StartTime = DateTime.Now;
		TimeStamps.Clear();
		Attempt = 0;
	}

	public void Stop()
	{
		EndTime = DateTime.Now;

		if (TimeStamps.Count % 2 == 1)
		{
			TimeStamps.Add(EndTime);
		}
	}

	public TimeSpan GetPlayDuration()
	{
		if (TimeStamps == null || TimeStamps.Count == 0)
		{
			return TimeSpan.Zero;
		}

		DateTime now = DateTime.Now;
		TimeSpan result = TimeSpan.Zero;

		if (TimeStamps.Count == 1)
		{
			TimeSpan duration = now - TimeStamps[0];
			return duration > TimeSpan.Zero ? duration : TimeSpan.Zero;
		}

		for (int i = 0; i < TimeStamps.Count - 1; i++)
		{
			if (i % 2 == 1)
			{
				continue;
			}

			DateTime startTime = TimeStamps[i];
			DateTime endTime = i + 1 < TimeStamps.Count ? TimeStamps[i + 1] : now;

			TimeSpan sessionDuration = endTime - startTime;

			if (sessionDuration > TimeSpan.Zero)
			{
				result += sessionDuration;
			}
		}

		if (TimeStamps.Count % 2 == 1)
		{
			result += now - TimeStamps[^1];
		}

		return result;
	}

	public override bool Equals(object obj)
	{
		if (obj is Level other)
		{
			return LevelUid == other.LevelUid;
		}

		return false;
	}

	public override int GetHashCode()
	{
		return LevelUid.GetHashCode();
	}
}
