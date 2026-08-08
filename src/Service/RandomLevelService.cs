using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Util;
using ZeepkistNetworking;

namespace AuthorTimeHunting.Service;

/// <summary>
///     The pool of levels a run draws from. One instance per run, created by AthStateMachine
///     - the exclusions it tracks are a within-run rule, and a fresh run starts from a clean
///     pool.
///     This used to be a process-wide singleton with no way to clear it, which meant every
///     run inherited the exclusions of all previous ones. With local playlists as the source
///     the pool ran dry after a few runs and starting a hunt failed until the game was restarted.
/// </summary>
public class RandomLevelService
{
	private const int _levelBatchSize = 100;

	private readonly GraphQLService _graphQL;
	private readonly LocalLevelCacheService _localLevelCache;

	public RandomLevelService(GraphQLService graphQL, LocalLevelCacheService localLevelCache)
	{
		_graphQL = graphQL;
		_localLevelCache = localLevelCache;
	}

	private HashSet<string> FetchedLevelUids { get; } = new(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, string> DrawnFrom { get; } = new(StringComparer.OrdinalIgnoreCase);

	private List<LevelItem> CachedLevels { get; } = new();
	private List<LevelItem> PlayedLevels { get; } = new();

	public int CachedCount => CachedLevels.Count;

	public int PlayedCount => PlayedLevels.Count;

	public int FetchedCount => FetchedLevelUids.Count;

	public string LastSource { get; private set; }

	public string LastDrawn { get; private set; }

	/// <summary>
	///     Where the level with this uid came from, or null if this pool never handed it out -
	///     which is the answer whenever the lobby's own playlist is being played.
	/// </summary>
	public string SourceOf(string levelUid)
	{
		if (string.IsNullOrEmpty(levelUid))
		{
			return null;
		}

		return DrawnFrom.TryGetValue(levelUid, out string source) ? source : null;
	}

	/// <summary>
	///     What this pool knows about the level with this uid, or null if it never handed it out.
	///     The lobby's playlist carries a level's name and author and nothing else, so this is the
	///     only place the author and gold times of a level that has not loaded yet can come from -
	///     and only for the levels the pool drew itself, with the times attached.
	/// </summary>
	public LevelItem Drawn(string levelUid)
	{
		if (string.IsNullOrEmpty(levelUid))
		{
			return null;
		}

		return PlayedLevels.FirstOrDefault(level =>
			string.Equals(level.FileUid, levelUid, StringComparison.OrdinalIgnoreCase));
	}

	public async Task<OnlineZeeplevel> DrawRandomLevelAsync()
	{
		if (CachedLevels.Count == 0)
		{
			List<LevelItem> fetched = await GetRandomLevelsAsync();
			CachedLevels.AddRange(fetched);
		}

		if (CachedLevels.Count == 0)
		{
			throw new InvalidOperationException("RandomLevelService: No cached levels available after fetching.");
		}

		LevelItem level = CachedLevels[0];
		CachedLevels.RemoveAt(0);
		PlayedLevels.Add(level);
		LastDrawn = level.Name;
		DrawnFrom[level.FileUid] = LastSource;

		Logger.LogInfo(
			$"RandomLevelService: Drew '{level.Name}' by {level.FileAuthor} (UID {level.FileUid}, workshop {level.WorkshopId}) from {LastSource}. Cached remaining: {CachedLevels.Count}, played: {PlayedLevels.Count}.");
		return level.ToOnlineZeepLevel();
	}

	public async Task<List<LevelItem>> GetRandomLevelsAsync()
	{
		List<LevelItem> newLevels = await TryFetchFromGraphQlAsync();
		LastSource = "GraphQL";

		if (newLevels.Count == 0)
		{
			Logger.LogWarning("RandomLevelService: GraphQL unavailable or empty. Falling back to local playlists.");
			newLevels = FetchFromLocalPlaylists();
			LastSource = "local playlists";
		}

		if (newLevels.Count == 0)
		{
			throw new InvalidOperationException(
				"RandomLevelService: No levels available from GraphQL or local playlists.");
		}

		Track(newLevels);
		return newLevels;
	}

	private async Task<List<LevelItem>> TryFetchFromGraphQlAsync()
	{
		try
		{
			List<LevelItem> levels = await _graphQL.GetRandomLevelAsync();

			if (levels == null || levels.Count == 0)
			{
				Logger.LogWarning("RandomLevelService: GraphQL returned no levels.");
				return new List<LevelItem>();
			}

			List<LevelItem> newLevels = levels.Where(level =>
				!string.IsNullOrEmpty(level?.FileUid) && !FetchedLevelUids.Contains(level.FileUid)).ToList();

			Logger.LogInfo($"RandomLevelService: Fetched {newLevels.Count} new levels from GraphQL.");
			return newLevels;
		}
		catch (Exception ex)
		{
			Logger.LogError($"RandomLevelService: GraphQL fetch failed: {ex.Message}");
			return new List<LevelItem>();
		}
	}

	private List<LevelItem> FetchFromLocalPlaylists()
	{
		List<LevelItem> localLevels =
			_localLevelCache.GetRandomLevelItems(_levelBatchSize, FetchedLevelUids);
		Logger.LogInfo($"RandomLevelService: Fetched {localLevels.Count} new levels from local playlists.");
		return localLevels;
	}

	private void Track(IEnumerable<LevelItem> levels)
	{
		foreach (LevelItem level in levels)
		{
			FetchedLevelUids.Add(level.FileUid);
		}
	}
}
