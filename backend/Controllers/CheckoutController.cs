using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;
using SmartLearning.Api.Services;

namespace SmartLearning.Api.Controllers
{
    // DEMO checkout: every payment succeeds immediately and no money is taken.
    // Coupon codes are supported (see CouponService).
    //
    // To use a real payment gateway (Razorpay, Stripe ...) later:
    //   1. Create an order with the gateway and return it to the React app.
    //   2. The React app opens the gateway's payment popup.
    //   3. The gateway calls you back; verify the payment signature on the server.
    //   4. Only then run the code below that gives access (enrollment / subscription).
    [Authorize]
    [Route("api/checkout")]
    public class CheckoutController : BaseApiController
    {
        private readonly AppDbContext _db;
        private readonly CourseAccessService _accessService;
        private readonly CouponService _couponService;
        private readonly NotificationService _notificationService;

        public CheckoutController(AppDbContext db, CourseAccessService accessService,
            CouponService couponService, NotificationService notificationService)
        {
            _db = db;
            _accessService = accessService;
            _couponService = couponService;
            _notificationService = notificationService;
        }

        // POST api/checkout/preview   body: { "itemType": "course", "itemId": 5, "couponCode": "WELCOME20" }
        // Shows the price with the coupon, before paying.
        [HttpPost("preview")]
        public async Task<ActionResult<CheckoutPreviewDto>> Preview(CheckoutPreviewRequest request)
        {
            decimal price;
            int? courseId = null;
            int? planId = null;

            if (request.ItemType == "course")
            {
                Course course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == request.ItemId && c.IsPublished);
                if (course == null)
                {
                    return ErrorMessage(404, "Course not found.");
                }
                price = course.Price;
                courseId = course.Id;
            }
            else
            {
                SubscriptionPlan plan = await _db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == request.ItemId && p.IsActive);
                if (plan == null)
                {
                    return ErrorMessage(404, "Plan not found.");
                }
                price = plan.Price;
                planId = plan.Id;
            }

            PriceResult result = await _couponService.CalculatePriceAsync(price, request.CouponCode, courseId, planId);
            return Ok(ToPreview(result));
        }

        // POST api/checkout/course/5   body: { "couponCode": "WELCOME20" }  (the code is optional)
        // Buys one course (lifetime access).
        [HttpPost("course/{courseId}")]
        public async Task<ActionResult<CheckoutResponse>> BuyCourse(int courseId, CheckoutRequest request)
        {
            int userId = GetUserId();

            Course course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId && c.IsPublished);
            if (course == null)
            {
                return ErrorMessage(404, "Course not found.");
            }

            if (course.InstructorId == userId)
            {
                return ErrorMessage(400, "You cannot buy your own course.");
            }

            if (course.Price == 0)
            {
                return ErrorMessage(400, "This course is free. Just enroll.");
            }

            Enrollment enrollment = await _db.Enrollments.FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);
            if (enrollment != null && enrollment.AccessType == AccessTypes.Purchased)
            {
                return ErrorMessage(400, "You already own this course.");
            }

            PriceResult price = await _couponService.CalculatePriceAsync(course.Price, request.CouponCode, course.Id, null);
            if (price.CouponError != null)
            {
                return ErrorMessage(400, price.CouponError);
            }

            // The coupon use, the payment and the enrollment are saved together, or not at all.
            using var transaction = await _db.Database.BeginTransactionAsync();

            if (!await ClaimCouponUseAsync(price))
            {
                return ErrorMessage(400, "This coupon has just reached its maximum number of uses.");
            }

            Payment payment = CreateDemoPayment(userId, "Course", price);
            payment.CourseId = course.Id;
            _db.Payments.Add(payment);

            if (enrollment == null)
            {
                _db.Enrollments.Add(new Enrollment
                {
                    UserId = userId,
                    CourseId = courseId,
                    AccessType = AccessTypes.Purchased,
                    EnrolledAt = DateTime.UtcNow
                });
            }
            else
            {
                // The student was enrolled through a subscription. Now they own it forever,
                // and their progress is kept.
                enrollment.AccessType = AccessTypes.Purchased;
            }

            try
            {
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException)
            {
                // Two "buy" requests arrived at the same moment. The other one created the
                // enrollment, and this one is cancelled completely (no payment, no coupon use).
                return ErrorMessage(400, "You already own this course.");
            }

            // Email + bell: "You're enrolled in ..." (and "New student" for the instructor)
            await _notificationService.OnEnrolledAsync(userId, courseId, AccessTypes.Purchased, payment.Amount, payment.TransactionId);

            return Ok(CreateResponse(payment, "Payment successful! You now own \"" + course.Title + "\"."));
        }

        // POST api/checkout/plan/2   body: { "couponCode": "..." }  -> buy (or renew) a subscription
        [HttpPost("plan/{planId}")]
        public async Task<ActionResult<CheckoutResponse>> BuyPlan(int planId, CheckoutRequest request)
        {
            int userId = GetUserId();

            SubscriptionPlan plan = await _db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == planId && p.IsActive);
            if (plan == null)
            {
                return ErrorMessage(404, "Plan not found.");
            }

            PriceResult price = await _couponService.CalculatePriceAsync(plan.Price, request.CouponCode, null, plan.Id);
            if (price.CouponError != null)
            {
                return ErrorMessage(400, price.CouponError);
            }

            // A "serializable" transaction: if the same student buys twice at the same moment,
            // the database lets only one of the two purchases finish. Without it, both would
            // start from the same end date and the student would pay twice for one extension.
            using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            if (!await ClaimCouponUseAsync(price))
            {
                return ErrorMessage(400, "This coupon has just reached its maximum number of uses.");
            }

            // If a subscription is still running, the new one starts when it ends
            // (so the student never loses days they already paid for).
            DateTime startDate = DateTime.UtcNow;
            DateTime? currentEndDate = await _accessService.GetSubscriptionEndDateAsync(userId);
            if (currentEndDate != null)
            {
                startDate = currentEndDate.Value;
            }

            UserSubscription subscription = new UserSubscription
            {
                UserId = userId,
                PlanId = plan.Id,
                StartDate = startDate,
                EndDate = startDate.AddDays(plan.DurationDays)
            };
            _db.UserSubscriptions.Add(subscription);

            Payment payment = CreateDemoPayment(userId, "Subscription", price);
            payment.PlanId = plan.Id;
            _db.Payments.Add(payment);

            try
            {
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                return ErrorMessage(409, "Another payment was being processed at the same time. "
                    + "Please check your purchase history before trying again.");
            }

            // Email + bell: "Your subscription is active"
            await _notificationService.OnSubscribedAsync(userId, plan.Name, subscription.EndDate, payment.Amount, payment.TransactionId);

            return Ok(CreateResponse(payment,
                "Payment successful! Your subscription is valid until " + subscription.EndDate.ToString("dd MMM yyyy") + "."));
        }

        // ---------- helpers ----------

        // Counts one use of the coupon, directly in the database, but only while it is still
        // allowed. Two students using the last available use at the same moment: only one wins.
        // Returns false when the coupon can no longer be used. (true when there is no coupon)
        private async Task<bool> ClaimCouponUseAsync(PriceResult price)
        {
            if (price.Coupon == null)
            {
                return true;
            }

            int couponId = price.Coupon.Id;
            int updatedRows = await _db.Coupons
                .Where(c => c.Id == couponId && c.IsActive && (c.MaxUses == null || c.UsedCount < c.MaxUses))
                .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.UsedCount, c => c.UsedCount + 1));

            return updatedRows == 1;
        }

        private Payment CreateDemoPayment(int userId, string paymentType, PriceResult price)
        {
            return new Payment
            {
                UserId = userId,
                PaymentType = paymentType,
                OriginalAmount = price.OriginalAmount,
                DiscountAmount = price.DiscountAmount,
                Amount = price.FinalAmount,
                CouponId = price.Coupon != null ? price.Coupon.Id : null,
                Status = "Paid",
                TransactionId = "DEMO-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper(),
                CreatedAt = DateTime.UtcNow
            };
        }

        private static CheckoutResponse CreateResponse(Payment payment, string message)
        {
            return new CheckoutResponse
            {
                Message = message,
                TransactionId = payment.TransactionId,
                OriginalAmount = payment.OriginalAmount,
                DiscountAmount = payment.DiscountAmount,
                Amount = payment.Amount
            };
        }

        private static CheckoutPreviewDto ToPreview(PriceResult result)
        {
            return new CheckoutPreviewDto
            {
                OriginalAmount = result.OriginalAmount,
                DiscountAmount = result.DiscountAmount,
                FinalAmount = result.FinalAmount,
                CouponApplied = result.Coupon != null,
                CouponCode = result.Coupon != null ? result.Coupon.Code : null,
                DiscountPercent = result.Coupon != null ? result.Coupon.DiscountPercent : 0,
                CouponError = result.CouponError
            };
        }
    }
}
