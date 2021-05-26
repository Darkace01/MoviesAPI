using System;
using System.Collections.Generic;

namespace MoviesAPI.DTOs
{
    public class MovieDTO
    {
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Trailer { get; set; }
        public bool InTheaters { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string Poster { get; set; }
        public List<GenreDTO> Genres { get; set; }
        public List<MovieTheaterDTO> MoviesTheaters { get; set; }
        public List<ActorMovieDTO> Actors { get; set; }
    }
}