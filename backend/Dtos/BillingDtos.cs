using System.ComponentModel.DataAnnotations;

namespace SmartLearning.Api.Dtos
{
    public class PlanDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public bool IsActive { get; set; }
    }

    public class PlanSaveRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Range(1, 1000000)]
        public decimal Price { get; set; }

        [Range(1, 3650)]
        public int DurationDays { get; set; }

        public bool IsActive { get; set; }
    }

    public class SubscriptionStatusDto
    {
        public bool IsActive { get; set; }

        // The plan that is running right now.
        public int? PlanId { get; set; }
        public string PlanName { get; set; }
        public DateTime? CurrentPlanEndDate { get; set; }

        // The last day of access, including plans bought in advance (see UpcomingPlans).
        public DateTime? EndDate { get; set; }
        public int DaysLeft { get; set; }

        // Plans already paid for that start when the current one ends.
        public List<UpcomingPlanDto> UpcomingPlans { get; set; } = new List<UpcomingPlanDto>();
    }

    public class UpcomingPlanDto
    {
        public int PlanId { get; set; }
        public string PlanName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class PaymentDto
    {
        public int Id { get; set; }
        public string PaymentType { get; set; }

        // Course title or plan name.
        public string ItemName { get; set; }
        public decimal OriginalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Amount { get; set; }
        public string CouponCode { get; set; }
        public string Status { get; set; }
        public string TransactionId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CheckoutResponse
    {
        public string Message { get; set; }
        public string TransactionId { get; set; }
        public decimal OriginalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Amount { get; set; }
    }

    // Body of POST api/checkout/course/5 and api/checkout/plan/2
    public class CheckoutRequest
    {
        // Optional discount code.
        [StringLength(40)]
        public string CouponCode { get; set; }
    }

    public class CheckoutPreviewRequest
    {
        // "course" or "plan"
        [Required]
        [RegularExpression("^(course|plan)$")]
        public string ItemType { get; set; }

        public int ItemId { get; set; }

        [StringLength(40)]
        public string CouponCode { get; set; }
    }

    // The price shown on the checkout page.
    public class CheckoutPreviewDto
    {
        public decimal OriginalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public bool CouponApplied { get; set; }
        public string CouponCode { get; set; }
        public int DiscountPercent { get; set; }
        public string CouponError { get; set; }
    }

    public class CouponDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public int DiscountPercent { get; set; }
        public int? CourseId { get; set; }
        public string CourseTitle { get; set; }
        public bool ForPlans { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? MaxUses { get; set; }
        public int UsedCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CouponSaveRequest
    {
        [Required]
        [RegularExpression("^[A-Za-z0-9_-]{3,30}$", ErrorMessage = "Use 3-30 letters, numbers, - or _ (no spaces).")]
        public string Code { get; set; }

        [Range(1, 100)]
        public int DiscountPercent { get; set; }

        // For instructors: the course the coupon is for (required).
        // For admins: a course, or empty for all courses / plans.
        public int? CourseId { get; set; }

        public bool ForPlans { get; set; }

        public DateTime? ExpiresAt { get; set; }

        [Range(1, 1000000)]
        public int? MaxUses { get; set; }

        public bool IsActive { get; set; }
    }

    public class AdminStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalInstructors { get; set; }
        public int TotalCourses { get; set; }
        public int PublishedCourses { get; set; }
        public int TotalEnrollments { get; set; }
        public int ActiveSubscriptions { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class AdminUserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ChangeRoleRequest
    {
        [Required]
        public string Role { get; set; }
    }
}
