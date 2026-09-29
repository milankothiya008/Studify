using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;

namespace SmartLearning.Api.Services
{
    // Turns users into instructor cards with their numbers.
    // Only published courses count, because that is what students can see.
    public static class InstructorMapper
    {
        // currentUserId = the logged in user (0 when nobody is logged in), used for IsFollowing.
        // Entity Framework turns this into a single SQL query.
        public static IQueryable<InstructorSummaryDto> SelectSummaries(IQueryable<User> users, AppDbContext db, int currentUserId)
        {
            return users.Select(u => new InstructorSummaryDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Headline = u.Headline,
                ProfileImageUrl = u.ProfileImageUrl,
                CourseCount = u.CoursesTaught.Count(c => c.IsPublished),

                // A student who joined 3 of the instructor's courses is counted once.
                StudentCount = db.Enrollments
                    .Where(e => e.Course.InstructorId == u.Id && e.Course.IsPublished)
                    .Select(e => e.UserId)
                    .Distinct()
                    .Count(),

                ReviewCount = db.Reviews.Count(r => r.Course.InstructorId == u.Id && r.Course.IsPublished),
                AverageRating = db.Reviews
                    .Where(r => r.Course.InstructorId == u.Id && r.Course.IsPublished)
                    .Average(r => (double?)r.Rating) ?? 0,

                FollowerCount = db.Follows.Count(f => f.InstructorId == u.Id),
                IsFollowing = db.Follows.Any(f => f.InstructorId == u.Id && f.FollowerId == currentUserId)
            });
        }

        // Users who have a public instructor profile: instructors, and admins who published a course.
        public static IQueryable<User> Instructors(AppDbContext db)
        {
            return db.Users.Where(u =>
                u.Role == Roles.Instructor ||
                u.CoursesTaught.Any(c => c.IsPublished));
        }
    }
}
