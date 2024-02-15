/*
 *using Ignist.Data;
using Ignist.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;

User
jeg får ikke get metoden min til å fungeree jeg får denne feilmelding Publication not found. min partion nøkkel i cosmos db er userid jeg kan dele med deg koden min kan du sjejkke? using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ignist.Models;

namespace Ignist.Data
{
    public interface IPublicationsRepository
    {
        Task<IEnumerable<Publication>> GetAllPublicationsAsync();
        Task<Publication> GetPublicationByIdAsync(string id);
        Task AddPublicationAsync(Publication publication);
        Task UpdatePublicationAsync(Publication publication);
        Task DeletePublicationAsync(string id, string UserId);
    }
}
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
            var query = _publicationContainer.GetItemLinqQueryable<Publication>(true)
                .ToFeedIterator();

            var results = new List<Publication>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }
            return results;
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
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Ignist.Models;
using Ignist.Data;
using Microsoft.Azure.Cosmos;

namespace Ignist.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PublicationsController : ControllerBase // Endret til stor 'P' i navnet for konvensjonens skyld
    {
        private readonly IPublicationsRepository _publicationsRepository;

        public PublicationsController(IPublicationsRepository publicationsRepository)
        {
            _publicationsRepository = publicationsRepository;
        }

        // Get all Publications
        [HttpGet]
        public async Task<ActionResult<List<Publication>>> GetAllPublications()
        {
            var publications = await _publicationsRepository.GetAllPublicationsAsync();
            return Ok(publications);
        }

        // Finding a publication with the specific Id
        [HttpGet("{id}")]
        public async Task<ActionResult<Publication>> GetPublication(string id) // Endret til string fordi Cosmos DB bruker string IDs
        {
            var publication = await _publicationsRepository.GetPublicationByIdAsync(id);
            if (publication is null)
            {
                return NotFound("Publication not found.");
            }

            return Ok(publication);
        }

        //Creating new Publications
        [HttpPost]
        public async Task<ActionResult<Publication>> AddPublication(Publication publication)
        {
            await _publicationsRepository.AddPublicationAsync(publication);
            return CreatedAtAction(nameof(GetPublication), new { id = publication.Id }, publication);
        }

        // Updating a Publication
        [HttpPut("{id}")]
        public async Task<ActionResult<Publication>> UpdatePublication(string id, Publication updatedPublication)
        {
            try
            {
                await _publicationsRepository.UpdatePublicationAsync(updatedPublication);
                return NoContent();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound("Publication not found.");
            }
        }

        // Deleting a publication
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePublication(string id, [FromQuery] string UserId)
        {
            try
            {
                await _publicationsRepository.DeletePublicationAsync(id, UserId);
                return NoContent();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound("Publication not found.");
            }
        }
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Ignist.Models;
using Ignist.Data;
using Microsoft.Azure.Cosmos;

namespace Ignist.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PublicationsController : ControllerBase // Endret til stor 'P' i navnet for konvensjonens skyld
    {
        private readonly IPublicationsRepository _publicationsRepository;

        public PublicationsController(IPublicationsRepository publicationsRepository)
        {
            _publicationsRepository = publicationsRepository;
        }

        // Get all Publications
        [HttpGet]
        public async Task<ActionResult<List<Publication>>> GetAllPublications()
        {
            var publications = await _publicationsRepository.GetAllPublicationsAsync();
            return Ok(publications);
        }

        // Finding a publication with the specific Id
        [HttpGet("{id}")]
        public async Task<ActionResult<Publication>> GetPublication(string id) // Endret til string fordi Cosmos DB bruker string IDs
        {
            var publication = await _publicationsRepository.GetPublicationByIdAsync(id);
            if (publication is null)
            {
                return NotFound("Publication not found.");
            }

            return Ok(publication);
        }

        //Creating new Publications
        [HttpPost]
        public async Task<ActionResult<Publication>> AddPublication(Publication publication)
        {
            await _publicationsRepository.AddPublicationAsync(publication);
            return CreatedAtAction(nameof(GetPublication), new { id = publication.Id }, publication);
        }

        // Updating a Publication
        [HttpPut("{id}")]
        public async Task<ActionResult<Publication>> UpdatePublication(string id, Publication updatedPublication)
        {
            try
            {
                await _publicationsRepository.UpdatePublicationAsync(updatedPublication);
                return NoContent();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound("Publication not found.");
            }
        }

        // Deleting a publication
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePublication(string id, [FromQuery] string UserId)
        {
            try
            {
                await _publicationsRepository.DeletePublicationAsync(id, UserId);
                return NoContent();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound("Publication not found.");
            }
        }
    }
}

*/