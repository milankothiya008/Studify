using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;
using SmartLearning.Api.Services;

namespace SmartLearning.Api.Controllers
{
    [Authorize]
    [Route("api/enrollments")]
    public class EnrollmentsController : BaseApiController
    {
        private readonly AppDbContext _db;
        private readonly CourseAccessService _accessService;

        public EnrollmentsController(AppDbContext db, CourseAccessService accessService)
        {
            _db = db;
            _accessService = accessService;
        }

        // POST api/enrollments/5
        // Enrolls in a FREE course, or in a paid course using an active subscription.
        // (Buying a single course is done in CheckoutController.)
        [HttpPost("{courseId}")]
        public async Task<ActionResult> Enroll(int courseId)
        {
            int userId = GetUserId();

            Course course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId && c.IsPublished);
            if (course == null)
            {
                return ErrorMessage(404, "Course not found.");
            }

            if (course.InstructorId == userId)
            {
                return ErrorMessage(400, "You are the instructor of this course.");
            }

            Enrollment existing = await _db.Enrollments.FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);
            if (existing != null)
            {
                return Ok(new { message = "You are already enrolled." });
            }

            string accessType;
            if (course.Price == 0)
            {
                accessType = AccessTypes.Free;
            }
            else if (await _accessService.HasActiveSubscriptionAsync(userId))
            {
                accessType = AccessTypes.Subscription;
            }
            else
            {
                return ErrorMessage(402, "This is a paid course. Buy it or get a subscription to enroll.");
            }

            _db.Enrollments.Add(new Enrollment
            {
                UserId = userId,
                CourseId = courseId,
                AccessType = accessType,
                EnrolledAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            return Ok(new { message = "You are enrolled. Happy learning!" });
        }

        // GET api/enrollments/my  -> the "My learning" page
        [HttpGet("my")]
        public async Task<ActionResult<List<MyCourseDto>>> GetMyCourses()
        {
            int userId = GetUserId();
            bool hasActiveSubscription = await _accessService.HasActiveSubscriptionAsync(userId);

            List<MyCourseDto> courses = await _db.Enrollments
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.LastAccessedAt ?? e.EnrolledAt)
                .Select(e => new MyCourseDto
                {
                    CourseId = e.CourseId,
                    Title = e.Course.Title,
                    ThumbnailUrl = e.Course.ThumbnailUrl,
                    InstructorName = e.Course.Instructor.FullName,
                    AccessType = e.AccessType,
                    TotalLectures = e.Course.Sections.SelectMany(s => s.Lectures).Count(),
                    CompletedLectures = _db.LectureProgresses.Count(p =>
                        p.UserId == userId && p.IsCompleted && p.Lecture.Section.CourseId == e.CourseId),
                    LastLectureId = e.LastLectureId,
                    EnrolledAt = e.EnrolledAt,
                    LastAccessedAt = e.LastAccessedAt,
                    CompletedAt = e.CompletedAt
                })
                .ToListAsync();

            // Work out the fields that are easier to calculate in C#.
            foreach (MyCourseDto course in courses)
            {
                course.ProgressPercent = CourseMapper.CalculatePercent(course.CompletedLectures, course.TotalLectures);
                course.CanWatch = course.AccessType != AccessTypes.Subscription || hasActiveSubscription;
            }

            return Ok(courses);
        }
    }
}
