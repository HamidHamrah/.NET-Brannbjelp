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
            var containerName = "LearnSmart";
            _publicationContainer = cosmosClient.GetContainer(databaseName, containerName);
        }

        public async Task<IEnumerable<Publication>> GetAllPublicationsAsync()
        {
            var query = _publicationContainer.GetItemLinqQueryable<Publication>(true).ToFeedIterator();

            var results = new List<Publication>();
            var processedIds = new HashSet<string>(); // Keeps track of processed publications to avoid duplicates

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response);
            }

            // Initialize the hierarchy
            var publicationsWithHierarchy = InitializeHierarchy(results, processedIds);

            return publicationsWithHierarchy;
        }

        private List<Publication> InitializeHierarchy(IEnumerable<Publication> publications, HashSet<string> processedIds)
        {
            var rootPublications = publications.Where(p => string.IsNullOrEmpty(p.ParentId)).ToList();

            foreach (var root in rootPublications)
            {
                BuildHierarchy(root, publications, processedIds);
            }

            return rootPublications;
        }

        private void BuildHierarchy(Publication current, IEnumerable<Publication> allPublications, HashSet<string> processedIds)
        {
            if (!processedIds.Add(current.Id)) // Check if already processed, and skip if true
                return;

            var children = allPublications.Where(p => p.ParentId == current.Id).ToList();
            foreach (var child in children)
            {
                BuildHierarchy(child, allPublications, processedIds); // Recursively build hierarchy for each child
            }
            current.ChildPublications = children; // Set the child publications
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
