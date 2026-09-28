namespace SmartLearning.Api.Models
{
    // A question a student asked in a course (Q&A), usually about one lecture.
    public class Question
    {
        public int Id { get; set; }

        public int CourseId { get; set; }
        public Course Course { get; set; }

        // The lecture it is about. Null = a general question about the course.
        public int? LectureId { get; set; }
        public Lecture Lecture { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public string Title { get; set; }
        public string Body { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<Answer> Answers { get; set; } = new List<Answer>();
    }

    // A reply to a question, from the instructor or another student.
    public class Answer
    {
        public int Id { get; set; }

        public int QuestionId { get; set; }
        public Question Question { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public string Body { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
