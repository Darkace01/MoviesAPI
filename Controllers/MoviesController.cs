using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
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
    public class MoviesController : ControllerBase
    {
        private readonly ApplicationDbContext _ctx;
        private readonly IMapper _mapper;
        private readonly IFileStorageService _fileStorage;
        private string container = "movies";

        public MoviesController(ApplicationDbContext ctx, IMapper mapper, IFileStorageService fileStorage)
        {
            _ctx = ctx;
            _mapper = mapper;
            _fileStorage = fileStorage;
        }

        [HttpGet("PostGet")]
        public async Task<ActionResult<MoviePostGetDTO>> PostGet()
        {
            var movieTheaters = await _ctx.MovieTheaters.OrderBy(x => x.Name).ToListAsync();
            var genres = await _ctx.Genres.OrderBy(x => x.Name).ToListAsync();

            var MovieTheaterDTO = _mapper.Map<List<MovieTheaterDTO>>(movieTheaters);
            var genreDTO = _mapper.Map<List<GenreDTO>>(genres);

            return new MoviePostGetDTO() { Genres = genreDTO, MovieTheaters = MovieTheaterDTO };
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromForm] MovieCreationDTO movieCreationDTO)
        {
            var movie = _mapper.Map<Movie>(movieCreationDTO);

            if (movieCreationDTO.Poster != null)
            {
                movie.Poster = await _fileStorage.SaveFile(container, movieCreationDTO.Poster);
            }
            AnnotateActorsOrder(movie);
            _ctx.Add(movie);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        private void AnnotateActorsOrder(Movie movie)
        {
            if (movie.MoviesActors != null)
            {
                for (int i = 0; i < movie.MoviesActors.Count; i++)
                {
                    movie.MoviesActors[i].Order = i;
                }
            }
        }
    }
}