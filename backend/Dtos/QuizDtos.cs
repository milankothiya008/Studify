using System.ComponentModel.DataAnnotations;

namespace SmartLearning.Api.Dtos
{
    // Short info about a section's quiz, shown in the curriculum.
    public class QuizInfoDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int QuestionCount { get; set; }
        public int PassPercent { get; set; }

        // For the logged in student (null if never taken).
        public int? BestScore { get; set; }
        public bool Passed { get; set; }
    }

    // ---------- Instructor: building a quiz ----------

    public class QuizSaveRequest
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Range(1, 100)]
        public int PassPercent { get; set; }
    }

    public class QuizQuestionSaveRequest
    {
        [Required]
        [StringLength(1000)]
        public string Text { get; set; }

        [Required] [StringLength(500)] public string Option1 { get; set; }
        [Required] [StringLength(500)] public string Option2 { get; set; }
        [Required] [StringLength(500)] public string Option3 { get; set; }
        [Required] [StringLength(500)] public string Option4 { get; set; }

        [Range(1, 4, ErrorMessage = "Choose the correct answer.")]
        public int CorrectOption { get; set; }

        [StringLength(1000)]
        public string Explanation { get; set; }
    }

    public class QuizQuestionEditDto
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string Option1 { get; set; }
        public string Option2 { get; set; }
        public string Option3 { get; set; }
        public string Option4 { get; set; }
        public int CorrectOption { get; set; }
        public string Explanation { get; set; }
    }

    public class QuizEditDto
    {
        public int Id { get; set; }
        public int SectionId { get; set; }
        public string Title { get; set; }
        public int PassPercent { get; set; }
        public int AttemptCount { get; set; }
        public List<QuizQuestionEditDto> Questions { get; set; }
    }

    // ---------- Student: taking a quiz ----------

    // Questions WITHOUT the correct answers.
    public class QuizPlayQuestionDto
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public List<string> Options { get; set; }
    }

    public class QuizPlayDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string SectionTitle { get; set; }
        public int PassPercent { get; set; }
        public int AttemptCount { get; set; }
        public int? BestScore { get; set; }
        public List<QuizPlayQuestionDto> Questions { get; set; }
    }

    public class QuizAnswerRequest
    {
        public int QuestionId { get; set; }

        // 1 - 4, or 0 when the question was skipped.
        [Range(0, 4)]
        public int SelectedOption { get; set; }
    }

    public class QuizAttemptRequest
    {
        [Required]
        public List<QuizAnswerRequest> Answers { get; set; }
    }

    public class QuizQuestionResultDto
    {
        public int QuestionId { get; set; }
        public int SelectedOption { get; set; }
        public int CorrectOption { get; set; }
        public bool IsCorrect { get; set; }
        public string Explanation { get; set; }
    }

    public class QuizResultDto
    {
        public int CorrectCount { get; set; }
        public int TotalQuestions { get; set; }
        public int ScorePercent { get; set; }
        public int PassPercent { get; set; }
        public bool Passed { get; set; }
        public int? BestScore { get; set; }
        public List<QuizQuestionResultDto> Results { get; set; }
    }
}
