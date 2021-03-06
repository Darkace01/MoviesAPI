using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesAPI.Data;
using MoviesAPI.DTOs;
using MoviesAPI.Entities;
using MoviesAPI.Helpers;

namespace MoviesAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdmin")]
    public class ActorsController : ControllerBase
    {
        private readonly ApplicationDbContext _ctx;
        private readonly IMapper _mapper;
        private readonly IFileStorageService _fileStorageService;
        private readonly string containerName = "actors";

        public ActorsController(ApplicationDbContext ctx, IMapper mapper, IFileStorageService fileStorageService)
        {
            _ctx = ctx;
            _mapper = mapper;
            _fileStorageService = fileStorageService;
        }

        [HttpGet]
        public async Task<ActionResult<List<ActorDTO>>> Get([FromQuery] PaginationDTO paginationDTO)
        {
            var queryable = _ctx.Actors.AsQueryable();
            await HttpContext.InsertParametersPaginationInHeader(queryable);
            var actors = await queryable.OrderBy(x => x.Name).Paginate(paginationDTO).ToListAsync();
            return _mapper.Map<List<ActorDTO>>(actors);
        }

        [HttpPost("searchByName")]
        public async Task<ActionResult<List<ActorMovieDTO>>> SearchByName([FromBody] string name)
        {
            if (string.IsNullOrEmpty(name)) { return new List<ActorMovieDTO>(); }

            return await _ctx.Actors
                    .Where(x => x.Name.Contains(name))
                    .OrderBy(x => x.Name)
                    .Select(x => new ActorMovieDTO { Id = x.Id, Name = x.Name, Picture = x.Picture })
                    .Take(5)
                    .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ActorDTO>> Get(int id)
        {
            var actors = await _ctx.Actors.FirstOrDefaultAsync(x => x.Id == id);
            if (actors == null)
            {
                return NotFound();
            }
            return _mapper.Map<ActorDTO>(actors);
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromForm] ActorCreationDTO actorCreationDTO)
        {
            var actor = _mapper.Map<Actor>(actorCreationDTO);
            if (actorCreationDTO.Picture != null)
            {
                actor.Picture = await _fileStorageService.SaveFile(containerName, actorCreationDTO.Picture);
            }
            _ctx.Add(actor);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Put(int id, [FromForm] ActorCreationDTO actorCreationDTO)
        {
            var actor = await _ctx.Actors.FirstOrDefaultAsync(x => x.Id == id);
            if (actor == null)
            {
                return NotFound();
            }
            actor = _mapper.Map(actorCreationDTO, actor);
            if (actorCreationDTO.Picture != null)
            {
                actor.Picture = await _fileStorageService.EditFile(containerName, actorCreationDTO.Picture, actor.Picture);
            }
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var actor = await _ctx.Actors.FirstOrDefaultAsync(x => x.Id == id);
            if (actor == null)
            {
                return NotFound();
            }

            _ctx.Remove(actor);
            await _ctx.SaveChangesAsync();
            await _fileStorageService.DeleteFile(actor.Picture, containerName);
            return NoContent();
        }
    }
}