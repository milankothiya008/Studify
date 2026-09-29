using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;
using SmartLearning.Api.Services;

namespace SmartLearning.Api.Controllers
{
    // Public instructor profiles, and following instructors.
    [Route("api/instructors")]
    public class InstructorsController : BaseApiController
    {
        private readonly AppDbContext _db;
        private readonly NotificationService _notificationService;

        public InstructorsController(AppDbContext db, NotificationService notificationService)
        {
            _db = db;
            _notificationService = notificationService;
        }

        // GET api/instructors?limit=8  -> instructors with the most students (home page)
        [HttpGet]
        public async Task<ActionResult<List<InstructorSummaryDto>>> GetTopInstructors(int limit = 8)
        {
            if (limit < 1 || limit > 24) limit = 8;

            IQueryable<User> teaching = _db.Users.Where(u => u.CoursesTaught.Any(c => c.IsPublished));

            List<InstructorSummaryDto> instructors = await InstructorMapper.SelectSummaries(teaching, _db, GetUserId())
                .OrderByDescending(i => i.StudentCount)
                .ThenByDescending(i => i.FollowerCount)
                .ThenBy(i => i.Id)
                .Take(limit)
                .ToListAsync();

            return Ok(instructors);
        }

        // GET api/instructors/following  -> instructors I follow
        [Authorize]
        [HttpGet("following")]
        public async Task<ActionResult<List<InstructorSummaryDto>>> GetFollowing()
        {
            int userId = GetUserId();

            // Newest follow first.
            List<int> ids = await _db.Follows
                .Where(f => f.FollowerId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => f.InstructorId)
                .ToListAsync();

            List<InstructorSummaryDto> instructors = await InstructorMapper
                .SelectSummaries(_db.Users.Where(u => ids.Contains(u.Id)), _db, userId)
                .ToListAsync();

            return Ok(instructors.OrderBy(i => ids.IndexOf(i.Id)).ToList());
        }

        // GET api/instructors/5  -> the public profile page
        [HttpGet("{id:int}")]
        public async Task<ActionResult<InstructorProfileDto>> GetProfile(int id)
        {
            int userId = GetUserId();

            User user = await InstructorMapper.Instructors(_db).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return ErrorMessage(404, "Instructor not found.");
            }

            InstructorSummaryDto summary = await InstructorMapper
                .SelectSummaries(_db.Users.Where(u => u.Id == id), _db, userId)
                .FirstAsync();

            IQueryable<Course> courses = _db.Courses
                .Where(c => c.InstructorId == id && c.IsPublished)
                .OrderByDescending(c => c.Enrollments.Count)
                .ThenByDescending(c => c.Id);

            // The newest reviews that have a comment.
            List<InstructorReviewDto> reviews = await _db.Reviews
                .Where(r => r.Course.InstructorId == id && r.Course.IsPublished && r.Comment != null && r.Comment != "")
                .OrderByDescending(r => r.CreatedAt)
                .Take(12)
                .Select(r => new InstructorReviewDto
                {
                    Id = r.Id,
                    UserName = r.User.FullName,
                    UserImageUrl = r.User.ProfileImageUrl,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    CourseId = r.CourseId,
                    CourseTitle = r.Course.Title
                })
                .ToListAsync();

            InstructorProfileDto profile = new InstructorProfileDto
            {
                Id = summary.Id,
                FullName = summary.FullName,
                Headline = summary.Headline,
                ProfileImageUrl = summary.ProfileImageUrl,
                CourseCount = summary.CourseCount,
                StudentCount = summary.StudentCount,
                ReviewCount = summary.ReviewCount,
                AverageRating = summary.AverageRating,
                FollowerCount = summary.FollowerCount,
                IsFollowing = summary.IsFollowing,
                Bio = user.Bio,
                WebsiteUrl = user.WebsiteUrl,
                LinkedInUrl = user.LinkedInUrl,
                YouTubeUrl = user.YouTubeUrl,
                TwitterUrl = user.TwitterUrl,
                JoinedAt = user.CreatedAt,
                IsMe = userId == id,
                Courses = await CourseMapper.SelectCards(courses).ToListAsync(),
                Reviews = reviews
            };

            return Ok(profile);
        }

        // GET api/instructors/5/followers
        [HttpGet("{id:int}/followers")]
        public async Task<ActionResult<List<FollowerDto>>> GetFollowers(int id)
        {
            List<FollowerDto> followers = await _db.Follows
                .Where(f => f.InstructorId == id)
                .OrderByDescending(f => f.CreatedAt)
                .Take(200)
                .Select(f => new FollowerDto
                {
                    Id = f.FollowerId,
                    FullName = f.Follower.FullName,
                    ProfileImageUrl = f.Follower.ProfileImageUrl,
                    IsInstructor = f.Follower.Role == Roles.Instructor || f.Follower.CoursesTaught.Any(c => c.IsPublished),
                    FollowedAt = f.CreatedAt
                })
                .ToListAsync();

            return Ok(followers);
        }

        // POST api/instructors/5/follow
        [Authorize]
        [HttpPost("{id:int}/follow")]
        public async Task<ActionResult<FollowResultDto>> Follow(int id)
        {
            int userId = GetUserId();
            if (userId == id)
            {
                return ErrorMessage(400, "You cannot follow yourself.");
            }

            User instructor = await InstructorMapper.Instructors(_db).FirstOrDefaultAsync(u => u.Id == id);
            if (instructor == null)
            {
                return ErrorMessage(404, "Instructor not found.");
            }

            bool alreadyFollowing = await _db.Follows.AnyAsync(f => f.FollowerId == userId && f.InstructorId == id);
            bool isNew = false;
            if (!alreadyFollowing)
            {
                _db.Follows.Add(new Follow { FollowerId = userId, InstructorId = id, CreatedAt = DateTime.UtcNow });
                try
                {
                    await _db.SaveChangesAsync();
                    isNew = true;
                }
                catch (DbUpdateException)
                {
                    // Clicked twice at the same moment: the other request already saved it.
                }
            }

            if (isNew)
            {
                User follower = await _db.Users.FindAsync(userId);
                await _notificationService.NotifyAsync(id, "New follower",
                    follower.FullName + " started following you.", "/instructors/" + id + "?tab=followers");
            }

            return Ok(new FollowResultDto
            {
                IsFollowing = true,
                FollowerCount = await _db.Follows.CountAsync(f => f.InstructorId == id),
                Message = "You are now following " + instructor.FullName + "."
            });
        }

        // DELETE api/instructors/5/follow
        [Authorize]
        [HttpDelete("{id:int}/follow")]
        public async Task<ActionResult<FollowResultDto>> Unfollow(int id)
        {
            int userId = GetUserId();
            await _db.Follows
                .Where(f => f.FollowerId == userId && f.InstructorId == id)
                .ExecuteDeleteAsync();

            return Ok(new FollowResultDto
            {
                IsFollowing = false,
                FollowerCount = await _db.Follows.CountAsync(f => f.InstructorId == id),
                Message = "You unfollowed this instructor."
            });
        }
    }
}
