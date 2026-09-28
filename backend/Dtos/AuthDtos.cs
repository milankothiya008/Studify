using System.ComponentModel.DataAnnotations;

namespace SmartLearning.Api.Dtos
{
    public class RegisterRequest
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
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

        public string Bio { get; set; }
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
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string NewPassword { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string NewPassword { get; set; }
    }
}
