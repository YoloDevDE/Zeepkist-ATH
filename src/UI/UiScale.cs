using System;
using Imui.Core;
using Imui.Style;

namespace AuthorTimeHunting.UI;

/// <summary>
///     Shrinks everything ATH draws to a third less than the game's own Imui theme, for the
///     length of one panel.
///     Scaling the individual rects was tried first and only got half the way there: Imui's own
///     controls - a button's label, a window's title bar, the padding around a window's content
///     - all read the theme rather than the rect, so a "smaller" panel kept full-size chrome and
///     ended up looking cramped rather than smaller. The theme is a plain set of fields, so the
///     honest move is to turn them down, draw, and put them back before anyone else draws.
/// </summary>
public readonly struct UiScale : IDisposable
{
	/// <summary>What the game's theme is multiplied by. 0.67 is the requested third off.</summary>
	public const float Factor = 0.67f;

	private readonly ImGui _gui;
	private readonly ImStyleLayout _layout;
	private readonly ImPadding _padding;

	private UiScale(ImGui gui, ImStyleLayout layout, ImPadding padding)
	{
		_gui = gui;
		_layout = layout;
		_padding = padding;
	}

	/// <summary>
	///     Turns the theme down and hands back the token that puts it back. Must wrap the window
	///     placement too, not just the drawing: the placement reads the spacing for its margins.
	/// </summary>
	public static UiScale Push(ImGui gui)
	{
		ImStyleLayout layout = gui.Style.Layout;
		ImPadding padding = gui.Style.Window.ContentPadding;

		gui.Style.Layout.TextSize = layout.TextSize * Factor;
		gui.Style.Layout.ExtraRowHeight = layout.ExtraRowHeight * Factor;
		gui.Style.Layout.Spacing = layout.Spacing * Factor;
		gui.Style.Layout.InnerSpacing = layout.InnerSpacing * Factor;
		gui.Style.Layout.Indent = layout.Indent * Factor;

		gui.Style.Window.ContentPadding.Left = padding.Left * Factor;
		gui.Style.Window.ContentPadding.Right = padding.Right * Factor;
		gui.Style.Window.ContentPadding.Top = padding.Top * Factor;
		gui.Style.Window.ContentPadding.Bottom = padding.Bottom * Factor;

		return new UiScale(gui, layout, padding);
	}

	public void Dispose()
	{
		if (_gui == null)
		{
			return;
		}

		_gui.Style.Layout = _layout;
		_gui.Style.Window.ContentPadding = _padding;
	}
}
