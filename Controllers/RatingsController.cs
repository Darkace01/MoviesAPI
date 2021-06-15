using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesAPI.Data;
using MoviesAPI.DTOs;
using MoviesAPI.Entities;

namespace MoviesAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RatingsController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<IdentityUser> userManager;

        public RatingsController(ApplicationDbContext _context, UserManager<IdentityUser> _userManager)
        {
            this.context = _context;
            this.userManager = _userManager;
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult> Post([FromBody] RatingDTO ratingDTO)
        {
            var email = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "email").Value;
            var user = await userManager.FindByEmailAsync(email);
            var userId = user.Id;

            var currentRate = await context.Ratings
                                .FirstOrDefaultAsync(x => x.MovieId == ratingDTO.MovieId && x.UserId == userId);

            if (currentRate == null)
            {
                var rating = new Rating
                {
                    MovieId = ratingDTO.MovieId,
                    Rate = ratingDTO.Rating,
                    UserId = userId
                };
                context.Add(rating);
            }
            else
            {
                currentRate.Rate = ratingDTO.Rating;
            }
            await context.SaveChangesAsync();
            return NoContent();
        }
    }
}