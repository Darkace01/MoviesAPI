using System.Collections.Generic;
using AutoMapper;
using MoviesAPI.DTOs;
using MoviesAPI.Entities;
using NetTopologySuite.Geometries;

namespace MoviesAPI.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles(GeometryFactory geometryFactory)
        {
            //Genre Mapping
            CreateMap<GenreDTO, Genre>().ReverseMap();
            CreateMap<GenreCreationDTO, Genre>().ReverseMap();

            //Actor Mapping
            CreateMap<ActorDTO, Actor>().ReverseMap();
            CreateMap<ActorCreationDTO, Actor>()
                    .ForMember(x => x.Picture, options => options.Ignore());

            //MovieTheater Mapping
            CreateMap<MovieTheater, MovieTheaterDTO>()
                    .ForMember(x => x.Latitude, dto => dto.MapFrom(prop => prop.Location.Y))
                    .ForMember(x => x.Longitude, dto => dto.MapFrom(prop => prop.Location.X));
            CreateMap<MovieTheaterCreationDTO, MovieTheater>()
                    .ForMember(x => x.Location, x => x.MapFrom(dto =>
                     geometryFactory.CreatePoint(new Coordinate(dto.Longitude, dto.Latitude))));

            //Movie Mapping
            CreateMap<MovieCreationDTO, Movie>()
                    .ForMember(x => x.Poster, options => options.Ignore())
                    .ForMember(x => x.MoviesGenres, options => options.MapFrom(MapMoviesGenres))
                    .ForMember(x => x.MovieTheatersMovies, options => options.MapFrom(MapMovieTheaters))
                    .ForMember(x => x.MoviesActors, options => options.MapFrom(MapMoviesActors));

            CreateMap<Movie, MovieDTO>()
                    .ForMember(x => x.Genres, options => options.MapFrom(MapMoviesGenres))
                    .ForMember(x => x.MoviesTheaters, options => options.MapFrom(MovieTheatersMovies))
                    .ForMember(x => x.Actors, options => options.MapFrom(MapMoviesActors));
        }

        private List<ActorMovieDTO> MapMoviesActors(Movie movie, MovieDTO movieDTO)
        {
            var result = new List<ActorMovieDTO>();

            if (movie.MoviesActors != null)
            {
                foreach (var moviesActors in movie.MoviesActors)
                {
                    result.Add(new ActorMovieDTO()
                    {
                        Id = moviesActors.ActorId,
                        Name = moviesActors.Actor.Name,
                        Character = moviesActors.Character,
                        Picture = moviesActors.Actor.Picture,
                        Order = moviesActors.Order
                    });
                }
            }

            return result;
        }

        private List<MovieTheaterDTO> MovieTheatersMovies(Movie movie, MovieDTO movieDTO)
        {
            var result = new List<MovieTheaterDTO>();
            if (movie.MovieTheatersMovies != null)
            {
                foreach (var movieTheaterMovies in movie.MovieTheatersMovies)
                {
                    result.Add(new MovieTheaterDTO() { Id = movieTheaterMovies.MovieTheaterId, Name = movieTheaterMovies.MovieTheater.Name });
                }
            }
            return result;
        }

        private List<GenreDTO> MapMoviesGenres(Movie movie, MovieDTO movieDTO)
        {
            var result = new List<GenreDTO>();

            if (movie.MoviesGenres != null)
            {
                foreach (var genre in movie.MoviesGenres)
                {
                    result.Add(new GenreDTO() { Id = genre.GenreId, Name = genre.Genre.Name });
                }
            }

            return result;
        }

        private List<MoviesGenres> MapMoviesGenres(MovieCreationDTO movieCreationDTO, Movie movie)
        {
            var result = new List<MoviesGenres>();

            if (movieCreationDTO.GenresIds == null) { return result; }
            foreach (var id in movieCreationDTO.GenresIds)
            {
                result.Add(new MoviesGenres() { GenreId = id });
            }
            return result;
        }

        private List<MovieTheatersMovies> MapMovieTheaters(MovieCreationDTO movieCreationDTO, Movie movie)
        {
            var result = new List<MovieTheatersMovies>();

            if (movieCreationDTO.MoviesTheatersIds == null) { return result; }
            foreach (var id in movieCreationDTO.MoviesTheatersIds)
            {
                result.Add(new MovieTheatersMovies() { MovieTheaterId = id });
            }
            return result;
        }

        private List<MoviesActors> MapMoviesActors(MovieCreationDTO movieCreationDTO, Movie movie)
        {
            var result = new List<MoviesActors>();

            if (movieCreationDTO.Actors == null) { return result; }
            foreach (var actor in movieCreationDTO.Actors)
            {
                result.Add(new MoviesActors() { ActorId = actor.Id, Character = actor.Character });
            }
            return result;
        }
    }
}