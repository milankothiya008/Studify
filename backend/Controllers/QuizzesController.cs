using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;
using SmartLearning.Api.Services;

namespace SmartLearning.Api.Controllers
{
    // Multiple-choice quizzes at the end of a section.
    //   api/instructor/...  -> the instructor builds the quiz
    //   api/learn/quizzes/  -> students take it
    [Authorize]
    [Route("api")]
    public class QuizzesController : BaseApiController
    {
        private readonly AppDbContext _db;
        private readonly CourseAccessService _accessService;

        public QuizzesController(AppDbContext db, CourseAccessService accessService)
        {
            _db = db;
            _accessService = accessService;
        }

        // =====================================================================
        // Instructor
        // =====================================================================

        // GET api/instructor/quizzes/3  -> the quiz with the correct answers
        [Authorize(Roles = Roles.Instructor + "," + Roles.Admin)]
        [HttpGet("instructor/quizzes/{id}")]
        public async Task<ActionResult<QuizEditDto>> GetQuizForEditing(int id)
        {
            Quiz quiz = await FindMyQuizAsync(id);
            if (quiz == null)
            {
                return ErrorMessage(404, "Quiz not found.");
            }

            int attempts = await _db.QuizAttempts.CountAsync(a => a.QuizId == id);

            return Ok(new QuizEditDto
            {
                Id = quiz.Id,
                SectionId = quiz.SectionId,
                Title = quiz.Title,
                PassPercent = quiz.PassPercent,
                AttemptCount = attempts,
                Questions = quiz.Questions
                    .OrderBy(q => q.OrderIndex)
                    .Select(q => new QuizQuestionEditDto
                    {
                        Id = q.Id,
                        Text = q.Text,
                        Option1 = q.Option1,
                        Option2 = q.Option2,
                        Option3 = q.Option3,
                        Option4 = q.Option4,
                        CorrectOption = q.CorrectOption,
                        Explanation = q.Explanation
                    })
                    .ToList()
            });
        }

        // POST api/instructor/sections/7/quiz   body: { "title": "...", "passPercent": 70 }
        [Authorize(Roles = Roles.Instructor + "," + Roles.Admin)]
        [HttpPost("instructor/sections/{sectionId}/quiz")]
        public async Task<ActionResult> CreateQuiz(int sectionId, QuizSaveRequest request)
        {
            Section section = await _db.Sections
                .Include(s => s.Course)
                .Include(s => s.Quiz)
                .FirstOrDefaultAsync(s => s.Id == sectionId);

            if (section == null || !IsMine(section.Course))
            {
                return ErrorMessage(404, "Section not found.");
            }

            if (section.Quiz != null)
            {
                return ErrorMessage(400, "This section already has a quiz.");
            }

            Quiz quiz = new Quiz
            {
                SectionId = sectionId,
                Title = request.Title.Trim(),
                PassPercent = request.PassPercent
            };
            _db.Quizzes.Add(quiz);
            await _db.SaveChangesAsync();

            return Ok(new { id = quiz.Id, message = "Quiz created. Now add some questions." });
        }

        // PUT api/instructor/quizzes/3
        [Authorize(Roles = Roles.Instructor + "," + Roles.Admin)]
        [HttpPut("instructor/quizzes/{id}")]
        public async Task<ActionResult> UpdateQuiz(int id, QuizSaveRequest request)
        {
            Quiz quiz = await FindMyQuizAsync(id);
            if (quiz == null)
            {
                return ErrorMessage(404, "Quiz not found.");
            }

            quiz.Title = request.Title.Trim();
            quiz.PassPercent = request.PassPercent;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Quiz saved." });
        }

        // DELETE api/instructor/quizzes/3  (its questions and attempts are deleted too)
        [Authorize(Roles = Roles.Instructor + "," + Roles.Admin)]
        [HttpDelete("instructor/quizzes/{id}")]
        public async Task<ActionResult> DeleteQuiz(int id)
        {
            Quiz quiz = await FindMyQuizAsync(id);
            if (quiz == null)
            {
                return ErrorMessage(404, "Quiz not found.");
            }

            _db.Quizzes.Remove(quiz);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // POST api/instructor/quizzes/3/questions
        [Authorize(Roles = Roles.Instructor + "," + Roles.Admin)]
        [HttpPost("instructor/quizzes/{id}/questions")]
        public async Task<ActionResult> AddQuestion(int id, QuizQuestionSaveRequest request)
        {
            Quiz quiz = await FindMyQuizAsync(id);
            if (quiz == null)
            {
                return ErrorMessage(404, "Quiz not found.");
            }

            int lastOrder = 0;
            if (quiz.Questions.Count > 0)
            {
                lastOrder = quiz.Questions.Max(q => q.OrderIndex);
            }

            QuizQuestion question = new QuizQuestion { QuizId = id, OrderIndex = lastOrder + 1 };
            CopyQuestion(request, question);
            _db.QuizQuestions.Add(question);
            await _db.SaveChangesAsync();

            return Ok(new { id = question.Id, message = "Question added." });
        }

        // PUT api/instructor/quiz-questions/11
        [Authorize(Roles = Roles.Instructor + "," + Roles.Admin)]
        [HttpPut("instructor/quiz-questions/{id}")]
        public async Task<ActionResult> UpdateQuestion(int id, QuizQuestionSaveRequest request)
        {
            QuizQuestion question = await FindMyQuestionAsync(id);
            if (question == null)
            {
                return ErrorMessage(404, "Question not found.");
            }

            CopyQuestion(request, question);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Question saved." });
        }

        // DELETE api/instructor/quiz-questions/11
        [Authorize(Roles = Roles.Instructor + "," + Roles.Admin)]
        [HttpDelete("instructor/quiz-questions/{id}")]
        public async Task<ActionResult> DeleteQuestion(int id)
        {
            QuizQuestion question = await FindMyQuestionAsync(id);
            if (question == null)
            {
                return ErrorMessage(404, "Question not found.");
            }

            _db.QuizQuestions.Remove(question);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // =====================================================================
        // Students
        // =====================================================================

        // GET api/learn/quizzes/3  -> the questions WITHOUT the correct answers
        [HttpGet("learn/quizzes/{id}")]
        public async Task<ActionResult<QuizPlayDto>> GetQuizToTake(int id)
        {
            int userId = GetUserId();
            Quiz quiz = await LoadQuizAsync(id);
            if (quiz == null)
            {
                return ErrorMessage(404, "Quiz not found.");
            }

            if (!await _accessService.CanWatchCourseAsync(userId, GetUserRole(), quiz.Section.Course))
            {
                return ErrorMessage(403, "You do not have access to this course.");
            }

            List<QuizAttempt> attempts = await _db.QuizAttempts
                .Where(a => a.QuizId == id && a.UserId == userId)
                .ToListAsync();

            return Ok(new QuizPlayDto
            {
                Id = quiz.Id,
                CourseId = quiz.Section.CourseId,
                Title = quiz.Title,
                SectionTitle = quiz.Section.Title,
                PassPercent = quiz.PassPercent,
                AttemptCount = attempts.Count,
                BestScore = attempts.Count > 0 ? attempts.Max(a => a.ScorePercent) : null,
                Questions = quiz.Questions
                    .OrderBy(q => q.OrderIndex)
                    .Select(q => new QuizPlayQuestionDto
                    {
                        Id = q.Id,
                        Text = q.Text,
                        Options = new List<string> { q.Option1, q.Option2, q.Option3, q.Option4 }
                    })
                    .ToList()
            });
        }

        // POST api/learn/quizzes/3/attempts   body: { "answers": [ { "questionId": 11, "selectedOption": 2 }, ... ] }
        // Grades the answers on the server and returns the correct answers with explanations.
        [HttpPost("learn/quizzes/{id}/attempts")]
        public async Task<ActionResult<QuizResultDto>> SubmitAttempt(int id, QuizAttemptRequest request)
        {
            int userId = GetUserId();
            Quiz quiz = await LoadQuizAsync(id);
            if (quiz == null)
            {
                return ErrorMessage(404, "Quiz not found.");
            }

            Course course = quiz.Section.Course;
            if (!await _accessService.CanWatchCourseAsync(userId, GetUserRole(), course))
            {
                return ErrorMessage(403, "You do not have access to this course.");
            }

            if (quiz.Questions.Count == 0)
            {
                return ErrorMessage(400, "This quiz has no questions yet.");
            }

            List<QuizQuestionResultDto> results = new List<QuizQuestionResultDto>();
            int correct = 0;

            foreach (QuizQuestion question in quiz.Questions.OrderBy(q => q.OrderIndex))
            {
                QuizAnswerRequest answer = request.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
                int selected = answer != null ? answer.SelectedOption : 0;
                bool isCorrect = selected == question.CorrectOption;
                if (isCorrect)
                {
                    correct++;
                }

                results.Add(new QuizQuestionResultDto
                {
                    QuestionId = question.Id,
                    SelectedOption = selected,
                    CorrectOption = question.CorrectOption,
                    IsCorrect = isCorrect,
                    Explanation = question.Explanation
                });
            }

            int total = quiz.Questions.Count;
            int score = correct * 100 / total;
            bool passed = score >= quiz.PassPercent;

            // Save the attempt for enrolled students (not for the instructor previewing the course).
            bool isEnrolled = await _db.Enrollments.AnyAsync(e => e.UserId == userId && e.CourseId == course.Id);
            if (isEnrolled)
            {
                _db.QuizAttempts.Add(new QuizAttempt
                {
                    QuizId = id,
                    UserId = userId,
                    CorrectCount = correct,
                    TotalQuestions = total,
                    ScorePercent = score,
                    Passed = passed,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }

            int? best = await _db.QuizAttempts
                .Where(a => a.QuizId == id && a.UserId == userId)
                .MaxAsync(a => (int?)a.ScorePercent);

            return Ok(new QuizResultDto
            {
                CorrectCount = correct,
                TotalQuestions = total,
                ScorePercent = score,
                PassPercent = quiz.PassPercent,
                Passed = passed,
                BestScore = best,
                Results = results
            });
        }

        // ---------- helpers ----------

        private bool IsMine(Course course)
        {
            return course.InstructorId == GetUserId() || GetUserRole() == Roles.Admin;
        }

        private async Task<Quiz> LoadQuizAsync(int id)
        {
            return await _db.Quizzes
                .Include(q => q.Questions)
                .Include(q => q.Section)
                    .ThenInclude(s => s.Course)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        private async Task<Quiz> FindMyQuizAsync(int id)
        {
            Quiz quiz = await LoadQuizAsync(id);
            if (quiz == null || !IsMine(quiz.Section.Course))
            {
                return null;
            }
            return quiz;
        }

        private async Task<QuizQuestion> FindMyQuestionAsync(int id)
        {
            QuizQuestion question = await _db.QuizQuestions
                .Include(q => q.Quiz)
                    .ThenInclude(z => z.Section)
                        .ThenInclude(s => s.Course)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null || !IsMine(question.Quiz.Section.Course))
            {
                return null;
            }
            return question;
        }

        private static void CopyQuestion(QuizQuestionSaveRequest request, QuizQuestion question)
        {
            question.Text = request.Text.Trim();
            question.Option1 = request.Option1.Trim();
            question.Option2 = request.Option2.Trim();
            question.Option3 = request.Option3.Trim();
            question.Option4 = request.Option4.Trim();
            question.CorrectOption = request.CorrectOption;
            question.Explanation = request.Explanation;
        }
    }
}
