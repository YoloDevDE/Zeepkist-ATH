using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.UI;

/// <summary>
///     Renders a <see cref="PanelView" /> back into the chat message it used to be, for
///     players who turn the in-game HUD off.
///     This exists so the wording lives in exactly one place. Before the panel model, the
///     chat layout and the content were the same code, so any change to one was a change to
///     the other.
/// </summary>
public static class PanelChatRenderer
{
	public static string Render(PanelView panel)
	{
		Message.Builder message = new Message.Builder().ClearLines();

		if (!string.IsNullOrEmpty(panel.Title))
		{
			message.AddLine(Colour(panel.Title, panel.TitleColour)).AddBreakSpace();
		}

		foreach (PanelBlock block in panel.Blocks)
		{
			switch (block.Kind)
			{
				case PanelBlockKind.Heading:
					message.AddSeperator(Colour(block.Label, block.LabelColour)).AddBreakSpace();
					break;

				case PanelBlockKind.Line:
					message.AddLine(Colour(Escape(block.Label), block.LabelColour)).AddBreakSpace();
					break;

				case PanelBlockKind.Row:
					message.AddKeyValue(Colour(block.Label, block.LabelColour),
						Colour(Escape(block.Value), block.ValueColour)).AddBreakSpace();
					break;
			}
		}

		return message.Build().ToString();
	}

	/// <summary>
	///     Lines and row values can carry level names and author names, i.e. text a stranger
	///     chose. Without noparse a level called "&lt;color=red&gt;" would recolour the chat.
	///     Headings and the title are ATH's own text and stay unescaped so emotes still work.
	/// </summary>
	private static string Escape(string text)
	{
		return string.IsNullOrEmpty(text) ? text : $"<noparse>{text}</noparse>";
	}

	private static string Colour(string text, Color32 colour)
	{
		return $"<#{colour.r:X2}{colour.g:X2}{colour.b:X2}>{text}</color>";
	}
}
