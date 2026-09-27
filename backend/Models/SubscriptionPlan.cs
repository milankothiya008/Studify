namespace SmartLearning.Api.Models
{
    // A plan that unlocks every course, for example "Monthly - 30 days".
    public class SubscriptionPlan
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }

        // How many days the subscription stays valid after buying it.
        public int DurationDays { get; set; }

        // Inactive plans are hidden from the pricing page.
        public bool IsActive { get; set; }
    }
}
