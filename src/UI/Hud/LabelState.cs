using TMPro;
using UnityEngine;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>What a label looked like before ATH took it over.</summary>
public readonly struct LabelState
{
	public LabelState(TextOverflowModes overflow, bool autoSizing, float fontSize, bool wordWrapping,
		Vector2 position, TMP_SpriteAsset spriteAsset)
	{
		Overflow = overflow;
		AutoSizing = autoSizing;
		FontSize = fontSize;
		WordWrapping = wordWrapping;
		Position = position;
		SpriteAsset = spriteAsset;
	}

	public TextOverflowModes Overflow { get; }
	public bool AutoSizing { get; }
	public float FontSize { get; }
	public bool WordWrapping { get; }
	public Vector2 Position { get; }

	/// <summary>Normally none at all - a label picks TMP's default until it is handed its own.</summary>
	public TMP_SpriteAsset SpriteAsset { get; }
}
