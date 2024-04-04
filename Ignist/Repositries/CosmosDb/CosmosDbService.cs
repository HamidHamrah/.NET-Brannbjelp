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

        public async Task<User> GetUserByIdAsync(string userId)
        {
            // Note: This query does not use a partition key, which can impact performance and cost
            var query = new QueryDefinition("select * from c where c.id = @userId").WithParameter("@userId", userId);

            var iterator = _container.GetItemQueryIterator<User>(query);

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

        public async Task UpdateUserAsync(User user)
        {
            await _container.UpsertItemAsync(user, new PartitionKey(user.Email));
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            var container = _cosmosClient.GetContainer("data3900", "User2"); //her må man oppgi manuelt database navn og container
            var query = "SELECT * FROM c"; 
            var queryIterator = container.GetItemQueryIterator<User>(query);
            var users = new List<User>();

            while (queryIterator.HasMoreResults)
            {
                var response = await queryIterator.ReadNextAsync();
                users.AddRange(response.ToList());
            }

            return users;
        }
        public async Task DeleteUserAsync(string email)
        {
            try
            {
                // Forsøker å hente brukeren først for å sikre at den eksisterer
                var user = await GetUserByEmailAsync(email);
                if (user == null)
                {
                    throw new ArgumentException("User not found.");
                }

                // Sletter brukeren basert på e-postadressen, som er partition key
                await _container.DeleteItemAsync<User>(user.Id, new PartitionKey(email));
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new ArgumentException($"No user found with email: {email}");
            }
        }
    }
}
