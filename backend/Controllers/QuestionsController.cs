using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;
using SmartLearning.Api.Services;

namespace SmartLearning.Api.Controllers
{
    // Q&A: students ask questions about a course (usually one lecture),
    // the instructor and other students answer.
    // Only people who can watch the course (students with access, the instructor, admins) can use it.
    [Authorize]
    [Route("api")]
    public class QuestionsController : BaseApiController
    {
        private readonly AppDbContext _db;
        private readonly CourseAccessService _accessService;
        private readonly NotificationService _notificationService;

        public QuestionsController(AppDbContext db, CourseAccessService accessService, NotificationService notificationService)
        {
            _db = db;
            _accessService = accessService;
            _notificationService = notificationService;
        }

        // GET api/courses/5/questions                -> all questions of the course
        // GET api/courses/5/questions?lectureId=9    -> questions about one lecture
        // GET api/courses/5/questions?search=loop    -> questions containing a word
        [HttpGet("courses/{courseId}/questions")]
        public async Task<ActionResult<List<QuestionSummaryDto>>> GetQuestions(int courseId, int? lectureId, string search)
        {
            Course course = await _db.Courses.FindAsync(courseId);
            if (course == null || !await _accessService.CanWatchCourseAsync(GetUserId(), GetUserRole(), course))
            {
                return ErrorMessage(403, "Enroll in this course to see its Q&A.");
            }

            IQueryable<Question> query = _db.Questions.Where(q => q.CourseId == courseId);

            if (lectureId != null)
            {
                query = query.Where(q => q.LectureId == lectureId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string text = search.Trim().ToLower();
                query = query.Where(q => q.Title.ToLower().Contains(text) || q.Body.ToLower().Contains(text));
            }

            List<QuestionSummaryDto> questions = await SelectSummaries(query.OrderByDescending(q => q.CreatedAt).Take(100))
                .ToListAsync();

            return Ok(questions);
        }

        // GET api/questions/12  -> one question with all its answers
        [HttpGet("questions/{id}")]
        public async Task<ActionResult<QuestionDetailDto>> GetQuestion(int id)
        {
            Question question = await _db.Questions.Include(q => q.Course).FirstOrDefaultAsync(q => q.Id == id);
            if (question == null)
            {
                return ErrorMessage(404, "Question not found.");
            }

            int userId = GetUserId();
            if (!await _accessService.CanWatchCourseAsync(userId, GetUserRole(), question.Course))
            {
                return ErrorMessage(403, "Enroll in this course to see its Q&A.");
            }

            QuestionSummaryDto summary = await SelectSummaries(_db.Questions.Where(q => q.Id == id)).FirstAsync();
            int instructorId = question.Course.InstructorId;
            bool isModerator = userId == instructorId || GetUserRole() == Roles.Admin;

            List<AnswerDto> answers = await _db.Answers
                .Where(a => a.QuestionId == id)
                .OrderBy(a => a.CreatedAt)
                .Select(a => new AnswerDto
                {
                    Id = a.Id,
                    Body = a.Body,
                    UserName = a.User.FullName,
                    UserImageUrl = a.User.ProfileImageUrl,
                    IsInstructor = a.UserId == instructorId,
                    CanDelete = a.UserId == userId || isModerator,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(new QuestionDetailDto
            {
                Question = summary,
                CanDelete = question.UserId == userId || isModerator,
                Answers = answers
            });
        }

        // POST api/courses/5/questions   body: { "lectureId": 9, "title": "...", "body": "..." }
        [HttpPost("courses/{courseId}/questions")]
        public async Task<ActionResult> AskQuestion(int courseId, QuestionSaveRequest request)
        {
            int userId = GetUserId();
            Course course = await _db.Courses.FindAsync(courseId);
            if (course == null || !await _accessService.CanWatchCourseAsync(userId, GetUserRole(), course))
            {
                return ErrorMessage(403, "Enroll in this course to ask questions.");
            }

            string lectureTitle = null;
            if (request.LectureId != null)
            {
                Lecture lecture = await _db.Lectures
                    .FirstOrDefaultAsync(l => l.Id == request.LectureId && l.Section.CourseId == courseId);
                if (lecture == null)
                {
                    return ErrorMessage(400, "This lecture is not part of the course.");
                }
                lectureTitle = lecture.Title;
            }

            Question question = new Question
            {
                CourseId = courseId,
                LectureId = request.LectureId,
                UserId = userId,
                Title = request.Title.Trim(),
                Body = (request.Body ?? "").Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _db.Questions.Add(question);
            await _db.SaveChangesAsync();

            // Tell the instructor (unless they asked it themselves).
            if (course.InstructorId != userId)
            {
                string where = lectureTitle != null ? " (lecture: " + lectureTitle + ")" : "";
                await _notificationService.NotifyAsync(course.InstructorId, "New question in " + course.Title,
                    "\"" + question.Title + "\"" + where, "/instructor/questions");
            }

            return Ok(new { id = question.Id, message = "Your question was posted." });
        }

        // POST api/questions/12/answers   body: { "body": "..." }
        [HttpPost("questions/{id}/answers")]
        public async Task<ActionResult> AddAnswer(int id, AnswerSaveRequest request)
        {
            int userId = GetUserId();
            Question question = await _db.Questions.Include(q => q.Course).FirstOrDefaultAsync(q => q.Id == id);
            if (question == null)
            {
                return ErrorMessage(404, "Question not found.");
            }

            if (!await _accessService.CanWatchCourseAsync(userId, GetUserRole(), question.Course))
            {
                return ErrorMessage(403, "Enroll in this course to answer questions.");
            }

            Answer answer = new Answer
            {
                QuestionId = id,
                UserId = userId,
                Body = request.Body.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _db.Answers.Add(answer);
            await _db.SaveChangesAsync();

            // Bell + email for the person who asked.
            User answeredBy = await _db.Users.FindAsync(userId);
            await _notificationService.OnAnsweredAsync(question, answeredBy, answer.Body);

            return Ok(new { id = answer.Id, message = "Your answer was posted." });
        }

        // DELETE api/questions/12   (the author, the course instructor or an admin)
        [HttpDelete("questions/{id}")]
        public async Task<ActionResult> DeleteQuestion(int id)
        {
            Question question = await _db.Questions.Include(q => q.Course).FirstOrDefaultAsync(q => q.Id == id);
            if (question == null)
            {
                return ErrorMessage(404, "Question not found.");
            }

            if (!CanModerate(question.UserId, question.Course))
            {
                return ErrorMessage(403, "You cannot delete this question.");
            }

            _db.Questions.Remove(question); // its answers are deleted too
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE api/answers/30   (the author, the course instructor or an admin)
        [HttpDelete("answers/{id}")]
        public async Task<ActionResult> DeleteAnswer(int id)
        {
            Answer answer = await _db.Answers
                .Include(a => a.Question)
                    .ThenInclude(q => q.Course)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (answer == null)
            {
                return ErrorMessage(404, "Answer not found.");
            }

            if (!CanModerate(answer.UserId, answer.Question.Course))
            {
                return ErrorMessage(403, "You cannot delete this answer.");
            }

            _db.Answers.Remove(answer);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // GET api/instructor/questions                   -> questions in all my courses
        // GET api/instructor/questions?unanswered=true   -> only questions I have not answered yet
        [Authorize(Roles = Roles.Instructor + "," + Roles.Admin)]
        [HttpGet("instructor/questions")]
        public async Task<ActionResult<List<QuestionSummaryDto>>> GetInstructorQuestions(bool unanswered = false)
        {
            int userId = GetUserId();
            IQueryable<Question> query = _db.Questions.Where(q => q.Course.InstructorId == userId);

            if (unanswered)
            {
                query = query.Where(q => !q.Answers.Any(a => a.UserId == userId));
            }

            List<QuestionSummaryDto> questions = await SelectSummaries(query.OrderByDescending(q => q.CreatedAt).Take(200))
                .ToListAsync();

            return Ok(questions);
        }

        // ---------- helpers ----------

        private bool CanModerate(int authorId, Course course)
        {
            int userId = GetUserId();
            return authorId == userId || course.InstructorId == userId || GetUserRole() == Roles.Admin;
        }

        private IQueryable<QuestionSummaryDto> SelectSummaries(IQueryable<Question> questions)
        {
            int userId = GetUserId();
            return questions.Select(q => new QuestionSummaryDto
            {
                Id = q.Id,
                CourseId = q.CourseId,
                CourseTitle = q.Course.Title,
                LectureId = q.LectureId,
                LectureTitle = q.Lecture != null ? q.Lecture.Title : null,
                Title = q.Title,
                Body = q.Body,
                UserName = q.User.FullName,
                UserImageUrl = q.User.ProfileImageUrl,
                AnswerCount = q.Answers.Count,
                HasInstructorAnswer = q.Answers.Any(a => a.UserId == q.Course.InstructorId),
                IsMine = q.UserId == userId,
                CreatedAt = q.CreatedAt
            });
        }
    }
}
