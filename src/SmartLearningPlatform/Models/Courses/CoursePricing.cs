using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartLearningPlatform.Models.Common;
using SmartLearningPlatform.Models.Courses.Enums;

namespace SmartLearningPlatform.Models.Courses;

/// <summary>
/// List price and discount for a course. Port of the Java <c>CoursePricing</c>
/// (<c>course_pricing</c>); <c>numeric(10,2)</c> is preserved and the
/// <c>currency_id</c> FK collapses into the <see cref="CurrencyCode"/> enum.
/// </summary>
[Table("course_pricing")]
public class CoursePricing : DateAudit, IValidatableObject
{
    [Key]
    [Display(Name = "Course")]
    public long CourseId { get; set; }

    [ForeignKey(nameof(CourseId))]
    public Course? Course { get; set; }

    [Required(ErrorMessage = "Price is required.")]
    [Range(0, 9_999_999.99, ErrorMessage = "Price must be between 0 and 9,999,999.99.")]
    [Column(TypeName = "numeric(10,2)")]
    [DataType(DataType.Currency)]
    [Display(Name = "Price")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Discount price is required.")]
    [Range(0, 9_999_999.99, ErrorMessage = "Discount price must be between 0 and 9,999,999.99.")]
    [Column(TypeName = "numeric(10,2)")]
    [DataType(DataType.Currency)]
    [Display(Name = "Discount price")]
    public decimal DiscountPrice { get; set; }

    [Required]
    [Display(Name = "Currency")]
    public CurrencyCode Currency { get; set; } = CurrencyCode.USD;

    [NotMapped]
    [Display(Name = "Free")]
    public bool IsFree => EffectivePrice == 0m;

    /// <summary>What a learner actually pays.</summary>
    [NotMapped]
    [Display(Name = "Effective price")]
    public decimal EffectivePrice => DiscountPrice > 0m && DiscountPrice < Price ? DiscountPrice : Price;

    /// <summary>Whole-percent saving, or null when nothing is discounted.</summary>
    [NotMapped]
    [Display(Name = "Discount")]
    public int? DiscountPercentage =>
        Price > 0m && DiscountPrice > 0m && DiscountPrice < Price
            ? (int)Math.Round((Price - DiscountPrice) / Price * 100m)
            : null;

    /// <summary>
    /// A discount above the list price is nonsense, and the columns are
    /// independent so no attribute can express it — hence the object-level check.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DiscountPrice > Price)
        {
            yield return new ValidationResult(
                "Discount price cannot be greater than the price.",
                new[] { nameof(DiscountPrice) });
        }
    }
}
