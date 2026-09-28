using System.Net.Http.Json;

namespace SmartLearning.Api.Services
{
    // One email: a subject, an HTML version and a plain text version.
    public class EmailMessage
    {
        public string Subject { get; set; }
        public string Html { get; set; }
        public string Text { get; set; }
    }

    // Sends emails with Brevo (https://www.brevo.com), free for 300 emails per day.
    //
    // Why Brevo and not Gmail SMTP? Railway blocks SMTP (email ports) on its Trial and
    // Hobby plans. Brevo is called over normal HTTPS, so it works everywhere.
    //
    // If Brevo is NOT configured, emails are not sent. Instead they are written to the
    // backend log (console), so you can still see the codes while testing on your computer.
    public class EmailService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(HttpClient httpClient, IConfiguration configuration, ILogger<EmailService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public bool IsConfigured
        {
            get
            {
                string apiKey = _configuration["Email:BrevoApiKey"];
                string senderEmail = _configuration["Email:SenderEmail"];
                return !string.IsNullOrWhiteSpace(apiKey) && !apiKey.StartsWith("YOUR_")
                    && !string.IsNullOrWhiteSpace(senderEmail) && !senderEmail.StartsWith("YOUR_");
            }
        }

        // Address of the React app, used for links inside emails.
        // FrontendUrl may contain several addresses separated by commas: the first one is used.
        public string FrontendUrl
        {
            get
            {
                string firstUrl = _configuration["FrontendUrl"].Split(',')[0];
                return firstUrl.Trim().TrimEnd('/');
            }
        }

        // Returns true when the email was sent (or logged, when Brevo is not configured).
        public async Task<bool> SendAsync(string toEmail, string toName, EmailMessage message)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning(
                    "EMAIL NOT SENT (Brevo is not configured). To: {To} | Subject: {Subject}\n{Text}",
                    toEmail, message.Subject, message.Text);
                return true;
            }

            // The JSON body that Brevo expects.
            var body = new
            {
                sender = new
                {
                    name = _configuration["Email:SenderName"],
                    email = _configuration["Email:SenderEmail"]
                },
                to = new[] { new { email = toEmail, name = toName } },
                subject = message.Subject,
                htmlContent = message.Html,
                textContent = message.Text
            };

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", _configuration["Email:BrevoApiKey"]);
            request.Content = JsonContent.Create(body);

            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                string error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Brevo could not send the email to {To}: {Status} {Error}", toEmail, (int)response.StatusCode, error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not reach Brevo to send the email to {To}", toEmail);
                return false;
            }
        }
    }
}
