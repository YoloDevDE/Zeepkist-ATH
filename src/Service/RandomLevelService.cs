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
///     the pool ran dry after a few runs and /ath start failed until the game was restarted.
/// </summary>
public class RandomLevelService
{
	private const int LevelBatchSize = 100;

	private readonly GraphQLService _graphQL;
	private readonly LocalLevelCacheService _localLevelCache;

	public RandomLevelService(GraphQLService graphQL, LocalLevelCacheService localLevelCache)
	{
		_graphQL = graphQL;
		_localLevelCache = localLevelCache;
	}

	private HashSet<string> FetchedLevelUids { get; } = new(StringComparer.OrdinalIgnoreCase);

	private List<LevelItem> CachedLevels { get; } = new();
	private List<LevelItem> PlayedLevels { get; } = new();

	/// <summary>
	///     How the pool is doing, for the debug panel. Counts rather than the lists themselves:
	///     the pool's contents are its own business, and handing them out would let a caller
	///     reach past <see cref="DrawRandomLevelAsync" /> and take a level without it being
	///     recorded as played.
	/// </summary>
	public int CachedCount => CachedLevels.Count;

	public int PlayedCount => PlayedLevels.Count;

	/// <summary>Every level UID this run has ever seen, drawn or not.</summary>
	public int FetchedCount => FetchedLevelUids.Count;

	/// <summary>Where the last batch came from, or null before the first fetch.</summary>
	public string LastSource { get; private set; }

	/// <summary>The level handed out last, so a draw can be checked without reading the log.</summary>
	public string LastDrawn { get; private set; }

	/// <summary>
	///     Draws a random level from the cached playlist and returns it as an
	///     <see cref="OnlineZeeplevel" />. Acts as a black box: if the cached list is
	///     currently empty it fetches until levels are available, then always takes the
	///     first entry. On success the level is removed from the cached list and moved
	///     into the played list.
	/// </summary>
	public async Task<OnlineZeeplevel> DrawRandomLevelAsync()
	{
		if (CachedLevels.Count == 0)
		{
			// No ConfigureAwait(false) anywhere in this chain: the local fallback below
			// calls PlaylistApi, Messenger and UnityEngine.Random, all of which are
			// main-thread only. Giving up Unity's SynchronizationContext here put the
			// whole fallback on a thread pool thread.
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

		Logger.LogInfo(
			$"RandomLevelService: Drew level '{level.Name}' (UID: {level.FileUid}). Cached remaining: {CachedLevels.Count}, played: {PlayedLevels.Count}.");
		return level.ToOnlineZeepLevel();
	}

	/// <summary>
	///     Fetches a batch of random levels, excluding levels that were already fetched during
	///     this program run. Tries GraphQL first and falls back to local playlists if GraphQL is
	///     unreachable or returns nothing. Throws if no levels can be provided at all.
	///     The tracking is in-memory only (not persistent).
	/// </summary>
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
			// GraphQLService keeps its ConfigureAwait(false) on the HTTP call itself -
			// everything after it in that method is pure parsing. This await is the one
			// that has to bring us back to the main thread.
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
			_localLevelCache.GetRandomLevelItems(LevelBatchSize, FetchedLevelUids);
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
