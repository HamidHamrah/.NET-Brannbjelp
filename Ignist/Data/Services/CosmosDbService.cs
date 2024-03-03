using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using User = Ignist.Models.User;

namespace Ignist.Data.Services
{
    public class CosmosDbService : ICosmosDbService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;

        public CosmosDbService(CosmosClient cosmosClient, IConfiguration configuration)
        {
            _cosmosClient = cosmosClient;
            var databaseName = configuration["CosmosDbSettings:DatabaseName"];
            var containerName = "User2"; 
            _container = _cosmosClient.GetContainer(databaseName, containerName);
        }

        // Henter en bruker basert på brukernavn, som er partition key
        public async Task<User> GetUserByEmailAsync(string email)
        {
            var query = new QueryDefinition("select * from c where c.email = @email")
                .WithParameter("@email", email);

            var iterator = _container.GetItemQueryIterator<User>(query, requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(email)
            });

            List<User> matches = new List<User>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                matches.AddRange(response.ToList());
            }

            return matches.FirstOrDefault();
        }


        // metoden for å lage ny bruker
        public async Task AddUserAsync(User user)
        {
            await _container.CreateItemAsync(user, new PartitionKey(user.Email));
        }
    }
}
