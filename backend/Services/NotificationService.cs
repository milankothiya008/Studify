using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Models;

namespace SmartLearning.Api.Services
{
    // Tells people that something happened:
    //   - in-app notifications (the bell icon), and
    //   - emails (enrollment, subscription, answers).
    // A notification problem never cancels an enrollment or a payment: errors are only logged.
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

        // Adds one notification under the bell icon of a user.
        public async Task NotifyAsync(int userId, string title, string message, string link)
        {
            try
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Link = link,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not save a notification for user {UserId}", userId);
            }
        }

        // Someone joined a course (free, bought, or with a subscription).
        // amount and transactionId are only given when the course was bought.
        public async Task OnEnrolledAsync(int userId, int courseId, string accessType, decimal? amount, string transactionId)
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

                // Student: bell + email
                await NotifyAsync(userId, "You're enrolled!", "You can now start \"" + course.Title + "\".", "/learn/" + course.Id);

                string courseUrl = _emailService.FrontendUrl + "/learn/" + course.Id;
                EmailMessage message = EmailTemplates.EnrollmentConfirmation(
                    user.FullName, course.Title, course.Instructor.FullName, accessText, amount, transactionId, courseUrl);
                await _emailService.SendAsync(user.Email, user.FullName, message);

                // Instructor: bell only
                string sale = amount != null ? " (sale: ₹" + amount.Value.ToString("N0") + ")" : "";
                await NotifyAsync(course.InstructorId, "New student",
                    user.FullName + " enrolled in \"" + course.Title + "\"" + sale + ".", "/instructor");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not send the enrollment notifications for course {CourseId}", courseId);
            }
        }

        public async Task OnSubscribedAsync(int userId, string planName, DateTime endDate, decimal amount, string transactionId)
        {
            try
            {
                User user = await _db.Users.FindAsync(userId);
                if (user == null)
                {
                    return;
                }

                await NotifyAsync(userId, "Subscription active",
                    "Your " + planName + " plan is valid until " + endDate.ToString("dd MMM yyyy") + ".", "/courses");

                EmailMessage message = EmailTemplates.SubscriptionConfirmation(
                    user.FullName, planName, endDate, amount, transactionId, _emailService.FrontendUrl + "/courses");
                await _emailService.SendAsync(user.Email, user.FullName, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not send the subscription notifications to user {UserId}", userId);
            }
        }

        // Somebody answered a question: tell the person who asked (bell + email).
        public async Task OnAnsweredAsync(Question question, User answeredBy, string answerText)
        {
            try
            {
                if (question.UserId == answeredBy.Id)
                {
                    return; // no need to notify people about their own answers
                }

                User asker = await _db.Users.FindAsync(question.UserId);
                Course course = await _db.Courses.FindAsync(question.CourseId);
                if (asker == null || course == null)
                {
                    return;
                }

                bool isInstructor = course.InstructorId == answeredBy.Id;
                string who = isInstructor ? "The instructor" : answeredBy.FullName;
                string link = "/learn/" + course.Id + "?question=" + question.Id;

                await NotifyAsync(asker.Id, "New answer to your question",
                    who + " answered \"" + question.Title + "\".", link);

                EmailMessage message = EmailTemplates.QuestionAnswered(
                    asker.FullName, who, question.Title, answerText, course.Title, _emailService.FrontendUrl + link);
                await _emailService.SendAsync(asker.Email, asker.FullName, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not send the answer notification for question {QuestionId}", question.Id);
            }
        }
    }
}
