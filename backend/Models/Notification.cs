namespace SmartLearning.Api.Models
{
    // A message shown under the bell icon, e.g. "Grace answered your question".
    public class Notification
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public string Title { get; set; }
        public string Message { get; set; }

        // Page to open when the notification is clicked, e.g. "/learn/5". Can be empty.
        public string Link { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
