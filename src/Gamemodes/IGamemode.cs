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
	///     The rules for a run about to start. Called once per run, so a mode is free to read
	///     the player's config here - the result is a snapshot and cannot change mid-run.
	/// </summary>
	RunSettings CreateSettings();
}