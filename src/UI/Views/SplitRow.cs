namespace AuthorTimeHunting.UI.Views;

/// <summary>
///     One line of the split list, already written out: which checkpoint it is, when this
///     attempt got there, how that compares to the best run so far, and how fast it went
///     through.
///     A checkpoint the attempt has not reached yet is still a row. It carries the placeholder
///     time and an empty gap, because a list that grows a line every few seconds moves every
///     line under it - and the line under it is the one being read.
/// </summary>
public readonly struct SplitRow
{
	public SplitRow(string label, string time, string gap, SplitPace pace, string speed, SplitPace speedPace)
	{
		Label = label;
		Time = time;
		Gap = gap;
		Pace = pace;
		Speed = speed;
		SpeedPace = speedPace;
	}

	public string Label { get; }

	public string Time { get; }

	/// <summary>The signed seconds between this attempt and the best one, or empty where there is no answer.</summary>
	public string Gap { get; }

	public SplitPace Pace { get; }

	public string Speed { get; }

	public SplitPace SpeedPace { get; }
}
