using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;
using SmartLearning.Api.Services;

namespace SmartLearning.Api.Controllers
{
    // The logged in user's own things: profile, password, subscription and purchases.
    [Authorize]
    [Route("api/account")]
    public class AccountController : BaseApiController
    {
        private readonly AppDbContext _db;
        private readonly FileStorageService _fileStorage;
        private readonly TokenService _tokenService;

        public AccountController(AppDbContext db, FileStorageService fileStorage, TokenService tokenService)
        {
            _db = db;
            _fileStorage = fileStorage;
            _tokenService = tokenService;
        }

        // PUT api/account/profile
        [HttpPut("profile")]
        public async Task<ActionResult<UserDto>> UpdateProfile(ProfileUpdateRequest request)
        {
            User user = await _db.Users.FindAsync(GetUserId());

            user.FullName = request.FullName.Trim();
            user.Headline = request.Headline;
            user.Bio = request.Bio;
            await _db.SaveChangesAsync();

            return Ok(AuthController.ToUserDto(user));
        }

        // POST api/account/photo   (form-data: file)
        [HttpPost("photo")]
        public async Task<ActionResult<UserDto>> UploadPhoto(IFormFile file)
        {
            User user = await _db.Users.FindAsync(GetUserId());

            try
            {
                UploadedFile uploaded = await _fileStorage.UploadImageAsync(file);
                await _fileStorage.DeleteFileAsync(user.ProfileImagePublicId, false);

                user.ProfileImageUrl = uploaded.Url;
                user.ProfileImagePublicId = uploaded.PublicId;
                await _db.SaveChangesAsync();

                return Ok(AuthController.ToUserDto(user));
            }
            catch (Exception ex)
            {
                return ErrorMessage(400, ex.Message);
            }
        }

        // POST api/account/change-password
        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword(ChangePasswordRequest request)
        {
            User user = await _db.Users.FindAsync(GetUserId());

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return ErrorMessage(400, "Your current password is wrong.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            TokenService.RenewSecurityStamp(user); // log out every other device
            await _db.SaveChangesAsync();

            // This device gets a fresh token, so it stays logged in.
            return Ok(new { message = "Password changed. Other devices have been logged out.", token = _tokenService.CreateToken(user) });
        }

        // GET api/account/subscription  -> is my subscription active, and until when?
        [HttpGet("subscription")]
        public async Task<ActionResult<SubscriptionStatusDto>> GetSubscription()
        {
            int userId = GetUserId();
            DateTime now = DateTime.UtcNow;

            // All subscriptions that have not ended yet (the running one + renewals bought in advance).
            List<UserSubscription> subscriptions = await _db.UserSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.UserId == userId && s.EndDate > now)
                .OrderBy(s => s.StartDate)
                .ToListAsync();

            UserSubscription current = subscriptions.FirstOrDefault(s => s.StartDate <= now);
            if (current == null)
            {
                return Ok(new SubscriptionStatusDto { IsActive = false });
            }

            DateTime endDate = subscriptions.Max(s => s.EndDate);

            // Plans bought in advance, which start later.
            List<UpcomingPlanDto> upcoming = subscriptions
                .Where(s => s.StartDate > now)
                .Select(s => new UpcomingPlanDto
                {
                    PlanId = s.PlanId,
                    PlanName = s.Plan.Name,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate
                })
                .ToList();

            return Ok(new SubscriptionStatusDto
            {
                IsActive = true,
                PlanId = current.PlanId,
                PlanName = current.Plan.Name,
                CurrentPlanEndDate = current.EndDate,
                EndDate = endDate,
                DaysLeft = (int)Math.Ceiling((endDate - now).TotalDays),
                UpcomingPlans = upcoming
            });
        }

        // GET api/account/payments  -> purchase history
        [HttpGet("payments")]
        public async Task<ActionResult<List<PaymentDto>>> GetPayments()
        {
            int userId = GetUserId();

            List<PaymentDto> payments = await _db.Payments
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PaymentDto
                {
                    Id = p.Id,
                    PaymentType = p.PaymentType,
                    ItemName = p.PaymentType == "Course"
                        ? (p.Course != null ? p.Course.Title : "Deleted course")
                        : (p.Plan != null ? p.Plan.Name + " subscription" : "Subscription"),
                    OriginalAmount = p.OriginalAmount,
                    DiscountAmount = p.DiscountAmount,
                    Amount = p.Amount,
                    CouponCode = p.Coupon != null ? p.Coupon.Code : null,
                    Status = p.Status,
                    TransactionId = p.TransactionId,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return Ok(payments);
        }
    }
}
