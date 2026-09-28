using System.Net;

namespace SmartLearning.Api.Services
{
    // The text of every email the platform sends.
    // Names and titles are HTML-encoded, so a course title like "<b>Hi</b>" cannot break the email.
    public static class EmailTemplates
    {
        public static EmailMessage VerificationCode(string name, string code)
        {
            return new EmailMessage
            {
                Subject = code + " is your SmartLearn verification code",
                Html = Layout("Verify your email",
                    "<p>Hi " + Encode(name) + ",</p>"
                    + "<p>Welcome to SmartLearn! Enter this code to verify your email address:</p>"
                    + CodeBox(code)
                    + "<p style=\"color:#64748b\">The code is valid for 10 minutes. If you did not sign up, you can ignore this email.</p>"),
                Text = "Hi " + name + ",\n\nYour SmartLearn verification code is: " + code
                    + "\nIt is valid for 10 minutes."
            };
        }

        public static EmailMessage PasswordResetCode(string name, string code)
        {
            return new EmailMessage
            {
                Subject = code + " is your SmartLearn password reset code",
                Html = Layout("Reset your password",
                    "<p>Hi " + Encode(name) + ",</p>"
                    + "<p>We received a request to reset your password. Enter this code to choose a new one:</p>"
                    + CodeBox(code)
                    + "<p style=\"color:#64748b\">The code is valid for 10 minutes. If you did not ask for this, you can ignore this email: your password stays the same.</p>"),
                Text = "Hi " + name + ",\n\nYour SmartLearn password reset code is: " + code
                    + "\nIt is valid for 10 minutes."
            };
        }

        // Sent after enrolling in a course (free, bought, or with a subscription).
        // amount and transactionId are only given when the course was bought.
        public static EmailMessage EnrollmentConfirmation(string name, string courseTitle, string instructorName,
            string accessText, decimal? amount, string transactionId, string courseUrl)
        {
            string receipt = "";
            string receiptText = "";
            if (amount != null)
            {
                receipt = "<table style=\"width:100%;border-collapse:collapse;margin:16px 0;font-size:14px\">"
                    + Row("Amount paid", "₹" + amount.Value.ToString("N2"))
                    + Row("Transaction", Encode(transactionId))
                    + Row("Date", DateTime.UtcNow.ToString("dd MMM yyyy"))
                    + "</table>";
                receiptText = "\nAmount paid: ₹" + amount.Value.ToString("N2") + "\nTransaction: " + transactionId;
            }

            return new EmailMessage
            {
                Subject = "You're enrolled in " + courseTitle,
                Html = Layout("You're enrolled! 🎉",
                    "<p>Hi " + Encode(name) + ",</p>"
                    + "<p>You have been enrolled in <strong>" + Encode(courseTitle) + "</strong> by "
                    + Encode(instructorName) + ".</p>"
                    + "<p style=\"color:#64748b\">Access: " + Encode(accessText) + "</p>"
                    + receipt
                    + Button("Start learning", courseUrl)),
                Text = "Hi " + name + ",\n\nYou have been enrolled in " + courseTitle + " by " + instructorName + "."
                    + "\nAccess: " + accessText + receiptText + "\n\nStart learning: " + courseUrl
            };
        }

        public static EmailMessage SubscriptionConfirmation(string name, string planName, DateTime endDate,
            decimal amount, string transactionId, string coursesUrl)
        {
            return new EmailMessage
            {
                Subject = "Your SmartLearn " + planName + " subscription is active",
                Html = Layout("Subscription confirmed",
                    "<p>Hi " + Encode(name) + ",</p>"
                    + "<p>Thank you! Your <strong>" + Encode(planName) + "</strong> subscription is active. "
                    + "You can now enroll in every course on SmartLearn.</p>"
                    + "<table style=\"width:100%;border-collapse:collapse;margin:16px 0;font-size:14px\">"
                    + Row("Valid until", endDate.ToString("dd MMM yyyy"))
                    + Row("Amount paid", "₹" + amount.ToString("N2"))
                    + Row("Transaction", Encode(transactionId))
                    + "</table>"
                    + Button("Browse courses", coursesUrl)),
                Text = "Hi " + name + ",\n\nYour " + planName + " subscription is active until "
                    + endDate.ToString("dd MMM yyyy") + ".\nAmount paid: ₹" + amount.ToString("N2")
                    + "\nTransaction: " + transactionId + "\n\nBrowse courses: " + coursesUrl
            };
        }

        public static EmailMessage QuestionAnswered(string name, string answeredBy, string questionTitle,
            string answerText, string courseTitle, string link)
        {
            string shortAnswer = answerText.Length > 400 ? answerText.Substring(0, 400) + "..." : answerText;

            return new EmailMessage
            {
                Subject = "New answer: " + questionTitle,
                Html = Layout("Your question was answered",
                    "<p>Hi " + Encode(name) + ",</p>"
                    + "<p><strong>" + Encode(answeredBy) + "</strong> answered your question in <strong>"
                    + Encode(courseTitle) + "</strong>:</p>"
                    + "<p style=\"font-weight:700;margin-bottom:4px\">" + Encode(questionTitle) + "</p>"
                    + "<div style=\"background:#f5f3ff;border-left:4px solid #7c3aed;padding:12px 16px;border-radius:8px;white-space:pre-line\">"
                    + Encode(shortAnswer) + "</div>"
                    + Button("View the discussion", link)),
                Text = "Hi " + name + ",\n\n" + answeredBy + " answered your question \"" + questionTitle + "\" in "
                    + courseTitle + ":\n\n" + shortAnswer + "\n\nView the discussion: " + link
            };
        }

        // ---------- building blocks ----------

        private static string Encode(string text)
        {
            return WebUtility.HtmlEncode(text ?? "");
        }

        // The frame around every email: purple header, white card, small footer.
        private static string Layout(string title, string content)
        {
            return "<div style=\"background:#f4f3fb;padding:32px 12px;font-family:Segoe UI,Arial,sans-serif;color:#1e1b2e\">"
                + "<div style=\"max-width:520px;margin:0 auto;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(76,29,149,.08)\">"
                + "<div style=\"background:linear-gradient(135deg,#4c1d95,#7c3aed);padding:24px 32px;color:#ffffff\">"
                + "<div style=\"font-size:20px;font-weight:800\">Smart<span style=\"color:#ddd6fe\">Learn</span></div>"
                + "<div style=\"font-size:22px;font-weight:700;margin-top:12px\">" + Encode(title) + "</div>"
                + "</div>"
                + "<div style=\"padding:24px 32px;font-size:15px;line-height:1.6\">" + content + "</div>"
                + "</div>"
                + "<p style=\"text-align:center;color:#94a3b8;font-size:12px;margin-top:16px\">SmartLearn · Learn anything, anytime</p>"
                + "</div>";
        }

        private static string CodeBox(string code)
        {
            return "<div style=\"font-size:34px;font-weight:800;letter-spacing:10px;text-align:center;"
                + "background:#f5f3ff;color:#5b21b6;border-radius:12px;padding:16px;margin:20px 0\">" + code + "</div>";
        }

        private static string Button(string text, string url)
        {
            return "<p style=\"text-align:center;margin:24px 0 8px\"><a href=\"" + Encode(url) + "\" "
                + "style=\"background:#7c3aed;color:#ffffff;text-decoration:none;font-weight:700;padding:12px 28px;border-radius:10px;display:inline-block\">"
                + Encode(text) + "</a></p>";
        }

        private static string Row(string label, string value)
        {
            return "<tr><td style=\"padding:8px 0;color:#64748b;border-bottom:1px solid #eee\">" + label + "</td>"
                + "<td style=\"padding:8px 0;text-align:right;font-weight:600;border-bottom:1px solid #eee\">" + value + "</td></tr>";
        }
    }
}
