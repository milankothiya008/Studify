namespace SmartLearning.Api.Models
{
    // How the student got into the course.
    public static class AccessTypes
    {
        public const string Free = "Free";                 // the course costs nothing
        public const string Purchased = "Purchased";       // the student bought this course
        public const string Subscription = "Subscription"; // works only while a subscription is active
    }

    // A student who joined a course.
    public class Enrollment
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int CourseId { get; set; }
        public Course Course { get; set; }

        // See AccessTypes above.
        public string AccessType { get; set; }

        public DateTime EnrolledAt { get; set; }

        // Used for the "Continue where you left off" button.
        public int? LastLectureId { get; set; }
        public DateTime? LastAccessedAt { get; set; }

        // Filled when every lecture of the course is completed.
        public DateTime? CompletedAt { get; set; }
    }
}
