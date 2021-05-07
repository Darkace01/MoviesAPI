using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoviesAPI.Data;
using MoviesAPI.Entities;
using MoviesAPI.Filters;

namespace MoviesAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenresController : ControllerBase
    {
        private readonly ILogger<GenresController> _logger;
        private readonly ApplicationDbContext _ctx;
        public GenresController(ILogger<GenresController> logger, ApplicationDbContext ctx)
        {
            _logger = logger;
            _ctx = ctx;
        }
        [HttpGet]
        public async Task<ActionResult<List<Genre>>> Get()
        {
            return await _ctx.Genres.ToListAsync();
        }

        [HttpGet("{id:int}")]
        public ActionResult<Genre> Get(int id)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] Genre genre)
        {
            _ctx.Add(genre);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut]
        public ActionResult Put()
        {
            return NoContent();
        }

        [HttpDelete]
        public ActionResult Delete()
        {
            return NoContent();
        }
    }
}