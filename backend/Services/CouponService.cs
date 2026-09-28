using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Models;

namespace SmartLearning.Api.Services
{
    // The price of an item after applying a coupon (or without one).
    public class PriceResult
    {
        public decimal OriginalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }

        // The coupon that was applied, or null.
        public Coupon Coupon { get; set; }

        // Why the coupon could not be used, or null.
        public string CouponError { get; set; }
    }

    // Checks coupon codes and calculates the final price.
    public class CouponService
    {
        private readonly AppDbContext _db;

        public CouponService(AppDbContext db)
        {
            _db = db;
        }

        public static string NormalizeCode(string code)
        {
            return (code ?? "").Trim().ToUpper();
        }

        // courseId is set when buying a course, planId when buying a subscription.
        public async Task<PriceResult> CalculatePriceAsync(decimal price, string couponCode, int? courseId, int? planId)
        {
            PriceResult result = new PriceResult
            {
                OriginalAmount = price,
                DiscountAmount = 0,
                FinalAmount = price
            };

            string code = NormalizeCode(couponCode);
            if (code == "")
            {
                return result; // no coupon typed
            }

            Coupon coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == code);

            if (coupon == null || !coupon.IsActive)
            {
                result.CouponError = "This coupon code is not valid.";
                return result;
            }

            if (coupon.ExpiresAt != null && coupon.ExpiresAt < DateTime.UtcNow)
            {
                result.CouponError = "This coupon has expired.";
                return result;
            }

            if (coupon.MaxUses != null && coupon.UsedCount >= coupon.MaxUses)
            {
                result.CouponError = "This coupon has already been used the maximum number of times.";
                return result;
            }

            bool fits;
            if (planId != null)
            {
                fits = coupon.ForPlans;
            }
            else
            {
                // A course coupon for this course, or a site-wide course coupon.
                fits = coupon.CourseId == courseId || (coupon.CourseId == null && !coupon.ForPlans);
            }

            if (!fits)
            {
                result.CouponError = "This coupon cannot be used for this item.";
                return result;
            }

            decimal discount = Math.Round(price * coupon.DiscountPercent / 100m, 2);
            result.Coupon = coupon;
            result.DiscountAmount = discount;
            result.FinalAmount = price - discount;
            return result;
        }
    }
}
