using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Models;

namespace SmartLearning.Api.Services
{
    // Sends the "you are enrolled" and "subscription confirmed" emails.
    // An email problem never cancels an enrollment or a payment: errors are only logged.
    public class NotificationService
    {
        private readonly AppDbContext _db;
        private readonly EmailService _emailService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(AppDbContext db, EmailService emailService, ILogger<NotificationService> logger)
        {
            _db = db;
            _emailService = emailService;
            _logger = logger;
        }

        // amount and transactionId are only given when the course was bought.
        public async Task SendEnrollmentEmailAsync(int userId, int courseId, string accessType, decimal? amount, string transactionId)
        {
            try
            {
                User user = await _db.Users.FindAsync(userId);
                Course course = await _db.Courses
                    .Include(c => c.Instructor)
                    .FirstOrDefaultAsync(c => c.Id == courseId);

                if (user == null || course == null)
                {
                    return;
                }

                string accessText = "Lifetime access";
                if (accessType == AccessTypes.Subscription)
                {
                    accessText = "Included in your subscription (available while it is active)";
                }
                else if (accessType == AccessTypes.Free)
                {
                    accessText = "Free course, lifetime access";
                }

                string courseUrl = _emailService.FrontendUrl + "/learn/" + course.Id;

                EmailMessage message = EmailTemplates.EnrollmentConfirmation(
                    user.FullName, course.Title, course.Instructor.FullName, accessText, amount, transactionId, courseUrl);

                await _emailService.SendAsync(user.Email, user.FullName, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not send the enrollment email for course {CourseId}", courseId);
            }
        }

        public async Task SendSubscriptionEmailAsync(int userId, string planName, DateTime endDate, decimal amount, string transactionId)
        {
            try
            {
                User user = await _db.Users.FindAsync(userId);
                if (user == null)
                {
                    return;
                }

                EmailMessage message = EmailTemplates.SubscriptionConfirmation(
                    user.FullName, planName, endDate, amount, transactionId, _emailService.FrontendUrl + "/courses");

                await _emailService.SendAsync(user.Email, user.FullName, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not send the subscription email to user {UserId}", userId);
            }
        }
    }
}
