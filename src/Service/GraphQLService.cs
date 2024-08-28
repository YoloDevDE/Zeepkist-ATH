using System;
using System.Threading.Tasks;
using AuthorTimeHunting.Entities;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;

namespace AuthorTimeHunting.Service;

public class GraphQLService
{
    // The singleton instance
    private static readonly Lazy<GraphQLService> _instance = new Lazy<GraphQLService>(() => new GraphQLService());

    private GraphQLHttpClient _graphQLClient;

    // Private constructor to prevent direct instantiation
    private GraphQLService()
    {
        InitializeGraphQLClient();
    }

    // Public method to get the singleton instance
    public static GraphQLService Instance => _instance.Value;

    private void InitializeGraphQLClient()
    {
        if (_graphQLClient == null)
        {
            _graphQLClient = new GraphQLHttpClient("https://graphql.zeepkist-gtr.com", new NewtonsoftJsonSerializer());
            Console.WriteLine("GraphQL client initialized.");
        }
    }

    // Method to get the total count of levels
    private async Task<int> GetTotalCountAsync()
    {
        try
        {
            GraphQLRequest query = new GraphQLRequest
            {
                Query = @"
                    query GetTotalCount {
                        allLevels(first: 1) {
                            totalCount
                        }
                    }"
            };

            GraphQLResponse<AllLevelsResponse> response = await _graphQLClient.SendQueryAsync<AllLevelsResponse>(query);
            return response.Data.AllLevels.TotalCount;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GraphQL GetTotalCountAsync failed: {ex.Message}");
            throw;
        }
    }

    // Method to get a random level
    public async Task<LevelItem> GetRandomLevelAsync()
    {
        try
        {
            int totalCount = await GetTotalCountAsync();
            if (totalCount == 0)
            {
                Console.WriteLine("Total count is zero. No levels to fetch.");
                return null;
            }

            Random random = new Random();
            int randomOffset = random.Next(0, totalCount);

            GraphQLRequest query = new GraphQLRequest
            {
                Query = @"
                       query GetLevelInfo($offset: Int) {
                          allLevels(first: 1, offset: $offset) {
                            nodes {
                              levelItemsByIdLevel(condition: {deleted: false}) {
                                nodes {
                                  name
                                  workshopId
                                  fileAuthor
                                  fileUid
                                }
                              }
                            }
                          }
                        }
",
                Variables = new { offset = randomOffset }
            };

            GraphQLResponse<AllLevelsResponse> response = await _graphQLClient.SendQueryAsync<AllLevelsResponse>(query);

            // Check if response or response.Data is null
            if (response == null)
            {
                Console.WriteLine("Response is null.");
                return null;
            }

            if (response.Data == null)
            {
                Console.WriteLine("Response.Data is null.");
                return null;
            }

            if (response.Data.AllLevels == null)
            {
                Console.WriteLine("Response.Data.AllLevels is null.");
                return null;
            }

            if (response.Data.AllLevels.Nodes == null)
            {
                Console.WriteLine("Response.Data.AllLevels.Nodes is null");
                return null;
            }

            if (response.Data.AllLevels.Nodes.Count == 0)
            {
                Console.WriteLine("Response.Data.AllLevels.Nodes is empty");
                return null;
            }

            if (response.Data.AllLevels.Nodes[0].LevelItemsByIdLevel == null)
            {
                Console.WriteLine("LevelItemsByIdLevel is null.");
                return null;
            }

            if (response.Data.AllLevels.Nodes[0].LevelItemsByIdLevel.Nodes == null || response.Data.AllLevels.Nodes[0].LevelItemsByIdLevel.Nodes.Count == 0)
            {
                Console.WriteLine("LevelItemsByIdLevel.Nodes is null or empty.");
                return null;
            }

            LevelItem randomLevel = response.Data.AllLevels.Nodes[0].LevelItemsByIdLevel.Nodes[0];

            // Final null check before returning the result
            if (randomLevel == null)
            {
                Console.WriteLine("Random level is null.");
                return null;
            }

            return randomLevel;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GraphQL GetRandomLevelAsync failed: {ex.Message}");
            throw;
        }
    }
}