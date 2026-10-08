using System.ComponentModel.DataAnnotations;

namespace SmartLearning.Api.Dtos
{
    // The rule for every new password (sign up, reset, change), checked by ASP.NET's
    // built-in [RegularExpression] validation before the controller code runs.
    // Attributes only accept constants, so the rule is written once here as const strings.
    public static class PasswordRules
    {
        // (?=.*[a-z])          at least one lowercase letter
        // (?=.*[A-Z])          at least one uppercase letter
        // (?=.*\d)             at least one number
        // (?=.*[^a-zA-Z\d\s])  at least one special character, like @ # ! $
        // .{8,128}             8 to 128 characters in total
        public const string Pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d\s]).{8,128}$";

        public const string Message =
            "Password must be at least 8 characters and include an uppercase letter, a lowercase letter, a number and a special character.";
    }

    public class RegisterRequest
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.Message)]
        public string Password { get; set; }

        // "Student" or "Instructor". Admin accounts cannot be created from the sign up page.
        [Required]
        public string Role { get; set; }
    }

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Headline { get; set; }
        public string Bio { get; set; }
        public string ProfileImageUrl { get; set; }
        public string WebsiteUrl { get; set; }
        public string LinkedInUrl { get; set; }
        public string YouTubeUrl { get; set; }
        public string TwitterUrl { get; set; }
    }

    public class AuthResponse
    {
        public string Token { get; set; }
        public UserDto User { get; set; }
    }

    public class ProfileUpdateRequest
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(150)]
        public string Headline { get; set; }

        [StringLength(5000)]
        public string Bio { get; set; }

        // Optional links. "example.com" is saved as "https://example.com".
        [StringLength(300)]
        public string WebsiteUrl { get; set; }

        [StringLength(300)]
        public string LinkedInUrl { get; set; }

        [StringLength(300)]
        public string YouTubeUrl { get; set; }

        [StringLength(300)]
        public string TwitterUrl { get; set; }
    }

    // Sent back after sign up: the account exists, but the email must be verified first.
    public class RegisterResponse
    {
        public bool RequiresVerification { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
    }

    public class VerifyEmailRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "The code has 6 digits.")]
        public string Code { get; set; }
    }

    // Used by "resend code" and by "forgot password".
    public class EmailOnlyRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class ResetPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "The code has 6 digits.")]
        public string Code { get; set; }

        [Required]
        [RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.Message)]
        public string NewPassword { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        [RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.Message)]
        public string NewPassword { get; set; }
    }
}
