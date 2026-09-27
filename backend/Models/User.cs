namespace SmartLearning.Api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }

        // We never store the real password, only a BCrypt hash of it.
        public string PasswordHash { get; set; }

        // "Student", "Instructor" or "Admin" (see Roles.cs)
        public string Role { get; set; }

        // Shown in the instructor part of a course page, e.g. "Senior .NET Developer"
        public string Headline { get; set; }
        public string Bio { get; set; }

        public string ProfileImageUrl { get; set; }
        public string ProfileImagePublicId { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public List<Course> CoursesTaught { get; set; } = new List<Course>();
        public List<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
