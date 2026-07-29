using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Util;
using ZeepkistNetworking;

namespace AuthorTimeHunting.Service;

public class RandomLevelService
{
    private const int LevelBatchSize = 100;

    private RandomLevelService() { }
    public static RandomLevelService Instance { get; } = new RandomLevelService();

    private HashSet<string> FetchedLevelUids { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private List<LevelItem> CachedLevels { get; } = new List<LevelItem>();
    private List<LevelItem> PlayedLevels { get; } = new List<LevelItem>();

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
            List<LevelItem> fetched = await GetRandomLevelsAsync().ConfigureAwait(false);
            CachedLevels.AddRange(fetched);
        }

        if (CachedLevels.Count == 0)
        {
            throw new InvalidOperationException("RandomLevelService: No cached levels available after fetching.");
        }

        LevelItem level = CachedLevels[0];
        CachedLevels.RemoveAt(0);
        PlayedLevels.Add(level);

        Logger.LogInfo($"RandomLevelService: Drew level '{level.Name}' (UID: {level.FileUid}). Cached remaining: {CachedLevels.Count}, played: {PlayedLevels.Count}.");
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
        List<LevelItem> newLevels = await TryFetchFromGraphQlAsync().ConfigureAwait(false);

        if (newLevels.Count == 0)
        {
            Logger.LogWarning("RandomLevelService: GraphQL unavailable or empty. Falling back to local playlists.");
            newLevels = FetchFromLocalPlaylists();
        }

        if (newLevels.Count == 0)
        {
            throw new InvalidOperationException("RandomLevelService: No levels available from GraphQL or local playlists.");
        }

        Track(newLevels);
        return newLevels;
    }

    private async Task<List<LevelItem>> TryFetchFromGraphQlAsync()
    {
        try
        {
            List<LevelItem> levels = await GraphQLService.Instance.GetRandomLevelAsync().ConfigureAwait(false);

            if (levels == null || levels.Count == 0)
            {
                Logger.LogWarning("RandomLevelService: GraphQL returned no levels.");
                return new List<LevelItem>();
            }

            List<LevelItem> newLevels = levels.Where(level => !string.IsNullOrEmpty(level?.FileUid) && !FetchedLevelUids.Contains(level.FileUid)).ToList();

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
        List<LevelItem> localLevels = LocalLevelCacheService.Instance.GetRandomLevelItems(LevelBatchSize, FetchedLevelUids);
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