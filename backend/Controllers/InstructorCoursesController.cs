using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;
using SmartLearning.Api.Services;

namespace SmartLearning.Api.Controllers
{
    // Instructors create and manage their own courses here.
    // (To see the full course with all lectures, the editor uses GET api/courses/{id}.)
    [Authorize(Roles = Roles.Instructor + "," + Roles.Admin)]
    [Route("api/instructor")]
    public class InstructorCoursesController : BaseApiController
    {
        private readonly AppDbContext _db;
        private readonly FileStorageService _fileStorage;
        private readonly NotificationService _notificationService;

        public InstructorCoursesController(AppDbContext db, FileStorageService fileStorage, NotificationService notificationService)
        {
            _db = db;
            _fileStorage = fileStorage;
            _notificationService = notificationService;
        }

        // GET api/instructor/dashboard  -> my courses with students, rating and revenue
        [HttpGet("dashboard")]
        public async Task<ActionResult<InstructorDashboardDto>> GetDashboard()
        {
            int userId = GetUserId();

            List<InstructorCourseDto> courses = await _db.Courses
                .Where(c => c.InstructorId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => new InstructorCourseDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    ThumbnailUrl = c.ThumbnailUrl,
                    IsPublished = c.IsPublished,
                    Price = c.Price,
                    StudentCount = c.Enrollments.Count,
                    AverageRating = c.Reviews.Count > 0 ? c.Reviews.Average(r => r.Rating) : 0,
                    ReviewCount = c.Reviews.Count,
                    Revenue = _db.Payments
                        .Where(p => p.CourseId == c.Id && p.Status == "Paid")
                        .Sum(p => p.Amount),
                    LectureCount = c.Sections.SelectMany(s => s.Lectures).Count(),
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            // Average of the courses that have at least one review.
            List<InstructorCourseDto> ratedCourses = courses.Where(c => c.ReviewCount > 0).ToList();

            InstructorDashboardDto dashboard = new InstructorDashboardDto
            {
                TotalCourses = courses.Count,
                TotalStudents = courses.Sum(c => c.StudentCount),
                TotalRevenue = courses.Sum(c => c.Revenue),
                AverageRating = ratedCourses.Count > 0 ? ratedCourses.Average(c => c.AverageRating) : 0,
                Courses = courses
            };

            return Ok(dashboard);
        }

        // GET api/instructor/analytics?days=30&utcOffsetMinutes=330  -> enrollments and revenue per day
        // utcOffsetMinutes = the instructor's time zone (India = +330), so a sale at 1 AM in India
        // is counted on the right day, not on the day before (which it still is in UTC).
        [HttpGet("analytics")]
        public async Task<ActionResult<InstructorAnalyticsDto>> GetAnalytics(int days = 30, int utcOffsetMinutes = 0)
        {
            if (days < 7) days = 7;
            if (days > 365) days = 365;
            if (utcOffsetMinutes < -840 || utcOffsetMinutes > 840) utcOffsetMinutes = 0;

            int userId = GetUserId();

            // "Today" in the instructor's time zone, and the first day of the period.
            DateTime localToday = DateTime.UtcNow.AddMinutes(utcOffsetMinutes).Date;
            DateTime firstLocalDay = localToday.AddDays(-(days - 1));
            // The same moment in UTC, to search the database.
            DateTime firstDay = firstLocalDay.AddMinutes(-utcOffsetMinutes);

            // Load the raw rows, then count them per day in C#.
            List<DateTime> enrollmentDates = await _db.Enrollments
                .Where(e => e.Course.InstructorId == userId && e.EnrolledAt >= firstDay)
                .Select(e => e.EnrolledAt)
                .ToListAsync();

            List<Payment> payments = await _db.Payments
                .Where(p => p.Course.InstructorId == userId && p.Status == "Paid" && p.CreatedAt >= firstDay)
                .ToListAsync();

            List<DailyStatDto> daily = new List<DailyStatDto>();
            for (int i = 0; i < days; i++)
            {
                DateTime day = firstLocalDay.AddDays(i);
                daily.Add(new DailyStatDto
                {
                    Date = day.ToString("yyyy-MM-dd"),
                    // Move each UTC time into the instructor's time zone before taking its date.
                    Enrollments = enrollmentDates.Count(d => d.AddMinutes(utcOffsetMinutes).Date == day),
                    Revenue = payments.Where(p => p.CreatedAt.AddMinutes(utcOffsetMinutes).Date == day).Sum(p => p.Amount)
                });
            }

            int completed = await _db.Enrollments
                .CountAsync(e => e.Course.InstructorId == userId && e.CompletedAt != null);

            int unanswered = await _db.Questions
                .CountAsync(q => q.Course.InstructorId == userId && !q.Answers.Any(a => a.UserId == userId));

            return Ok(new InstructorAnalyticsDto
            {
                Days = days,
                Enrollments = enrollmentDates.Count,
                Revenue = payments.Sum(p => p.Amount),
                CompletedStudents = completed,
                UnansweredQuestions = unanswered,
                Daily = daily
            });
        }

        // POST api/instructor/courses  -> creates a new draft course
        [HttpPost("courses")]
        public async Task<ActionResult> CreateCourse(CourseSaveRequest request)
        {
            if (!await CategoryExistsAsync(request.CategoryId))
            {
                return ErrorMessage(400, "This category does not exist anymore. Please choose another one.");
            }

            Course course = new Course
            {
                InstructorId = GetUserId(),
                IsPublished = false,
                CreatedAt = DateTime.UtcNow
            };
            CopyRequestToCourse(request, course);

            _db.Courses.Add(course);
            await _db.SaveChangesAsync();

            return Ok(new { id = course.Id });
        }

        // PUT api/instructor/courses/5
        [HttpPut("courses/{id}")]
        public async Task<ActionResult> UpdateCourse(int id, CourseSaveRequest request)
        {
            Course course = await FindMyCourseAsync(id);
            if (course == null)
            {
                return ErrorMessage(404, "Course not found.");
            }

            if (!await CategoryExistsAsync(request.CategoryId))
            {
                return ErrorMessage(400, "This category does not exist anymore. Please choose another one.");
            }

            CopyRequestToCourse(request, course);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Course saved." });
        }

        // DELETE api/instructor/courses/5  (only when no student has enrolled yet)
        [HttpDelete("courses/{id}")]
        public async Task<ActionResult> DeleteCourse(int id)
        {
            Course course = await _db.Courses
                .Include(c => c.Sections)
                    .ThenInclude(s => s.Lectures)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null || !IsMine(course))
            {
                return ErrorMessage(404, "Course not found.");
            }

            bool hasStudents = await _db.Enrollments.AnyAsync(e => e.CourseId == id);
            if (hasStudents)
            {
                return ErrorMessage(400, "This course has students, so it cannot be deleted. Unpublish it instead.");
            }

            // Remember the uploaded files before the course is gone.
            List<string> videoIds = new List<string> { course.PromoVideoPublicId };
            foreach (Section section in course.Sections)
            {
                foreach (Lecture lecture in section.Lectures)
                {
                    videoIds.Add(lecture.VideoPublicId);
                }
            }
            string thumbnailId = course.ThumbnailPublicId;

            // Sections, lectures, quizzes and coupons are deleted together with the course (cascade delete).
            _db.Courses.Remove(course);
            await _db.SaveChangesAsync();

            // Only now, when the database delete worked, remove the files.
            await _fileStorage.DeleteFileAsync(thumbnailId, false);
            foreach (string videoId in videoIds)
            {
                await _fileStorage.DeleteFileAsync(videoId, true);
            }

            return NoContent();
        }

        // POST api/instructor/courses/5/thumbnail   (form-data: file)
        [HttpPost("courses/{id}/thumbnail")]
        public async Task<ActionResult> UploadThumbnail(int id, IFormFile file)
        {
            Course course = await FindMyCourseAsync(id);
            if (course == null)
            {
                return ErrorMessage(404, "Course not found.");
            }

            try
            {
                UploadedFile uploaded = await _fileStorage.UploadImageAsync(file);
                await _fileStorage.DeleteFileAsync(course.ThumbnailPublicId, false);

                course.ThumbnailUrl = uploaded.Url;
                course.ThumbnailPublicId = uploaded.PublicId;
                course.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(new { url = uploaded.Url });
            }
            catch (Exception ex)
            {
                return ErrorMessage(400, ex.Message);
            }
        }

        // POST api/instructor/courses/5/promo-video   (form-data: file)
        [HttpPost("courses/{id}/promo-video")]
        public async Task<ActionResult> UploadPromoVideo(int id, IFormFile file)
        {
            Course course = await FindMyCourseAsync(id);
            if (course == null)
            {
                return ErrorMessage(404, "Course not found.");
            }

            try
            {
                UploadedFile uploaded = await _fileStorage.UploadVideoAsync(file);
                await _fileStorage.DeleteFileAsync(course.PromoVideoPublicId, true);

                course.PromoVideoUrl = uploaded.Url;
                course.PromoVideoPublicId = uploaded.PublicId;
                course.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(new { url = uploaded.Url });
            }
            catch (Exception ex)
            {
                return ErrorMessage(400, ex.Message);
            }
        }

        // POST api/instructor/courses/5/publish
        [HttpPost("courses/{id}/publish")]
        public async Task<ActionResult> Publish(int id)
        {
            Course course = await _db.Courses
                .Include(c => c.Sections)
                    .ThenInclude(s => s.Lectures)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null || !IsMine(course))
            {
                return ErrorMessage(404, "Course not found.");
            }

            // A course needs a few things before students can see it.
            List<string> problems = new List<string>();
            if (string.IsNullOrWhiteSpace(course.Subtitle)) problems.Add("add a subtitle");
            if (string.IsNullOrWhiteSpace(course.Description)) problems.Add("add a description");
            if (string.IsNullOrWhiteSpace(course.ThumbnailUrl)) problems.Add("upload a course image");
            if (course.CategoryId == null) problems.Add("choose a category");

            int lecturesWithVideo = course.Sections.SelectMany(s => s.Lectures).Count(l => !string.IsNullOrEmpty(l.VideoUrl));
            if (lecturesWithVideo == 0) problems.Add("add at least one lecture with a video");

            if (problems.Count > 0)
            {
                return ErrorMessage(400, "Before publishing, please " + string.Join(", ", problems) + ".");
            }

            // Followers hear about a course only the first time it goes live.
            bool isFirstPublish = course.PublishedAt == null;

            course.IsPublished = true;
            course.UpdatedAt = DateTime.UtcNow;
            if (isFirstPublish)
            {
                course.PublishedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();

            if (isFirstPublish)
            {
                await _notificationService.OnCoursePublishedAsync(course);
            }

            return Ok(new { message = "Your course is now live!" });
        }

        // POST api/instructor/courses/5/unpublish
        [HttpPost("courses/{id}/unpublish")]
        public async Task<ActionResult> Unpublish(int id)
        {
            Course course = await FindMyCourseAsync(id);
            if (course == null)
            {
                return ErrorMessage(404, "Course not found.");
            }

            course.IsPublished = false;
            course.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Course unpublished. Enrolled students can still watch it." });
        }

        // ---------- helpers ----------

        // Returns the course only if the logged in user owns it (admins can edit any course).
        private async Task<Course> FindMyCourseAsync(int id)
        {
            Course course = await _db.Courses.FindAsync(id);
            if (course == null || !IsMine(course))
            {
                return null;
            }
            return course;
        }

        // No category (null) is allowed; an id that is not in the database is not.
        private async Task<bool> CategoryExistsAsync(int? categoryId)
        {
            if (categoryId == null)
            {
                return true;
            }
            return await _db.Categories.AnyAsync(c => c.Id == categoryId);
        }

        private bool IsMine(Course course)
        {
            return course.InstructorId == GetUserId() || GetUserRole() == Roles.Admin;
        }

        private static void CopyRequestToCourse(CourseSaveRequest request, Course course)
        {
            course.Title = request.Title.Trim();
            course.Subtitle = request.Subtitle;
            course.Description = request.Description;
            course.WhatYouWillLearn = request.WhatYouWillLearn;
            course.Requirements = request.Requirements;
            course.Language = string.IsNullOrWhiteSpace(request.Language) ? "English" : request.Language;
            course.Level = string.IsNullOrWhiteSpace(request.Level) ? "All Levels" : request.Level;
            course.Price = request.Price;
            course.CategoryId = request.CategoryId;
            course.UpdatedAt = DateTime.UtcNow;
        }
    }
}
