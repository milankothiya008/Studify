namespace SmartLearning.Api.Models
{
    // One row is added every time a student buys a plan.
    // A subscription is active when StartDate <= now < EndDate.
    public class UserSubscription
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int PlanId { get; set; }
        public SubscriptionPlan Plan { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
