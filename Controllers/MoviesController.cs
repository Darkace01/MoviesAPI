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

        [HttpGet("{id}")]
        public async Task<ActionResult<MovieDTO>> Get(int id)
        {
            var movie = await _ctx.Movies
                        .Include(x => x.MoviesGenres).ThenInclude(x => x.Genre)
                        .Include(x => x.MovieTheatersMovies).ThenInclude(x => x.MovieTheater)
                        .Include(x => x.MoviesActors).ThenInclude(x => x.Actor)
                        .FirstOrDefaultAsync(x => x.Id == id);

            if (movie == null)
            {
                return NotFound();
            }

            var dto = _mapper.Map<MovieDTO>(movie);
            dto.Actors = dto.Actors.OrderBy(x => x.Order).ToList();
            return dto;
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

        [HttpGet("putget/{id}")]
        public async Task<ActionResult<MoviePutGetDTO>> PutGet(int id)
        {
            var movieActionResult = await Get(id);
            if (movieActionResult.Result is NotFoundResult) { return NotFound(); }

            var movie = movieActionResult.Value;

            var genresSelectedIds = movie.Genres.Select(x => x.Id).ToList();
            var NonSelectedGenres = await _ctx.Genres.Where(x => !genresSelectedIds.Contains(x.Id))
                .ToListAsync();

            var movieTheatersIds = movie.MoviesTheaters.Select(x => x.Id).ToList();
            var NonSelectedMovieTheaters = await _ctx.MovieTheaters.Where(x =>
                    !movieTheatersIds.Contains(x.Id)).ToListAsync();

            var NonSelectedGenresDTOs = _mapper.Map<List<GenreDTO>>(NonSelectedGenres);
            var NonSelectedMovieTheatersDTO = _mapper.Map<List<MovieTheaterDTO>>(NonSelectedMovieTheaters);

            var response = new MoviePutGetDTO
            {
                Movie = movie,
                SelectedGenres = movie.Genres,
                NonSelectedGenres = NonSelectedGenresDTOs,
                SelectedMovieTheaters = movie.MoviesTheaters,
                NonSelectedMovieTheaters = NonSelectedMovieTheatersDTO,
                Actors = movie.Actors
            };
            return response;

        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Put(int id, [FromForm] MovieCreationDTO movieCreationDTO)
        {
            var movie = await _ctx.Movies.Include(x => x.MoviesActors)
                                .Include(x => x.MoviesGenres)
                                .Include(x => x.MovieTheatersMovies)
                                .FirstOrDefaultAsync(x => x.Id == id);

            if (movie == null)
            {
                return NotFound();
            }

            movie = _mapper.Map(movieCreationDTO, movie);
            if (movieCreationDTO.Poster != null)
            {
                movie.Poster = await _fileStorage.EditFile(container, movieCreationDTO.Poster, movie.Poster);
            }

            AnnotateActorsOrder(movie);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        private static void AnnotateActorsOrder(Movie movie)
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