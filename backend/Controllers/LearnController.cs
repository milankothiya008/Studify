using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;
using SmartLearning.Api.Services;

namespace SmartLearning.Api.Controllers
{
    // The course player: watch lectures and save progress.
    [Authorize]
    [Route("api/learn")]
    public class LearnController : BaseApiController
    {
        // A lecture counts as completed when 90% of the video has been watched.
        private const double CompleteAtPercent = 0.9;

        private readonly AppDbContext _db;
        private readonly CourseAccessService _accessService;

        public LearnController(AppDbContext db, CourseAccessService accessService)
        {
            _db = db;
            _accessService = accessService;
        }

        // GET api/learn/5  -> course curriculum with video links and my progress
        [HttpGet("{courseId}")]
        public async Task<ActionResult<PlayerDto>> GetPlayer(int courseId)
        {
            int userId = GetUserId();

            Course course = await _db.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Sections)
                    .ThenInclude(s => s.Lectures)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
            {
                return ErrorMessage(404, "Course not found.");
            }

            bool canWatch = await _accessService.CanWatchCourseAsync(userId, GetUserRole(), course);
            if (!canWatch)
            {
                return ErrorMessage(403, "You do not have access to this course. Enroll, buy it, or renew your subscription.");
            }

            Enrollment enrollment = await _db.Enrollments.FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

            List<LectureProgress> progressList = await _db.LectureProgresses
                .Where(p => p.UserId == userId && p.Lecture.Section.CourseId == courseId)
                .ToListAsync();

            List<SectionDto> sections = CourseMapper.BuildSections(course, true, progressList);
            int totalLectures = sections.Sum(s => s.Lectures.Count);
            int completedLectures = sections.Sum(s => s.Lectures.Count(l => l.IsCompleted));

            // Every lecture is done (for example because the instructor deleted the last
            // unfinished one): mark the course as completed.
            if (enrollment != null && enrollment.CompletedAt == null && totalLectures > 0 && completedLectures == totalLectures)
            {
                enrollment.CompletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            PlayerDto player = new PlayerDto
            {
                CourseId = course.Id,
                Title = course.Title,
                InstructorName = course.Instructor.FullName,
                Description = course.Description,
                Sections = sections,
                TotalLectures = totalLectures,
                CompletedLectures = completedLectures,
                ProgressPercent = CourseMapper.CalculatePercent(completedLectures, totalLectures),
                LastLectureId = enrollment != null ? enrollment.LastLectureId : null,
                IsEnrolled = enrollment != null,
                IsCourseCompleted = enrollment != null && enrollment.CompletedAt != null
            };

            return Ok(player);
        }

        // POST api/learn/lectures/9/progress   body: { "watchedSeconds": 120 }
        // The player calls this every few seconds while a video is playing.
        [HttpPost("lectures/{lectureId}/progress")]
        public async Task<ActionResult<ProgressResponse>> SaveProgress(int lectureId, ProgressRequest request)
        {
            return await UpdateProgressAsync(lectureId, request.WatchedSeconds, null);
        }

        // POST api/learn/lectures/9/complete   body: { "isCompleted": true }
        // Called when a video ends, or when the student ticks / unticks the checkbox.
        [HttpPost("lectures/{lectureId}/complete")]
        public async Task<ActionResult<ProgressResponse>> SetCompleted(int lectureId, CompleteRequest request)
        {
            return await UpdateProgressAsync(lectureId, null, request.IsCompleted);
        }

        // GET api/learn/5/certificate  -> only after the whole course is completed
        [HttpGet("{courseId}/certificate")]
        public async Task<ActionResult<CertificateDto>> GetCertificate(int courseId)
        {
            int userId = GetUserId();

            Enrollment enrollment = await _db.Enrollments
                .Include(e => e.User)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Instructor)
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

            if (enrollment == null || enrollment.CompletedAt == null)
            {
                return ErrorMessage(404, "Complete every lecture of the course to get your certificate.");
            }

            int totalSeconds = await _db.Lectures
                .Where(l => l.Section.CourseId == courseId)
                .SumAsync(l => l.DurationSeconds);

            CertificateDto certificate = new CertificateDto
            {
                StudentName = enrollment.User.FullName,
                CourseTitle = enrollment.Course.Title,
                InstructorName = enrollment.Course.Instructor.FullName,
                TotalDurationSeconds = totalSeconds,
                CompletedAt = enrollment.CompletedAt.Value,
                CertificateNumber = "SL-" + enrollment.CourseId + "-" + enrollment.UserId + "-" + enrollment.Id
            };

            return Ok(certificate);
        }

        // Shared code for the two progress endpoints above.
        // watchedSeconds or isCompleted can be null when they are not being changed.
        // isRetry is true when this method calls itself a second time (see the catch below).
        private async Task<ActionResult<ProgressResponse>> UpdateProgressAsync(int lectureId, int? watchedSeconds, bool? isCompleted, bool isRetry = false)
        {
            int userId = GetUserId();

            Lecture lecture = await _db.Lectures
                .Include(l => l.Section)
                    .ThenInclude(s => s.Course)
                .FirstOrDefaultAsync(l => l.Id == lectureId);

            if (lecture == null)
            {
                return ErrorMessage(404, "Lecture not found.");
            }

            Course course = lecture.Section.Course;

            bool canWatch = await _accessService.CanWatchCourseAsync(userId, GetUserRole(), course);
            if (!canWatch)
            {
                return ErrorMessage(403, "You do not have access to this course.");
            }

            Enrollment enrollment = await _db.Enrollments.FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == course.Id);
            if (enrollment == null)
            {
                // The instructor (or an admin) is previewing the course: nothing to save.
                return Ok(new ProgressResponse { LectureId = lectureId });
            }

            // Find the progress row, or create it the first time.
            LectureProgress progress = await _db.LectureProgresses
                .FirstOrDefaultAsync(p => p.UserId == userId && p.LectureId == lectureId);

            if (progress == null)
            {
                progress = new LectureProgress
                {
                    UserId = userId,
                    LectureId = lectureId
                };
                _db.LectureProgresses.Add(progress);
            }

            if (watchedSeconds != null)
            {
                progress.WatchedSeconds = watchedSeconds.Value;

                if (lecture.DurationSeconds > 0 && watchedSeconds.Value >= lecture.DurationSeconds * CompleteAtPercent)
                {
                    progress.IsCompleted = true;
                }
            }

            if (isCompleted != null)
            {
                progress.IsCompleted = isCompleted.Value;
            }

            progress.UpdatedAt = DateTime.UtcNow;

            // Remember where the student is, for the "Continue" button.
            enrollment.LastLectureId = lectureId;
            enrollment.LastAccessedAt = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Two saves for the same lecture arrived at the same moment and both tried
                // to create the progress row. The other one won, so try once more:
                // this time the row exists and is simply updated.
                if (isRetry)
                {
                    throw;
                }
                _db.ChangeTracker.Clear();
                return await UpdateProgressAsync(lectureId, watchedSeconds, isCompleted, true);
            }

            // Count the progress of the whole course.
            int totalLectures = await _db.Lectures.CountAsync(l => l.Section.CourseId == course.Id);
            int completedLectures = await _db.LectureProgresses.CountAsync(p =>
                p.UserId == userId && p.IsCompleted && p.Lecture.Section.CourseId == course.Id);

            // The first time every lecture is completed, the course is completed.
            if (totalLectures > 0 && completedLectures == totalLectures && enrollment.CompletedAt == null)
            {
                enrollment.CompletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            ProgressResponse response = new ProgressResponse
            {
                LectureId = lectureId,
                IsLectureCompleted = progress.IsCompleted,
                TotalLectures = totalLectures,
                CompletedLectures = completedLectures,
                ProgressPercent = CourseMapper.CalculatePercent(completedLectures, totalLectures),
                IsCourseCompleted = enrollment.CompletedAt != null
            };

            return Ok(response);
        }
    }
}
