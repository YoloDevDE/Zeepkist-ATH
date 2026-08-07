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
		new ClassicGamemode()
	];

	public GamemodeRegistry()
	{
		Selected = _modes[0];
		DisplayNames = _modes.Select(mode => mode.DisplayName).ToArray();
	}

	public IReadOnlyList<IGamemode> All => _modes;

	/// <summary>What a dropdown lists, in the order of <see cref="All" />. The list never changes.</summary>
	public string[] DisplayNames { get; }

	public IGamemode Selected { get; set; }

	public void SelectNext()
	{
		int next = (_modes.IndexOf(Selected) + 1) % _modes.Count;
		Selected = _modes[next];
	}

	public IGamemode Resolve(string id)
	{
		return string.IsNullOrWhiteSpace(id) ?
			null :
			_modes.FirstOrDefault(mode => string.Equals(mode.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
	}

	public string IdList()
	{
		return string.Join(", ", _modes.Select(mode => mode.Id));
	}
}
