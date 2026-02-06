using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.Service;

public class RandomLevelService
{
    private bool _isInitializing;

    private RandomLevelService()
    {
        _ = InitializeAsync();
    }

    public static RandomLevelService Instance { get; } = new RandomLevelService();

    private List<LevelItem> CachedRandomLevelItems { get; } = new List<LevelItem>();
    private List<LevelItem> FetchedLevelItems { get; } = new List<LevelItem>();

    private async Task InitializeAsync()
    {
        if (_isInitializing)
        {
            return;
        }

        _isInitializing = true;
        try
        {
            await PopulateCachedRandomLevelItems(5);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error initializing RandomLevelService: {ex.Message}");
        }
        finally
        {
            _isInitializing = false;
        }
    }

    public LevelItem GetRandomLevelItem()
    {
        if (CachedRandomLevelItems.Count == 0)
        {
            PopulateCachedRandomLevelItemsSync();
        }

        LevelItem levelItem;
        do
        {
            levelItem = CachedRandomLevelItems[0];
            CachedRandomLevelItems.RemoveAt(0);

            if (CachedRandomLevelItems.Count <= 2)
            {
                // Starte das Auffüllen im Hintergrund
                _ = PopulateCachedRandomLevelItems(100);
            }
        } while (FetchedLevelItems.Any(x => x.FileUid == levelItem.FileUid) || levelItem.ValidationTimeAuthor > levelItem.ValidationTimeGold);

        FetchedLevelItems.Add(levelItem);
        return levelItem;
    }

    // Synchrone Methode als Fallback
    private void PopulateCachedRandomLevelItemsSync()
    {
        try
        {
            Task<List<LevelItem>> task = GraphQLService.Instance.GetRandomLevelAsync(5);
            task.Wait(); // Notwendiges Blocking in diesem Ausnahmefall

            List<LevelItem> levelItems = task.Result;
            levelItems = levelItems.Where(x => FetchedLevelItems.All(f => f.FileUid != x.FileUid)).ToList();
            CachedRandomLevelItems.AddRange(levelItems);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in PopulateCachedRandomLevelItemsSync: {ex.Message}");
        }
    }

    private async Task PopulateCachedRandomLevelItems(int amount = 2)
    {
        try
        {
            List<LevelItem> levelItems = await GraphQLService.Instance.GetRandomLevelAsync(amount);
            levelItems = levelItems.Where(x => FetchedLevelItems.All(f => f.FileUid != x.FileUid)).ToList();

            if (levelItems.Count > 0)
            {
                CachedRandomLevelItems.AddRange(levelItems);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in PopulateCachedRandomLevelItems: {ex.Message}");
        }
    }
}