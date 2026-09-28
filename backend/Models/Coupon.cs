namespace SmartLearning.Api.Models
{
    // A discount code, e.g. "WELCOME20" = 20% off.
    //
    // - CourseId filled  -> works only for that course (instructors create these).
    // - CourseId empty and ForPlans = false -> works for every course (admins only).
    // - ForPlans = true  -> works for subscription plans (admins only).
    public class Coupon
    {
        public int Id { get; set; }

        // Always stored in UPPER CASE.
        public string Code { get; set; }

        // 1 - 100
        public int DiscountPercent { get; set; }

        public int? CourseId { get; set; }
        public Course Course { get; set; }

        public bool ForPlans { get; set; }

        // Who created it (an instructor or an admin).
        public int CreatedById { get; set; }
        public User CreatedBy { get; set; }

        // Optional limits. Null = no limit.
        public DateTime? ExpiresAt { get; set; }
        public int? MaxUses { get; set; }

        // How many paid orders used this code.
        public int UsedCount { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
