using MoviesAPI.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoviesAPI.Services
{
    public interface IRepository
    {
        void AddGenre(Genre genre);
        Task<List<Genre>> GetAllGenre();
        Genre GetGenreById(int Id);
    }
}