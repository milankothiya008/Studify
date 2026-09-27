namespace SmartLearning.Api.Models
{
    // A record of every purchase (a course or a subscription plan).
    // This project uses a DEMO payment, so no real money is taken.
    public class Payment
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        // "Course" or "Subscription"
        public string PaymentType { get; set; }

        // Only one of these is filled, depending on PaymentType.
        public int? CourseId { get; set; }
        public Course Course { get; set; }
        public int? PlanId { get; set; }
        public SubscriptionPlan Plan { get; set; }

        public decimal Amount { get; set; }

        // Always "Paid" in the demo. A real gateway would also use "Pending" and "Failed".
        public string Status { get; set; }

        // Fake transaction id for the demo, e.g. "DEMO-3F2A1B..."
        public string TransactionId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
