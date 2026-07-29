using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Util;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;

namespace AuthorTimeHunting.Service;

public class GraphQLService
{
    // The singleton instance

    private GraphQLHttpClient _graphQLClient;

    // Private constructor to prevent direct instantiation
    private GraphQLService()
    {
        InitializeGraphQLClient();
    }

    // Public method to get the singleton instance
    public static GraphQLService Instance { get; } = new GraphQLService();


    private void InitializeGraphQLClient()
    {
        string graphQlUrl = Plugin.Instance.MyConfig.GraphQlUrl.Value;
        Logger.LogInfo($"Initializing GraphQL client at '{graphQlUrl}'");

        if (_graphQLClient == null)
        {
            _graphQLClient = new GraphQLHttpClient(graphQlUrl, new NewtonsoftJsonSerializer());
            Logger.LogInfo("GraphQL client initialized.");
        }
    }


    // Method to get a random level
    public async Task<List<LevelItem>> GetRandomLevelAsync(int amount = 100, int maxAuthorTime = 180)
    {
        try
        {
            Logger.LogInfo($"Fetching {amount} random levels with max author time of {maxAuthorTime} seconds.");
            GraphQLRequest query = new GraphQLRequest
            {
                Query = $$$"""
                           query GetRandomLevel {
                             zRtm(
                               pMaxAuthorTime: {{{maxAuthorTime}}}
                               filter: {deleted: {equalTo: false}, amountFinishes: {notEqualTo: 0}}
                               first: {{{amount}}}
                             ) {
                               nodes {
                                 name
                                 validationTimeAuthor
                                 validationTimeGold
                                 fileAuthor
                                 fileUid
                                 workshopId
                                 authorId
                               }
                             }
                           }
                           """
            };
            GraphQLResponse<Root> response = await _graphQLClient.SendQueryAsync<Root>(query).ConfigureAwait(false);
            Logger.LogInfo($"Fetched {amount} random levels with max author time of {maxAuthorTime} seconds.");

            if (response.Errors != null && response.Errors.Any())
            {
                Logger.LogError($"GraphQL response contains errors: {string.Join(", ", response.Errors.Select(e => e.Message))}");
                return null;
            }

            if (response.Data?.ZRtm?.Nodes == null)
            {
                Logger.LogError("Invalid response structure: ZRtm or Nodes is null");
                return null;
            }

            if (response.Data.ZRtm.Nodes.Count == 0)
            {
                Logger.LogError("No level data found in response");
                return null;
            }

            // Convert all Nodes to LevelItems safely
            List<LevelItem> levelItems = new List<LevelItem>();

            foreach (Node node in response.Data.ZRtm.Nodes)
            {
                if (node == null)
                {
                    continue;
                }

                ulong authorId = 0;
                ulong workshopId = 0;

                if (!string.IsNullOrEmpty(node.AuthorId))
                {
                    ulong.TryParse(node.AuthorId, out authorId);
                }

                if (!string.IsNullOrEmpty(node.WorkshopId))
                {
                    ulong.TryParse(node.WorkshopId, out workshopId);
                }

                levelItems.Add(new LevelItem
                {
                    Name = node.Name, ValidationTimeAuthor = node.ValidationTimeAuthor, ValidationTimeGold = node.ValidationTimeGold, FileAuthor = node.FileAuthor, FileUid = node.FileUid, AuthorId = authorId, WorkshopId = workshopId
                });
            }

            Logger.LogInfo($"Successfully retrieved {levelItems.Count} random levels");
            return levelItems;
        }
        catch (Exception ex)
        {
            Logger.LogError($"GraphQL GetRandomLevelAsync failed: {ex.Message}");
            throw;
        }
    }
}