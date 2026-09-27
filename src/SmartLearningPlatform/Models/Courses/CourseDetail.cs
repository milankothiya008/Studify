using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartLearningPlatform.Models.Common;

namespace SmartLearningPlatform.Models.Courses;

/// <summary>
/// Long-form copy and the feature flags for a course. Port of the Java
/// <c>CourseDetail</c> (<c>course_details</c>); <c>@MapsId</c> becomes a shared
/// primary key, so <see cref="CourseId"/> is both PK and FK.
/// </summary>
[Table("course_details")]
public class CourseDetail : DateAudit
{
    [Key]
    [Display(Name = "Course")]
    public long CourseId { get; set; }

    [ForeignKey(nameof(CourseId))]
    public Course? Course { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Requirements are required.")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Requirements")]
    public string Requirement { get; set; } = string.Empty;

    [Required(ErrorMessage = "Learning outcomes are required.")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Learning outcomes")]
    public string LearningOutcome { get; set; } = string.Empty;

    /// <summary>Maps the Java <c>certification_available</c> column.</summary>
    [Display(Name = "Certificate on completion")]
    public bool HasCertificate { get; set; }

    [Display(Name = "Has assignments")]
    public bool HasAssignment { get; set; }

    [Display(Name = "Has project")]
    public bool HasProject { get; set; }

    [Display(Name = "Has quiz")]
    public bool HasQuiz { get; set; }
}
