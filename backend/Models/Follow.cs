namespace SmartLearning.Api.Models
{
    // A user follows an instructor, and gets a notification when the instructor publishes a new course.
    public class Follow
    {
        public int Id { get; set; }

        // The user who clicked "Follow".
        public int FollowerId { get; set; }
        public User Follower { get; set; }

        // The instructor being followed.
        public int InstructorId { get; set; }
        public User Instructor { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
