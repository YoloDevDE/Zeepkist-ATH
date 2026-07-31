using System.Collections.Generic;
using UnityEngine;

namespace AuthorTimeHunting.UI;

/// <summary>
///     Everything ATH says, as data rather than as a formatted string.
///     Each of these used to be built directly as a TextMeshPro string with colour tags
///     inside RunPresenter, which meant the wording, the layout and the colours could only
///     ever be a chat message. A panel is renderer-agnostic: <see cref="AthPanelDrawer" />
///     draws it as an in-game window, <see cref="PanelChatRenderer" /> turns it back into the
///     old chat message. One source of truth, two outputs.
/// </summary>
public class PanelView
{
	public PanelView(string title, Color32 titleColour, IReadOnlyList<PanelBlock> blocks, float displaySeconds)
	{
		Title = title;
		TitleColour = titleColour;
		Blocks = blocks;
		DisplaySeconds = displaySeconds;
	}

	public string Title { get; }
	public Color32 TitleColour { get; }
	public IReadOnlyList<PanelBlock> Blocks { get; }

	/// <summary>
	///     How long the in-game window shows this before hiding itself. Zero means it stays
	///     until something replaces it - used for the end-of-run summary, which the player
	///     should be able to read at their own pace.
	/// </summary>
	public float DisplaySeconds { get; }

	public static PanelBuilder Build(string title, Color32 titleColour)
	{
		return new PanelBuilder(title, titleColour);
	}
}

public enum PanelBlockKind
{
	/// <summary>A section heading.</summary>
	Heading,

	/// <summary>A free line of text.</summary>
	Line,

	/// <summary>A label on the left, a value on the right.</summary>
	Row
}

public readonly struct PanelBlock
{
	public PanelBlock(PanelBlockKind kind, string label, string value, Color32 labelColour, Color32 valueColour)
	{
		Kind = kind;
		Label = label;
		Value = value;
		LabelColour = labelColour;
		ValueColour = valueColour;
	}

	public PanelBlockKind Kind { get; }
	public string Label { get; }
	public string Value { get; }
	public Color32 LabelColour { get; }
	public Color32 ValueColour { get; }
}

public class PanelBuilder
{
	private readonly List<PanelBlock> _blocks = new();
	private readonly string _title;
	private readonly Color32 _titleColour;
	private float _displaySeconds;

	internal PanelBuilder(string title, Color32 titleColour)
	{
		_title = title;
		_titleColour = titleColour;
	}

	public PanelBuilder Heading(string text)
	{
		_blocks.Add(new PanelBlock(PanelBlockKind.Heading, text, null, HudPalette.Heading, HudPalette.Heading));
		return this;
	}

	public PanelBuilder Line(string text)
	{
		return Line(text, HudPalette.Default);
	}

	public PanelBuilder Line(string text, Color32 colour)
	{
		_blocks.Add(new PanelBlock(PanelBlockKind.Line, text, null, colour, colour));
		return this;
	}

	public PanelBuilder Row(string label, string value)
	{
		return Row(label, value, HudPalette.White);
	}

	public PanelBuilder Row(string label, string value, Color32 valueColour)
	{
		return Row(label, HudPalette.Key, value, valueColour);
	}

	public PanelBuilder Row(string label, Color32 labelColour, string value, Color32 valueColour)
	{
		_blocks.Add(new PanelBlock(PanelBlockKind.Row, label, value, labelColour, valueColour));
		return this;
	}

	/// <summary>Adds the block only when <paramref name="condition" /> holds.</summary>
	public PanelBuilder When(bool condition, System.Action<PanelBuilder> add)
	{
		if (condition)
		{
			add(this);
		}

		return this;
	}

	/// <summary>Zero, the default, keeps the panel up until something replaces it.</summary>
	public PanelBuilder ShowFor(float seconds)
	{
		_displaySeconds = seconds;
		return this;
	}

	public PanelView Done()
	{
		return new PanelView(_title, _titleColour, _blocks, _displaySeconds);
	}
}
