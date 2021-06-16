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

namespace MoviesAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsAdmin")]
    public class MovieTheaterController : ControllerBase
    {
        private readonly ApplicationDbContext _ctx;
        private readonly IMapper _mapper;

        public MovieTheaterController(ApplicationDbContext ctx, IMapper mapper)
        {
            _ctx = ctx;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<MovieTheaterDTO>>> Get()
        {
            var entities = await _ctx.MovieTheaters.OrderBy(x => x.Name).ToListAsync();
            return _mapper.Map<List<MovieTheaterDTO>>(entities);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MovieTheaterDTO>> Get(int id)
        {
            var movieTheater = await _ctx.MovieTheaters.FirstOrDefaultAsync(x => x.Id == id);
            if (movieTheater == null)
                return NotFound();

            return _mapper.Map<MovieTheaterDTO>(movieTheater);
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] MovieTheaterCreationDTO movieCreationDTO)
        {
            var movieTheater = _mapper.Map<MovieTheater>(movieCreationDTO);
            _ctx.Add(movieTheater);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Put(int id, [FromBody] MovieTheaterCreationDTO movieTheaterCreationDTO)
        {
            var movieTheater = await _ctx.MovieTheaters.FirstOrDefaultAsync(x => x.Id == id);
            if (movieTheater == null)
                return NotFound();

            movieTheater = _mapper.Map(movieTheaterCreationDTO, movieTheater);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var movieTheater = await _ctx.MovieTheaters.FirstOrDefaultAsync(x => x.Id == id);
            if (movieTheater == null)
            {
                return NotFound();
            }

            _ctx.Remove(movieTheater);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }
    }
}