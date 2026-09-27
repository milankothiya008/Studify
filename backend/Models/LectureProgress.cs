namespace SmartLearning.Api.Models
{
    // How much of one lecture a student has watched.
    public class LectureProgress
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int LectureId { get; set; }
        public Lecture Lecture { get; set; }

        // Where the student stopped in the video, so they can resume from there.
        public int WatchedSeconds { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
