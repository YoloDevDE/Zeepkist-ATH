namespace AuthorTimeHunting.UI;

/// <summary>One rule of the run about to start, as the loading screen prints it.</summary>
public readonly struct SetupLine
{
	public SetupLine(string label, string value)
	{
		Label = label;
		Value = value;
	}

	public string Label { get; }

	public string Value { get; }
}
