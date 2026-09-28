namespace SmartLearning.Api.Models
{
    // A private note a student writes while watching a lecture,
    // saved together with the second of the video it belongs to.
    public class Note
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int CourseId { get; set; }

        public int LectureId { get; set; }
        public Lecture Lecture { get; set; }

        // Position in the video, e.g. 83 = 1:23
        public int Seconds { get; set; }

        public string Text { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
