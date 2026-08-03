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
	/// <summary>
	///     Lower-case, no spaces. This is what a player types after /ath start, so it is part
	///     of the mod's interface and should not change once it has shipped.
	/// </summary>
	string Id { get; }

	/// <summary>The name shown in the UI, e.g. "ATH Solo Hunt (Classic)".</summary>
	string DisplayName { get; }

	/// <summary>One line, for the mode picker and the chat command's help.</summary>
	string Description { get; }

	/// <summary>
	///     The mode explained, one rule per line, for the welcome screen. A mode has to be
	///     readable before it is played: the numbers it hands to <see cref="CreateSettings" />
	///     are the rules, and nothing in the HUD ever says what they were.
	///     Written out rather than generated from <see cref="RunSettings" />, because "levels no
	///     longer than a three minute author time" is a rule the settings object does not carry -
	///     it lives in the query the level pool is drawn with.
	/// </summary>
	IReadOnlyList<string> Rules { get; }

	/// <summary>
	///     The rules for a run about to start. Called once per run, so a mode is free to read
	///     the player's config here - the result is a snapshot and cannot change mid-run.
	/// </summary>
	RunSettings CreateSettings();
}
