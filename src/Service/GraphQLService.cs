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
            _graphQLClient = new GraphQLHttpClient("https://graphql-beta.zeepki.st/", new NewtonsoftJsonSerializer());
            Console.WriteLine("GraphQL client initialized.");
        }
    }


    // Method to get a random level
    public async Task<LevelItem> GetRandomLevelAsync()
    {
        try
        {
            GraphQLRequest query = new GraphQLRequest
            {
                Query = @"
                   query GetLevelInfo {
                          zRtm(first: 1, pMaxAuthorTime: 180) {
                            nodes {
                              name
                              validationTimeAuthor
                              fileAuthor
                              fileUid
                              workshopId
                            }
                          }
                        }"
            };

            GraphQLResponse<Root> response = await _graphQLClient.SendQueryAsync<Root>(query);

            // Check if response or response.Data is null
            if (response.Data == null)
            {
                Console.WriteLine("Response.Data is null.");
                return null;
            }

            if (response.Data.ZRtm == null)
            {
                Console.WriteLine("Response.Data.ZRtm is null.");
                return null;
            }

            if (response.Data.ZRtm.Nodes == null)
            {
                Console.WriteLine("Response.Data.ZRtm.Nodes is null");
                return null;
            }

            if (response.Data.ZRtm.Nodes.Count == 0)
            {
                Console.WriteLine("Response.Data.ZRtm.Nodes is empty");
                return null;
            }

            // Convert Node to LevelItem
            Node node = response.Data.ZRtm.Nodes[0];
            LevelItem levelItem = new LevelItem
            {
                Name = node.Name,
                ValidationTimeAuthor = node.ValidationTimeAuthor,
                FileAuthor = node.FileAuthor,
                FileUid = node.FileUid,
                WorkshopId = ulong.Parse(node.WorkshopId)
            };

            return levelItem;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GraphQL GetRandomLevelAsync failed: {ex.Message}");
            throw;
        }
    }
}