namespace SmartLearning.Api.Models
{
    // A course a user saved for later.
    public class WishlistItem
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int CourseId { get; set; }
        public Course Course { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
