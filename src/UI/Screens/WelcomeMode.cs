using System.Collections.Generic;
using AuthorTimeHunting.Gamemodes;

namespace AuthorTimeHunting.UI.Screens;

/// <summary>
///     One gamemode as the welcome screen draws it: the name, the one-liner, and the rules
///     already turned into list items. A gamemode describes itself in plain sentences and
///     knows nothing about dashes - that is this screen's idea of how a list looks, so this is
///     where it is applied, once.
/// </summary>
public readonly struct WelcomeMode
{
	public WelcomeMode(IGamemode mode)
	{
		Name = mode.DisplayName;
		Description = mode.Description;

		IReadOnlyList<string> rules = mode.Rules;
		Rules = new string[rules.Count];

		for (int i = 0; i < rules.Count; i++)
		{
			Rules[i] = $"- {rules[i]}";
		}
	}

	public string Name { get; }
	public string Description { get; }
	public string[] Rules { get; }
}
