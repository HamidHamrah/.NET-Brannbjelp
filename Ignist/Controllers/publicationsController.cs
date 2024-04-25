using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Ignist.Models;
using Ignist.Data;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Authorization;
using Serilog;
using System.Security.Claims;

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
            try
            {
                var publications = await _publicationsRepository.GetAllPublicationsAsync();
                return Ok(publications);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving publications. please try again later.");
            }
        }

        // Finding a publication with the specific Id
        [HttpGet("{id}")]
        public async Task<ActionResult<Publication>> GetPublication(string id, [FromQuery] string UserId) // Endret til string fordi Cosmos DB bruker string IDs
        {
            var publication = await _publicationsRepository.GetPublicationByIdAsync(id, UserId);
            if (publication is null)
            {
                return NotFound("Publication not found.");
            }

            return Ok(publication);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Publication>> AddPublication(Publication publication)
        {
            Log.Information("Admin {AdminId} attempting to add a new publication", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            try
            {
                await _publicationsRepository.AddPublicationAsync(publication);
                Log.Information("Publication with ID {PublicationId} added successfully", publication.Id);

                return CreatedAtAction(nameof(GetPublication), new { id = publication.Id }, publication);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add publication for Admin {AdminId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                throw;  // Re-throw the exception to be handled by the global error handler or middleware
            }
        }


        private async Task AddPublicationRecursive(Publication publication, string parentId = null)
        {
            // Log the initiation of the addition process, note if it's a root or a child publication
            if (parentId == null)
            {
                Log.Information("Starting recursive addition of root publication with Title: {Title}", publication.Title);
            }
            else
            {
                Log.Information("Adding child publication with Title: {Title} under Parent ID: {ParentId}", publication.Title, parentId);
                publication.ParentId = parentId;
            }

            try
            {
                await _publicationsRepository.AddPublicationAsync(publication);
                Log.Debug("Successfully added publication with ID {PublicationId}", publication.Id);

                // Recursively add each child publication
                foreach (var childPublication in publication.ChildPublications)
                {
                    await AddPublicationRecursive(childPublication, publication.Id);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add publication with Title: {Title} and Parent ID: {ParentId}", publication.Title, parentId);
                throw; // Re-throw to handle the error further up the stack
            }
        }



        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Publication>> UpdatePublication(string id, Publication updatedPublication)
        {
            Log.Information("Admin {AdminId} attempting to update publication with ID {PublicationId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);

            if (string.IsNullOrEmpty(updatedPublication.Id) || updatedPublication.Id != id)
            {
                Log.Warning("Mismatched ID in update request for publication. Request ID: {RequestId}, Publication ID: {PublicationId}", id, updatedPublication.Id);
                return BadRequest("The ID of the publication does not match the request.");
            }

            // Optionally, add more checks or logic here, e.g., validate parentId if necessary

            try
            {
                await _publicationsRepository.UpdatePublicationAsync(updatedPublication);
                Log.Information("Publication with ID {PublicationId} updated successfully", id);
                return NoContent();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Log.Error(ex, "Failed to find publication with ID {PublicationId} for updating", id);
                return NotFound("Publication not found.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An unexpected error occurred while updating publication with ID {PublicationId}", id);
                throw; // re-throw the exception to be handled by the global exception handler
            }
        }



        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePublication(string id, [FromQuery] string UserId)
        {
            Log.Information("Admin {AdminId} attempting to delete publication with ID {PublicationId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);

            try
            {
                await _publicationsRepository.DeletePublicationAsync(id, UserId);
                Log.Information("Publication with ID {PublicationId} deleted successfully by Admin {AdminId}", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return NoContent();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Log.Warning(ex, "Failed to delete publication with ID {PublicationId}: Publication not found", id);
                return NotFound("Publication not found.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An unexpected error occurred while deleting publication with ID {PublicationId} by Admin {AdminId}", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                throw; // Re-throw the exception to be handled by the global exception handler or middleware
            }
        }

    }
}
