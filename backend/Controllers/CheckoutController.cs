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
        private readonly NotificationService _notificationService;

        public CheckoutController(AppDbContext db, CourseAccessService accessService, NotificationService notificationService)
        {
            _db = db;
            _accessService = accessService;
            _notificationService = notificationService;
        }

        // POST api/checkout/course/5  -> buy one course (lifetime access)
        [HttpPost("course/{courseId}")]
        public async Task<ActionResult<CheckoutResponse>> BuyCourse(int courseId)
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

            Payment payment = CreateDemoPayment(userId, "Course", course.Price);
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
            }
            catch (DbUpdateException)
            {
                // Two "buy" requests arrived at the same moment. The other one created the
                // enrollment, and this one is cancelled completely (its payment is not saved).
                return ErrorMessage(400, "You already own this course.");
            }

            // Email: "You're enrolled in ..." with the receipt
            await _notificationService.SendEnrollmentEmailAsync(userId, courseId, AccessTypes.Purchased, payment.Amount, payment.TransactionId);

            return Ok(new CheckoutResponse
            {
                Message = "Payment successful! You now own \"" + course.Title + "\".",
                TransactionId = payment.TransactionId,
                Amount = payment.Amount
            });
        }

        // POST api/checkout/plan/2  -> buy (or renew) a subscription
        [HttpPost("plan/{planId}")]
        public async Task<ActionResult<CheckoutResponse>> BuyPlan(int planId)
        {
            int userId = GetUserId();

            SubscriptionPlan plan = await _db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == planId && p.IsActive);
            if (plan == null)
            {
                return ErrorMessage(404, "Plan not found.");
            }

            // A "serializable" transaction: if the same student buys twice at the same moment,
            // the database lets only one of the two purchases finish. Without it, both would
            // start from the same end date and the student would pay twice for one extension.
            using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

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

            Payment payment = CreateDemoPayment(userId, "Subscription", plan.Price);
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

            // Email: "Your subscription is active" with the receipt
            await _notificationService.SendSubscriptionEmailAsync(userId, plan.Name, subscription.EndDate, payment.Amount, payment.TransactionId);

            return Ok(new CheckoutResponse
            {
                Message = "Payment successful! Your subscription is valid until " + subscription.EndDate.ToString("dd MMM yyyy") + ".",
                TransactionId = payment.TransactionId,
                Amount = payment.Amount
            });
        }

        private static Payment CreateDemoPayment(int userId, string paymentType, decimal amount)
        {
            return new Payment
            {
                UserId = userId,
                PaymentType = paymentType,
                Amount = amount,
                Status = "Paid",
                TransactionId = "DEMO-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper(),
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
