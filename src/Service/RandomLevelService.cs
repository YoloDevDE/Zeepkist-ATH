using System;
using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.Service;

public class RandomLevelService
{
    private static readonly Lazy<RandomLevelService> _lazyInstance = new Lazy<RandomLevelService>(() => new RandomLevelService());

    private RandomLevelService() { }

    public static RandomLevelService Instance => _lazyInstance.Value;

    private List<string> FetchedLevelUids { get; } = new List<string>();

    /// <summary>
    ///     Peeks at whether a valid next level exists (does not consume/mark it as fetched).
    ///     Optionally also excludes a specific UID (e.g. the current level that would be a duplicate).
    /// </summary>
    public bool HasValidNextLevel(string excludedUid = null)
    {
        List<string> excluded = new List<string>(FetchedLevelUids);

        if (!string.IsNullOrEmpty(excludedUid) && !excluded.Contains(excludedUid))
        {
            excluded.Add(excludedUid);
        }

        LevelItem peek = LocalLevelCacheService.Instance.GetRandomLevelItem(excluded);
        return peek != null;
    }

    public LevelItem GetRandomLevelItem()
    {
        LevelItem levelItem = LocalLevelCacheService.Instance.GetRandomLevelItem(FetchedLevelUids);

        if (levelItem == null)
        {
            Logger.LogError("RandomLevelService: Could not get a random level from local cache.");
            return null;
        }

        FetchedLevelUids.Add(levelItem.FileUid);
        Logger.LogInfo($"RandomLevelService: Selected level '{levelItem.Name}' (UID: {levelItem.FileUid})");
        return levelItem;
    }

    public void Reset()
    {
        FetchedLevelUids.Clear();
        Logger.LogInfo("RandomLevelService: Session reset, played level list cleared.");
    }
}