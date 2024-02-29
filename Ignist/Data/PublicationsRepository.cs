using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using Ignist.Models;
using Ignist.Data;

namespace Ignist.Data
{
    public class PublicationsRepository : IPublicationsRepository
    {
        private readonly CosmosClient cosmosClient;
        private readonly IConfiguration configuration;
        private readonly Container _publicationContainer;

        public PublicationsRepository(CosmosClient cosmosClient, IConfiguration configuration)
        {
            this.cosmosClient = cosmosClient;
            this.configuration = configuration;
            var databaseName = configuration["CosmosDbSettings:DatabaseName"];
            var containerName = "Hamud";
            _publicationContainer = cosmosClient.GetContainer(databaseName, containerName);
        }

        public async Task<IEnumerable<Publication>> GetAllPublicationsAsync()
        {
            var query = _publicationContainer.GetItemLinqQueryable<Publication>(true)
                        .ToFeedIterator();

            var results = new List<Publication>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            // Assuming you adjust your model or handling to fetch child publications
            foreach (var publication in results)
            {
                publication.ChildPublications = await GetChildPublicationsAsync(publication.Id);
            }

            return results;
        }
        public async Task<List<Publication>> GetChildPublicationsAsync(string parentId)
        {
            // This query filters publications based on the parentId property
            var query = _publicationContainer.GetItemLinqQueryable<Publication>(true)
                        .Where(p => p.ParentId == parentId)
                        .ToFeedIterator();

            var children = new List<Publication>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                children.AddRange(response.ToList());
            }

            // Optionally, recursively fetch children of these children if deeper hierarchy is needed
            // Be cautious of potential performance impacts with deep recursion
            foreach (var child in children)
            {
                child.ChildPublications = await GetChildPublicationsAsync(child.Id);
            }

            return children;
        }



        public async Task<Publication> GetPublicationByIdAsync(string id)
        {
            try
            {
                var response = await _publicationContainer.ReadItemAsync<Publication>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }
        //Hamid
        public async Task<Publication> GetLastPublicationAsync()
        {
            var query = _publicationContainer.GetItemLinqQueryable<Publication>()
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(1);

            var iterator = query.ToFeedIterator();
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault();
            }

            return null;
        }
        //
        public async Task AddPublicationAsync(Publication publication)
        {
            await _publicationContainer.CreateItemAsync(publication, new PartitionKey(publication.UserId));
        }


        public async Task UpdatePublicationAsync(Publication publication)
        {
            await _publicationContainer.UpsertItemAsync(publication, new PartitionKey(publication.UserId));
        }

        public async Task DeletePublicationAsync(string id, string UserId)
        {
            await _publicationContainer.DeleteItemAsync<Publication>(id, new PartitionKey(UserId));
        }

        public async Task<Publication> GetPublicationByIdAsync(string id, string UserId)
        {
            try
            {
                // Bruk userId som PartitionKey for å lese et dokument
                var response = await _publicationContainer.ReadItemAsync<Publication>(id, new PartitionKey(UserId));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

        }
    }
}
