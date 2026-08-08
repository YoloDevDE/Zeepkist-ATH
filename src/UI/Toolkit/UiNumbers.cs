namespace AuthorTimeHunting.UI.Toolkit;

/// <summary>
///     Small whole numbers as text, out of a table.
///     Attempts, crashes, wheels lost, ranks, level numbers - every panel prints a handful of
///     these, every frame it is up, and int.ToString allocates a fresh string each time even
///     though the answer has been the same for the last four hundred frames. The counts a hunt
///     produces are all small, so the strings can simply exist already.
///     Above the table it falls through to ToString, because being wrong is worse than
///     allocating.
/// </summary>
public static class UiNumbers
{
	private const int _max = 256;

	private static readonly string[] _labels = Build();

	public static string Text(int value)
	{
		return value >= 0 && value < _labels.Length ? _labels[value] : value.ToString();
	}

	private static string[] Build()
	{
		string[] labels = new string[_max];

		for (int i = 0; i < _max; i++)
		{
			labels[i] = i.ToString();
		}

		return labels;
	}
}
