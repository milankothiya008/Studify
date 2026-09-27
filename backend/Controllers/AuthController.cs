using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;
using SmartLearning.Api.Services;

namespace SmartLearning.Api.Controllers
{
    [Route("api/auth")]
    public class AuthController : BaseApiController
    {
        private readonly AppDbContext _db;
        private readonly TokenService _tokenService;

        public AuthController(AppDbContext db, TokenService tokenService)
        {
            _db = db;
            _tokenService = tokenService;
        }

        // POST api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            if (request.Role != Roles.Student && request.Role != Roles.Instructor)
            {
                return ErrorMessage(400, "Role must be Student or Instructor.");
            }

            string email = request.Email.Trim().ToLower();

            bool emailTaken = await _db.Users.AnyAsync(u => u.Email == email);
            if (emailTaken)
            {
                return ErrorMessage(400, "This email is already registered.");
            }

            User user = new User
            {
                FullName = request.FullName.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(CreateAuthResponse(user));
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
