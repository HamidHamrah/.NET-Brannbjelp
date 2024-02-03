using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Ignist.Models;
using Ignist.Data;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Ignist.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class publicationsController : ControllerBase
    {
        private readonly DataContext _DataContext;
        public publicationsController(DataContext dataContext)
        {
            _DataContext = dataContext;
        }
        //Get all Publications
        [HttpGet]
        public async Task<ActionResult<List<publications>>> GetAllPublications()
        {
            var publications = await _DataContext.publications.ToListAsync();
            return Ok(publications);
        }

        // Finding a publication with the specific Id
        [HttpGet("{id}")]
        public async Task<ActionResult<publications>> GetPublication(int id)
        {
            var publication = await _DataContext.publications.FindAsync(id);
            if (publication is null)
            {
                return NotFound("Publication not found.");
            }

            return Ok(publication);
        }

        //Creating new Publications
        [HttpPost]
        public async Task<ActionResult<publications>> AddPublication(publications publication)
        {
            _DataContext.publications.Add(publication);
            await _DataContext.SaveChangesAsync();

            return Ok(await _DataContext.publications.ToListAsync());
        }

        //Updating an Uplication

        [HttpPut]
        public async Task<ActionResult<publications>> UpdatePublication(publications updatedpublication)
        {
            var dbPublication = await _DataContext.publications.FindAsync(updatedpublication.Id);
            if (dbPublication is null)
                return NotFound("Publication not found.");
            dbPublication.Tittle = updatedpublication.Tittle;
            dbPublication.Contetn = updatedpublication.Contetn;
            dbPublication.CreatedAT = updatedpublication.CreatedAT;
            dbPublication.UpdatedAt = updatedpublication.UpdatedAt;
            await _DataContext.SaveChangesAsync();

            return Ok(await _DataContext.publications.ToListAsync());
        }

        // Deleting a publication

        [HttpDelete]
        public async Task<ActionResult<publications>>  DeletPublication(int Id)
        {
            var dbPublication = await _DataContext.publications.FindAsync(Id);
            if (dbPublication is null)
                return NotFound("Publication not found.");
            _DataContext.publications.Remove(dbPublication);

            await _DataContext.SaveChangesAsync();

            return Ok(await _DataContext.publications.ToListAsync());
        }
    }
}

