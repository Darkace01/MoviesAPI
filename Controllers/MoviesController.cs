using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public class MoviesController : ControllerBase
    {
        private readonly ApplicationDbContext _ctx;
        private readonly IMapper _mapper;
        private readonly IFileStorageService _fileStorage;
        private string container = "movies";
        private readonly UserManager<IdentityUser> userManager;

        public MoviesController(ApplicationDbContext ctx, IMapper mapper, IFileStorageService fileStorage, UserManager<IdentityUser> userManager)
        {
            _ctx = ctx;
            _mapper = mapper;
            _fileStorage = fileStorage;
            this.userManager = userManager;
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

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<HomeDTO>> Get()
        {
            var top = 6;
            var today = DateTime.Today;

            var upcommingReleases = await _ctx.Movies
                        .Where(x => x.ReleaseDate > today)
                        .OrderBy(x => x.ReleaseDate)
                        .Take(top)
                        .ToListAsync();

            var inTheaters = await _ctx.Movies
                        .Where(x => x.InTheaters)
                        .OrderBy(x => x.ReleaseDate)
                        .Take(top)
                        .ToListAsync();

            var homeDTO = new HomeDTO
            {
                UpcomingReleases = _mapper.Map<List<MovieDTO>>(upcommingReleases),
                InTheaters = _mapper.Map<List<MovieDTO>>(inTheaters)
            };

            return homeDTO;
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<MovieDTO>> Get(int id)
        {
            var movie = await _ctx.Movies
                        .Include(x => x.MoviesGenres).ThenInclude(x => x.Genre)
                        .Include(x => x.MovieTheatersMovies).ThenInclude(x => x.MovieTheater)
                        .Include(x => x.MoviesActors).ThenInclude(x => x.Actor)
                        .AsSplitQuery()
                        .FirstOrDefaultAsync(x => x.Id == id);

            if (movie == null)
            {
                return NotFound();
            }

            var averageVote = 0.0;
            var userVote = 0;

            if (await _ctx.Ratings.AnyAsync(x => x.MovieId == id))
            {
                averageVote = await _ctx.Ratings.Where(x => x.MovieId == id)
                                .AverageAsync(x => x.Rate);

                if (HttpContext.User.Identity.IsAuthenticated)
                {
                    var email = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "email").Value;
                    var user = await userManager.FindByEmailAsync(email);
                    var userId = user.Id;

                    var ratingDb = await _ctx.Ratings.FirstOrDefaultAsync(x => x.MovieId == id && x.UserId == userId);

                    if (ratingDb != null)
                    {
                        userVote = ratingDb.Rate;
                    }
                }
            }

            var dto = _mapper.Map<MovieDTO>(movie);
            dto.AverageVote = averageVote;
            dto.UserVote = userVote;
            dto.Actors = dto.Actors.OrderBy(x => x.Order).ToList();
            return dto;
        }

        [HttpGet("filter")]
        public async Task<ActionResult<List<MovieDTO>>> Filter([FromQuery] FilterMoviesDTO filterMoviesDTO)
        {
            var moviesQueryable = _ctx.Movies.AsQueryable();

            if (!string.IsNullOrEmpty(filterMoviesDTO.Title))
            {
                moviesQueryable = moviesQueryable.Where(x => x.Title.Contains(filterMoviesDTO.Title));
            }
            if (filterMoviesDTO.InTheaters)
            {
                moviesQueryable = moviesQueryable.Where(x => x.InTheaters);
            }
            if (filterMoviesDTO.UpcomingReleases)
            {
                var today = DateTime.Today;
                moviesQueryable = moviesQueryable.Where(x => x.ReleaseDate > today);
            }
            if (filterMoviesDTO.GenreId != 0)
            {
                moviesQueryable = moviesQueryable
                                    .Where(x => x.MoviesGenres.Select(y => y.GenreId)
                                    .Contains(filterMoviesDTO.GenreId));
            }
            await HttpContext.InsertParametersPaginationInHeader(moviesQueryable);
            var movies = await moviesQueryable.OrderBy(x => x.Title).Paginate(filterMoviesDTO.PaginationDTO)
                            .ToListAsync();
            return _mapper.Map<List<MovieDTO>>(movies);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var movie = await _ctx.Movies.FirstOrDefaultAsync(x => x.Id == id);

            if (movie == null)
            {
                return NotFound();
            }
            _ctx.Remove(movie);
            await _ctx.SaveChangesAsync();
            await _fileStorage.DeleteFile(movie.Poster, container);
            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<int>> Post([FromForm] MovieCreationDTO movieCreationDTO)
        {
            var movie = _mapper.Map<Movie>(movieCreationDTO);

            if (movieCreationDTO.Poster != null)
            {
                movie.Poster = await _fileStorage.SaveFile(container, movieCreationDTO.Poster);
            }
            AnnotateActorsOrder(movie);
            _ctx.Add(movie);
            await _ctx.SaveChangesAsync();
            return movie.Id;
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
                                .AsSplitQuery()
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