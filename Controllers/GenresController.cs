using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using MoviesAPI.Entities;
using MoviesAPI.Services;

namespace MoviesAPI.Controllers
{
    [Route("api/[controller]")]
    public class GenresController
    {
        private readonly IRepository _repository;
        public GenresController(IRepository repository)
        {
            _repository = repository;
        }
        [HttpGet]
        public List<Genre> Get()
        {
            return _repository.GetAllGenre();
        }

        [HttpPost]
        public void Post()
        {

        }

        [HttpPut]
        public void Put()
        {

        }

        [HttpDelete]
        public void Delete()
        {

        }
    }
}