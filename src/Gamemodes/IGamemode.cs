using System.Collections.Generic;

namespace AuthorTimeHunting.Gamemodes;

/// <summary>
///     One way of playing ATH. A mode is an identity plus the rules a run starts with; the
///     run's state machine is the same for all of them.
///     Adding a mode is one file implementing this and one line in
///     <see cref="GamemodeRegistry" />. That is the whole point of the interface - before it,
///     "the rules" were config reads spread through the states, so a second mode would have
///     meant a second set of ifs in each of them.
/// </summary>
public interface IGamemode
{
	string Id { get; }

	string DisplayName { get; }

	string Description { get; }

	IReadOnlyList<string> Rules { get; }

	RunSettings CreateSettings();
}
