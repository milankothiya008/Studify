using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;
using SmartLearning.Api.Services;

namespace SmartLearning.Api.Controllers
{
    // Admin panel. Categories and plans have their own controllers.
    [Authorize(Roles = Roles.Admin)]
    [Route("api/admin")]
    public class AdminController : BaseApiController
    {
        private readonly AppDbContext _db;

        public AdminController(AppDbContext db)
        {
            _db = db;
        }

        // GET api/admin/stats
        [HttpGet("stats")]
        public async Task<ActionResult<AdminStatsDto>> GetStats()
        {
            DateTime now = DateTime.UtcNow;

            AdminStatsDto stats = new AdminStatsDto
            {
                TotalUsers = await _db.Users.CountAsync(),
                TotalStudents = await _db.Users.CountAsync(u => u.Role == Roles.Student),
                TotalInstructors = await _db.Users.CountAsync(u => u.Role == Roles.Instructor),
                TotalCourses = await _db.Courses.CountAsync(),
                PublishedCourses = await _db.Courses.CountAsync(c => c.IsPublished),
                TotalEnrollments = await _db.Enrollments.CountAsync(),
                ActiveSubscriptions = await _db.UserSubscriptions
                    .Where(s => s.StartDate <= now && s.EndDate > now)
                    .Select(s => s.UserId)
                    .Distinct()
                    .CountAsync(),
                TotalRevenue = await _db.Payments.Where(p => p.Status == "Paid").SumAsync(p => p.Amount)
            };

            return Ok(stats);
        }

        // GET api/admin/users
        [HttpGet("users")]
        public async Task<ActionResult<List<AdminUserDto>>> GetUsers()
        {
            List<AdminUserDto> users = await _db.Users
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new AdminUserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    IsEmailVerified = u.IsEmailVerified,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        // PUT api/admin/users/3/role   body: { "role": "Instructor" }
        [HttpPut("users/{id}/role")]
        public async Task<ActionResult> ChangeRole(int id, ChangeRoleRequest request)
        {
            if (request.Role != Roles.Student && request.Role != Roles.Instructor && request.Role != Roles.Admin)
            {
                return ErrorMessage(400, "Unknown role.");
            }

            if (id == GetUserId())
            {
                return ErrorMessage(400, "You cannot change your own role.");
            }

            User user = await _db.Users.FindAsync(id);
            if (user == null)
            {
                return ErrorMessage(404, "User not found.");
            }

            user.Role = request.Role;
            TokenService.RenewSecurityStamp(user); // the user must log in again to get the new role
            await _db.SaveChangesAsync();

            return Ok(new { message = "Role changed. The user must log in again to see the change." });
        }

        // POST api/admin/demo-courses  -> adds the demo courses that are missing (see Data/DemoCourses.cs)
        [HttpPost("demo-courses")]
        public ActionResult AddDemoCourses()
        {
            int added = DemoCourses.AddMissing(_db);

            if (added == 0)
            {
                return Ok(new { added = 0, message = "Demo content is up to date (courses, quizzes and coupons)." });
            }
            string word = added == 1 ? " demo course added." : " demo courses added.";
            return Ok(new { added = added, message = added + word });
        }

        // GET api/admin/courses  -> every course, including drafts
        [HttpGet("courses")]
        public async Task<ActionResult<List<CourseCardDto>>> GetCourses()
        {
            List<CourseCardDto> courses = await CourseMapper
                .SelectCards(_db.Courses.OrderByDescending(c => c.CreatedAt))
                .ToListAsync();

            return Ok(courses);
        }
    }
}
