namespace SmartLearning.Api.Models
{
    // What a one-time code is for.
    public static class CodePurposes
    {
        public const string VerifyEmail = "VerifyEmail";     // sent at sign up
        public const string ResetPassword = "ResetPassword"; // sent by "Forgot password?"
    }

    // A 6-digit one-time code (OTP) that we emailed to someone.
    public class EmailCode
    {
        public int Id { get; set; }

        public string Email { get; set; }

        // See CodePurposes above.
        public string Purpose { get; set; }

        // Like passwords, the code itself is never stored, only a BCrypt hash of it.
        public string CodeHash { get; set; }

        // Wrong guesses so far. After 5 the code stops working.
        public int Attempts { get; set; }

        // True once the code has been used successfully.
        public bool IsUsed { get; set; }

        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
