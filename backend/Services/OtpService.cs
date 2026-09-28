using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Models;

namespace SmartLearning.Api.Services
{
    // Creates, emails and checks 6-digit one-time codes (OTP).
    // Used for "verify your email" at sign up and for "forgot password".
    public class OtpService
    {
        private const int CodeValidMinutes = 10;
        private const int MaxWrongAttempts = 5;
        private const int SecondsBetweenCodes = 60;

        private readonly AppDbContext _db;
        private readonly EmailService _emailService;

        public OtpService(AppDbContext db, EmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        // Creates a new code and emails it to the user.
        // Returns null when it worked, or an error message to show to the user.
        public async Task<string> SendCodeAsync(User user, string purpose)
        {
            DateTime now = DateTime.UtcNow;

            List<EmailCode> oldCodes = await _db.EmailCodes
                .Where(c => c.Email == user.Email && c.Purpose == purpose)
                .ToListAsync();

            // Do not allow a new email every second.
            EmailCode newest = oldCodes.OrderByDescending(c => c.CreatedAt).FirstOrDefault();
            if (newest != null)
            {
                int secondsSince = (int)(now - newest.CreatedAt).TotalSeconds;
                if (secondsSince < SecondsBetweenCodes)
                {
                    int wait = SecondsBetweenCodes - secondsSince;
                    return "Please wait " + wait + " seconds before asking for a new code.";
                }
            }

            // Only the newest code is valid, so the old ones can go.
            _db.EmailCodes.RemoveRange(oldCodes);

            // A random number from 000000 to 999999.
            string code = RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");

            EmailCode emailCode = new EmailCode
            {
                Email = user.Email,
                Purpose = purpose,
                CodeHash = BCrypt.Net.BCrypt.HashPassword(code),
                ExpiresAt = now.AddMinutes(CodeValidMinutes),
                CreatedAt = now
            };
            _db.EmailCodes.Add(emailCode);
            await _db.SaveChangesAsync();

            EmailMessage message;
            if (purpose == CodePurposes.ResetPassword)
            {
                message = EmailTemplates.PasswordResetCode(user.FullName, code);
            }
            else
            {
                message = EmailTemplates.VerificationCode(user.FullName, code);
            }

            bool sent = await _emailService.SendAsync(user.Email, user.FullName, message);
            if (!sent)
            {
                // Remove the code again, so the user can retry immediately.
                _db.EmailCodes.Remove(emailCode);
                await _db.SaveChangesAsync();
                return "We could not send the email right now. Please try again in a minute.";
            }

            return null;
        }

        // Checks a code the user typed.
        // Returns null when the code is right, or an error message to show to the user.
        public async Task<string> CheckCodeAsync(string email, string purpose, string code)
        {
            EmailCode saved = await _db.EmailCodes
                .Where(c => c.Email == email && c.Purpose == purpose)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            if (saved == null || saved.IsUsed)
            {
                return "No active code found. Please ask for a new code.";
            }

            if (saved.ExpiresAt < DateTime.UtcNow)
            {
                return "This code has expired. Please ask for a new code.";
            }

            if (saved.Attempts >= MaxWrongAttempts)
            {
                return "Too many wrong tries. Please ask for a new code.";
            }

            if (string.IsNullOrWhiteSpace(code) || !BCrypt.Net.BCrypt.Verify(code.Trim(), saved.CodeHash))
            {
                saved.Attempts++;
                await _db.SaveChangesAsync();

                int triesLeft = MaxWrongAttempts - saved.Attempts;
                if (triesLeft == 0)
                {
                    return "Wrong code. Please ask for a new code.";
                }
                return "Wrong code. You have " + triesLeft + " tries left.";
            }

            saved.IsUsed = true;
            await _db.SaveChangesAsync();
            return null;
        }
    }
}
