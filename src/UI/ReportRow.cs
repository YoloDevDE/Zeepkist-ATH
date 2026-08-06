using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.UI;

/// <summary>One label/value pair. The colour applies to the value, not the label.</summary>
public readonly struct ReportRow
{
	public ReportRow(string label, string value) : this(label, value, Color.Style.Text.Default)
	{
	}

	public ReportRow(string label, string value, Color32 valueColour)
	{
		Label = label;
		Value = value;
		ValueColour = valueColour;
	}

	public string Label { get; }
	public string Value { get; }
	public Color32 ValueColour { get; }
}
