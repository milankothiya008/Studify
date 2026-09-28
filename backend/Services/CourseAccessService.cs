using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Models;

namespace SmartLearning.Api.Services
{
    // All the rules about "who can watch which course" live in this one place.
    public class CourseAccessService
    {
        private readonly AppDbContext _db;

        public CourseAccessService(AppDbContext db)
        {
            _db = db;
        }

        // True when the user has a subscription running right now.
        public async Task<bool> HasActiveSubscriptionAsync(int userId)
        {
            DateTime now = DateTime.UtcNow;
            return await _db.UserSubscriptions
                .AnyAsync(s => s.UserId == userId && s.StartDate <= now && s.EndDate > now);
        }

        // The last day of the user's subscription (including renewals bought in advance),
        // or null when the user has no running subscription.
        public async Task<DateTime?> GetSubscriptionEndDateAsync(int userId)
        {
            DateTime now = DateTime.UtcNow;
            List<UserSubscription> subscriptions = await _db.UserSubscriptions
                .Where(s => s.UserId == userId && s.EndDate > now)
                .ToListAsync();

            if (subscriptions.Count == 0)
            {
                return null;
            }

            return subscriptions.Max(s => s.EndDate);
        }

        // Rules:
        // 1. Admins can watch everything.
        // 2. The instructor can watch their own course.
        // 3. A student needs an enrollment:
        //    - Free / Purchased enrollments work forever.
        //    - Subscription enrollments work only while the subscription is active.
        public async Task<bool> CanWatchCourseAsync(int userId, string role, Course course)
        {
            if (userId == 0)
            {
                return false;
            }

            if (role == Roles.Admin || course.InstructorId == userId)
            {
                return true;
            }

            Enrollment enrollment = await _db.Enrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == course.Id);

            if (enrollment == null)
            {
                return false;
            }

            // A subscription enrollment needs an active subscription,
            // unless the instructor has made the course free in the meantime.
            if (enrollment.AccessType == AccessTypes.Subscription && course.Price > 0)
            {
                return await HasActiveSubscriptionAsync(userId);
            }

            return true;
        }
    }
}
