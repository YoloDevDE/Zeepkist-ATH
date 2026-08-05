using TMPro;

namespace AuthorTimeHunting.UI;

/// <summary>What a label looked like before ATH took it over.</summary>
public readonly struct LabelState
{
	public LabelState(TextOverflowModes overflow, bool autoSizing, float fontSize, bool wordWrapping)
	{
		Overflow = overflow;
		AutoSizing = autoSizing;
		FontSize = fontSize;
		WordWrapping = wordWrapping;
	}

	public TextOverflowModes Overflow { get; }
	public bool AutoSizing { get; }
	public float FontSize { get; }
	public bool WordWrapping { get; }
}
