using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;
using SmartLearning.Api.Services;

namespace SmartLearning.Api.Controllers
{
    // Discount codes.
    //   Instructors: codes for their own courses only.
    //   Admins: codes for any course, for all courses (site-wide), or for subscription plans.
    [Authorize(Roles = Roles.Instructor + "," + Roles.Admin)]
    [Route("api/coupons")]
    public class CouponsController : BaseApiController
    {
        private readonly AppDbContext _db;

        public CouponsController(AppDbContext db)
        {
            _db = db;
        }

        private bool IsAdmin
        {
            get { return GetUserRole() == Roles.Admin; }
        }

        // GET api/coupons              -> my coupons (admins: all coupons)
        // GET api/coupons?courseId=5   -> coupons of one course
        [HttpGet]
        public async Task<ActionResult<List<CouponDto>>> GetCoupons(int? courseId)
        {
            int userId = GetUserId();
            IQueryable<Coupon> query = _db.Coupons;

            if (!IsAdmin)
            {
                // Instructors see the coupons of their own courses.
                query = query.Where(c => c.Course != null && c.Course.InstructorId == userId);
            }

            if (courseId != null)
            {
                query = query.Where(c => c.CourseId == courseId);
            }

            List<CouponDto> coupons = await query
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CouponDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    DiscountPercent = c.DiscountPercent,
                    CourseId = c.CourseId,
                    CourseTitle = c.Course != null ? c.Course.Title : null,
                    ForPlans = c.ForPlans,
                    ExpiresAt = c.ExpiresAt,
                    MaxUses = c.MaxUses,
                    UsedCount = c.UsedCount,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(coupons);
        }

        // POST api/coupons
        [HttpPost]
        public async Task<ActionResult> Create(CouponSaveRequest request)
        {
            string error = await CheckRequestAsync(request);
            if (error != null)
            {
                return ErrorMessage(400, error);
            }

            string code = CouponService.NormalizeCode(request.Code);
            bool taken = await _db.Coupons.AnyAsync(c => c.Code == code);
            if (taken)
            {
                return ErrorMessage(400, "The code " + code + " is already used. Choose another one.");
            }

            Coupon coupon = new Coupon
            {
                Code = code,
                CreatedById = GetUserId(),
                UsedCount = 0,
                CreatedAt = DateTime.UtcNow
            };
            CopyRequest(request, coupon);

            _db.Coupons.Add(coupon);
            await _db.SaveChangesAsync();

            return Ok(new { id = coupon.Id, message = "Coupon " + code + " created." });
        }

        // PUT api/coupons/7   (the code itself cannot be changed)
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, CouponSaveRequest request)
        {
            Coupon coupon = await FindMyCouponAsync(id);
            if (coupon == null)
            {
                return ErrorMessage(404, "Coupon not found.");
            }

            string error = await CheckRequestAsync(request);
            if (error != null)
            {
                return ErrorMessage(400, error);
            }

            CopyRequest(request, coupon);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Coupon saved." });
        }

        // DELETE api/coupons/7   (a coupon that was already used can only be turned off)
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            Coupon coupon = await FindMyCouponAsync(id);
            if (coupon == null)
            {
                return ErrorMessage(404, "Coupon not found.");
            }

            if (coupon.UsedCount > 0)
            {
                return ErrorMessage(400, "This coupon was already used, so it stays in the purchase history. Turn it off instead.");
            }

            _db.Coupons.Remove(coupon);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ---------- helpers ----------

        // Returns an error message, or null when the request is allowed.
        private async Task<string> CheckRequestAsync(CouponSaveRequest request)
        {
            if (request.ExpiresAt != null && request.ExpiresAt < DateTime.UtcNow)
            {
                return "The expiry date must be in the future.";
            }

            if (request.ForPlans && request.CourseId != null)
            {
                return "A coupon is either for a course or for subscription plans, not both.";
            }

            if (!IsAdmin)
            {
                // Instructors: always for one of their own courses.
                if (request.CourseId == null || request.ForPlans)
                {
                    return "Choose one of your courses for this coupon.";
                }
            }

            if (request.CourseId != null)
            {
                Course course = await _db.Courses.FindAsync(request.CourseId);
                if (course == null)
                {
                    return "Course not found.";
                }
                if (!IsAdmin && course.InstructorId != GetUserId())
                {
                    return "You can only create coupons for your own courses.";
                }
            }

            return null;
        }

        private async Task<Coupon> FindMyCouponAsync(int id)
        {
            Coupon coupon = await _db.Coupons.Include(c => c.Course).FirstOrDefaultAsync(c => c.Id == id);
            if (coupon == null)
            {
                return null;
            }
            if (!IsAdmin && (coupon.Course == null || coupon.Course.InstructorId != GetUserId()))
            {
                return null;
            }
            return coupon;
        }

        private static void CopyRequest(CouponSaveRequest request, Coupon coupon)
        {
            coupon.DiscountPercent = request.DiscountPercent;
            coupon.CourseId = request.CourseId;
            coupon.ForPlans = request.ForPlans;
            // Dates from the browser are turned into UTC, like every date in the database.
            coupon.ExpiresAt = request.ExpiresAt != null ? request.ExpiresAt.Value.ToUniversalTime() : null;
            coupon.MaxUses = request.MaxUses;
            coupon.IsActive = request.IsActive;
        }
    }
}
