using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;
using SmartLearning.Api.Services;

namespace SmartLearning.Api.Controllers
{
    // Courses a user saved for later (the heart button).
    [Authorize]
    [Route("api/wishlist")]
    public class WishlistController : BaseApiController
    {
        private readonly AppDbContext _db;

        public WishlistController(AppDbContext db)
        {
            _db = db;
        }

        // GET api/wishlist  -> my saved courses
        [HttpGet]
        public async Task<ActionResult<List<CourseCardDto>>> GetWishlist()
        {
            int userId = GetUserId();

            IQueryable<Course> courses = _db.WishlistItems
                .Where(w => w.UserId == userId && w.Course.IsPublished)
                .OrderByDescending(w => w.CreatedAt)
                .Select(w => w.Course);

            return Ok(await CourseMapper.SelectCards(courses).ToListAsync());
        }

        // GET api/wishlist/ids  -> just the course ids (to fill the hearts on every page)
        [HttpGet("ids")]
        public async Task<ActionResult<List<int>>> GetIds()
        {
            int userId = GetUserId();
            List<int> ids = await _db.WishlistItems
                .Where(w => w.UserId == userId)
                .Select(w => w.CourseId)
                .ToListAsync();
            return Ok(ids);
        }

        // POST api/wishlist/5
        [HttpPost("{courseId}")]
        public async Task<ActionResult> Add(int courseId)
        {
            int userId = GetUserId();

            bool courseExists = await _db.Courses.AnyAsync(c => c.Id == courseId && c.IsPublished);
            if (!courseExists)
            {
                return ErrorMessage(404, "Course not found.");
            }

            bool alreadySaved = await _db.WishlistItems.AnyAsync(w => w.UserId == userId && w.CourseId == courseId);
            if (!alreadySaved)
            {
                _db.WishlistItems.Add(new WishlistItem { UserId = userId, CourseId = courseId, CreatedAt = DateTime.UtcNow });
                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // Saved twice at the same moment (double click): the other request already did it.
                }
            }

            return Ok(new { message = "Added to your wishlist." });
        }

        // DELETE api/wishlist/5
        [HttpDelete("{courseId}")]
        public async Task<ActionResult> Remove(int courseId)
        {
            int userId = GetUserId();
            await _db.WishlistItems
                .Where(w => w.UserId == userId && w.CourseId == courseId)
                .ExecuteDeleteAsync();
            return Ok(new { message = "Removed from your wishlist." });
        }
    }
}
