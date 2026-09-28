namespace SmartLearning.Api.Models
{
    // A multiple-choice quiz at the end of a section. A section has at most one quiz.
    public class Quiz
    {
        public int Id { get; set; }

        public int SectionId { get; set; }
        public Section Section { get; set; }

        public string Title { get; set; }

        // Score needed to pass, e.g. 70 = 70%.
        public int PassPercent { get; set; }

        public List<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    }

    // One question with four possible answers.
    public class QuizQuestion
    {
        public int Id { get; set; }

        public int QuizId { get; set; }
        public Quiz Quiz { get; set; }

        public string Text { get; set; }

        public string Option1 { get; set; }
        public string Option2 { get; set; }
        public string Option3 { get; set; }
        public string Option4 { get; set; }

        // The right option: 1, 2, 3 or 4.
        public int CorrectOption { get; set; }

        // Shown after answering, e.g. "Because an int cannot hold decimals."
        public string Explanation { get; set; }

        public int OrderIndex { get; set; }
    }

    // One time a student took a quiz.
    public class QuizAttempt
    {
        public int Id { get; set; }

        public int QuizId { get; set; }
        public Quiz Quiz { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int CorrectCount { get; set; }
        public int TotalQuestions { get; set; }
        public int ScorePercent { get; set; }
        public bool Passed { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
