using System.ComponentModel.DataAnnotations;

namespace SmartLearning.Api.Dtos
{
    // ---------------- Q&A ----------------

    public class QuestionSummaryDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public int? LectureId { get; set; }
        public string LectureTitle { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string UserName { get; set; }
        public string UserImageUrl { get; set; }
        public int AnswerCount { get; set; }
        public bool HasInstructorAnswer { get; set; }
        public bool IsMine { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AnswerDto
    {
        public int Id { get; set; }
        public string Body { get; set; }
        public string UserName { get; set; }
        public string UserImageUrl { get; set; }
        public bool IsInstructor { get; set; }
        public bool CanDelete { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class QuestionDetailDto
    {
        public QuestionSummaryDto Question { get; set; }
        public bool CanDelete { get; set; }
        public List<AnswerDto> Answers { get; set; }
    }

    public class QuestionSaveRequest
    {
        public int? LectureId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "The title must be 5 to 200 characters.")]
        public string Title { get; set; }

        [StringLength(5000)]
        public string Body { get; set; }
    }

    public class AnswerSaveRequest
    {
        [Required]
        [StringLength(5000, MinimumLength = 2)]
        public string Body { get; set; }
    }

    // ---------------- Notes ----------------

    public class NoteDto
    {
        public int Id { get; set; }
        public int LectureId { get; set; }
        public string LectureTitle { get; set; }
        public int Seconds { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NoteSaveRequest
    {
        [Range(0, 86400)]
        public int Seconds { get; set; }

        [Required]
        [StringLength(2000, MinimumLength = 1)]
        public string Text { get; set; }
    }

    // ---------------- Notifications ----------------

    public class NotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Link { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationListDto
    {
        public int UnreadCount { get; set; }
        public List<NotificationDto> Items { get; set; }
    }

    // ---------------- Certificates ----------------

    // What anybody (for example an employer) sees when checking a certificate number.
    public class CertificateCheckDto
    {
        public bool IsValid { get; set; }
        public string CertificateNumber { get; set; }
        public string StudentName { get; set; }
        public string CourseTitle { get; set; }
        public int CourseId { get; set; }
        public string InstructorName { get; set; }
        public int TotalDurationSeconds { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    // ---------------- Instructor analytics ----------------

    public class DailyStatDto
    {
        public string Date { get; set; }   // "2026-09-28"
        public int Enrollments { get; set; }
        public decimal Revenue { get; set; }
    }

    public class InstructorAnalyticsDto
    {
        public int Days { get; set; }
        public int Enrollments { get; set; }
        public decimal Revenue { get; set; }
        public int CompletedStudents { get; set; }
        public int UnansweredQuestions { get; set; }
        public List<DailyStatDto> Daily { get; set; }
    }
}
