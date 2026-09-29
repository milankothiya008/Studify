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

        // False until the user types the 6-digit code we emailed at sign up.
        // Users who are not verified cannot log in.
        public bool IsEmailVerified { get; set; }

        // A random value that is copied into every login token. It changes when the role or
        // the password changes, and then all older tokens stop working (see Program.cs).
        public string SecurityStamp { get; set; }

        // Shown in the instructor part of a course page, e.g. "Senior .NET Developer"
        public string Headline { get; set; }
        public string Bio { get; set; }

        public string ProfileImageUrl { get; set; }
        public string ProfileImagePublicId { get; set; }

        // Links shown on the public instructor profile (all optional, always http or https).
        public string WebsiteUrl { get; set; }
        public string LinkedInUrl { get; set; }
        public string YouTubeUrl { get; set; }
        public string TwitterUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public List<Course> CoursesTaught { get; set; } = new List<Course>();
        public List<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
