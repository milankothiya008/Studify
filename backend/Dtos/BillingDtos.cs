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
        public string PlanName { get; set; }
        public DateTime? EndDate { get; set; }
        public int DaysLeft { get; set; }
    }

    public class PaymentDto
    {
        public int Id { get; set; }
        public string PaymentType { get; set; }

        // Course title or plan name.
        public string ItemName { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string TransactionId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CheckoutResponse
    {
        public string Message { get; set; }
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
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
        public DateTime CreatedAt { get; set; }
    }

    public class ChangeRoleRequest
    {
        [Required]
        public string Role { get; set; }
    }
}
