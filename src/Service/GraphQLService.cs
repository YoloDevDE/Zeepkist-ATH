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
        if (_graphQLClient == null)
        {
            Logger.LogInfo("GraphQL client initialized.");
            _graphQLClient = new GraphQLHttpClient("https://graphql.zeepki.st/", new NewtonsoftJsonSerializer());
            Logger.LogInfo("GraphQL client initialized.");
        }
    }


    // Method to get a random level
    public async Task<List<LevelItem>> GetRandomLevelAsync(int amount = 2, int maxAuthorTime = 180)
    {
        try
        {
            GraphQLRequest query = new GraphQLRequest
            {
                Query = $$$"""
                           query GetRandomLevel {
                             zRtm(
                               pMaxAuthorTime: {{{maxAuthorTime}}}
                               pMinFinishes: 1
                               filter: {deleted: {equalTo: false}}
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

            GraphQLResponse<Root> response = await _graphQLClient.SendQueryAsync<Root>(query);

            if (response?.Data?.ZRtm?.Nodes?.Count == 0)
            {
                Logger.LogError("No level data found in response");
                return null;
            }

            if (!response?.Data?.ZRtm?.Nodes?.Any() ?? true)
            {
                Logger.LogError("Invalid response structure");
                return null;
            }

            // Convert all Nodes to LevelItems
            List<LevelItem> levelItems = response.Data.ZRtm.Nodes.Select(node => new LevelItem
            {
                Name = node.Name,
                ValidationTimeAuthor = node.ValidationTimeAuthor,
                ValidationTimeGold = node.ValidationTimeGold,
                FileAuthor = node.FileAuthor,
                FileUid = node.FileUid,
                AuthorId = ulong.Parse(node.AuthorId),
                WorkshopId = ulong.Parse(node.WorkshopId)
            }).ToList();

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