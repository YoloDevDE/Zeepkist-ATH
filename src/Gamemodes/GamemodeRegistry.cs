using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthorTimeHunting.Gamemodes;

/// <summary>
///     Every mode the mod knows, and which one the next run will use.
///     Session-scoped and deliberately not persisted: a mode is chosen for a run, and a
///     player who last played Ranked should not silently start one tomorrow.
/// </summary>
public class GamemodeRegistry
{
	private readonly List<IGamemode> _modes =
	[
		// Order is the order the UI lists them in, and the first entry is the default.
		new ClassicGamemode()
	];

	public GamemodeRegistry()
	{
		Selected = _modes[0];
	}

	/// <summary>All known modes, in display order.</summary>
	public IReadOnlyList<IGamemode> All => _modes;

	/// <summary>
	///     The mode the next run will use. Set by /ath start &lt;mode&gt; and by the control
	///     panel; a run that has already started carries its own copy and ignores changes here.
	/// </summary>
	public IGamemode Selected { get; set; }

	/// <summary>Moves the selection on by one, wrapping. What the UI's picker button does.</summary>
	public void SelectNext()
	{
		int next = (_modes.IndexOf(Selected) + 1) % _modes.Count;
		Selected = _modes[next];
	}

	/// <summary>
	///     Finds a mode by the id a player typed. Case-insensitive, because nobody types
	///     "classic" the same way twice.
	/// </summary>
	public IGamemode Resolve(string id)
	{
		return string.IsNullOrWhiteSpace(id) ?
			null :
			_modes.FirstOrDefault(mode => string.Equals(mode.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>The known ids, for the "no such mode" message.</summary>
	public string IdList()
	{
		return string.Join(", ", _modes.Select(mode => mode.Id));
	}
}
