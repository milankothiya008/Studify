using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;
using SmartLearning.Api.Services;

namespace SmartLearning.Api.Controllers
{
    // Sign up (with email verification), log in, and forgot password.
    //
    // Sign up flow:
    //   1. POST register      -> account is created (not verified) and a 6-digit code is emailed
    //   2. POST verify-email  -> the code is checked, the email is verified, the user is logged in
    //
    // Forgot password flow:
    //   1. POST forgot-password -> a 6-digit code is emailed
    //   2. POST reset-password  -> the code is checked, the password is changed, the user is logged in
    [Route("api/auth")]
    public class AuthController : BaseApiController
    {
        private readonly AppDbContext _db;
        private readonly TokenService _tokenService;
        private readonly OtpService _otpService;

        public AuthController(AppDbContext db, TokenService tokenService, OtpService otpService)
        {
            _db = db;
            _tokenService = tokenService;
            _otpService = otpService;
        }

        // POST api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request)
        {
            if (request.Role != Roles.Student && request.Role != Roles.Instructor)
            {
                return ErrorMessage(400, "Role must be Student or Instructor.");
            }

            string email = request.Email.Trim().ToLower();
            User user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user != null && user.IsEmailVerified)
            {
                return ErrorMessage(400, "This email is already registered. Please log in.");
            }

            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Users.Add(user);
            }

            // A new account, or someone who signed up earlier but never verified the email:
            // (re)write the details and send a fresh code.
            user.FullName = request.FullName.Trim();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            user.Role = request.Role;
            TokenService.RenewSecurityStamp(user);
            user.IsEmailVerified = false;
            await _db.SaveChangesAsync();

            string error = await _otpService.SendCodeAsync(user, CodePurposes.VerifyEmail);

            return Ok(new RegisterResponse
            {
                RequiresVerification = true,
                Email = email,
                // If the code could not be sent now (for example "please wait"),
                // the verify page still opens and the user can press "Resend code".
                Message = error ?? "We sent a 6-digit code to " + email + "."
            });
        }

        // POST api/auth/verify-email   body: { "email": "...", "code": "123456" }
        [HttpPost("verify-email")]
        public async Task<ActionResult<AuthResponse>> VerifyEmail(VerifyEmailRequest request)
        {
            string email = request.Email.Trim().ToLower();
            User user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return ErrorMessage(400, "No account found for this email. Please sign up.");
            }

            if (user.IsEmailVerified)
            {
                return ErrorMessage(400, "This email is already verified. Please log in.");
            }

            string error = await _otpService.CheckCodeAsync(email, CodePurposes.VerifyEmail, request.Code);
            if (error != null)
            {
                return ErrorMessage(400, error);
            }

            user.IsEmailVerified = true;
            await _db.SaveChangesAsync();

            return Ok(CreateAuthResponse(user));
        }

        // POST api/auth/resend-code   body: { "email": "..." }
        [HttpPost("resend-code")]
        public async Task<ActionResult> ResendCode(EmailOnlyRequest request)
        {
            string email = request.Email.Trim().ToLower();
            User user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || user.IsEmailVerified)
            {
                return ErrorMessage(400, "There is nothing to verify for this email.");
            }

            string error = await _otpService.SendCodeAsync(user, CodePurposes.VerifyEmail);
            if (error != null)
            {
                return ErrorMessage(429, error);
            }

            return Ok(new { message = "A new code is on its way to " + email + "." });
        }

        // POST api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            string email = request.Email.Trim().ToLower();
            User user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return ErrorMessage(401, "Wrong email or password.");
            }

            if (!user.IsEmailVerified)
            {
                // Send a fresh code (if the last one is older than a minute), and tell the
                // React app to open the "verify your email" page.
                await _otpService.SendCodeAsync(user, CodePurposes.VerifyEmail);
                return StatusCode(403, new
                {
                    message = "Please verify your email first. We sent a code to " + email + ".",
                    code = "EMAIL_NOT_VERIFIED",
                    email = email
                });
            }

            return Ok(CreateAuthResponse(user));
        }

        // POST api/auth/forgot-password   body: { "email": "..." }
        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword(EmailOnlyRequest request)
        {
            string email = request.Email.Trim().ToLower();
            User user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            // We give the same answer whether the account exists or not,
            // so nobody can use this page to find out who has an account.
            string answer = "If an account exists for " + email + ", we sent it a 6-digit code.";

            if (user == null)
            {
                return Ok(new { message = answer });
            }

            string error = await _otpService.SendCodeAsync(user, CodePurposes.ResetPassword);
            if (error != null)
            {
                return ErrorMessage(429, error);
            }

            return Ok(new { message = answer });
        }

        // POST api/auth/reset-password   body: { "email": "...", "code": "123456", "newPassword": "..." }
        [HttpPost("reset-password")]
        public async Task<ActionResult<AuthResponse>> ResetPassword(ResetPasswordRequest request)
        {
            string email = request.Email.Trim().ToLower();
            User user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return ErrorMessage(400, "Wrong code. Please check the email we sent you.");
            }

            string error = await _otpService.CheckCodeAsync(email, CodePurposes.ResetPassword, request.Code);
            if (error != null)
            {
                return ErrorMessage(400, error);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            TokenService.RenewSecurityStamp(user); // log out every other device
            // The code arrived in their inbox, so the email address is proven too.
            user.IsEmailVerified = true;
            await _db.SaveChangesAsync();

            return Ok(CreateAuthResponse(user));
        }

        // GET api/auth/me  -> the logged in user
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> Me()
        {
            User user = await _db.Users.FindAsync(GetUserId());
            if (user == null)
            {
                return ErrorMessage(401, "User not found.");
            }

            return Ok(ToUserDto(user));
        }

        private AuthResponse CreateAuthResponse(User user)
        {
            return new AuthResponse
            {
                Token = _tokenService.CreateToken(user),
                User = ToUserDto(user)
            };
        }

        public static UserDto ToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                Headline = user.Headline,
                Bio = user.Bio,
                ProfileImageUrl = user.ProfileImageUrl
            };
        }
    }
}
