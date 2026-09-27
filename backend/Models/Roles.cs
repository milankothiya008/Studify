namespace SmartLearning.Api.Models
{
    // The three kinds of users in the platform.
    // They are stored as plain text in the Users.Role column.
    public static class Roles
    {
        public const string Student = "Student";
        public const string Instructor = "Instructor";
        public const string Admin = "Admin";
    }
}
