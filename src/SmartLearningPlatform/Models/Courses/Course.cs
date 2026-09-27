using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartLearningPlatform.Models.Common;
using SmartLearningPlatform.Models.Courses.Enums;
using SmartLearningPlatform.Models.Users;

namespace SmartLearningPlatform.Models.Courses;

/// <summary>
/// The course aggregate root. Port of the Java <c>Course</c> (<c>course</c>
/// table): identity and workflow state live here, while the long-form text,
/// artwork and money each sit in their own one-to-one satellite so a course
/// list never has to drag them along.
/// </summary>
[Table("course")]
public class Course : UserDateAudit, ISoftDeletable
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Select an instructor.")]
    [Display(Name = "Instructor")]
    public long InstructorId { get; set; }

    [ForeignKey(nameof(InstructorId))]
    public User? Instructor { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 3)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Subtitle is required.")]
    [StringLength(300)]
    [Display(Name = "Subtitle")]
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>Collapsed from the Java <c>course_level_id</c> FK.</summary>
    [Required(ErrorMessage = "Select a level.")]
    [Display(Name = "Level")]
    public CourseLevel CourseLevel { get; set; } = CourseLevel.Beginner;

    [Required]
    [Display(Name = "Status")]
    public CourseStatus CourseStatus { get; set; } = CourseStatus.Draft;

    [Display(Name = "Deleted at")]
    public DateTime? DeletedAt { get; set; }

    // ─── One-to-one satellites (each shares this row's primary key) ───

    [Display(Name = "Details")]
    public CourseDetail? Detail { get; set; }

    [Display(Name = "Media")]
    public CourseMedia? Media { get; set; }

    [Display(Name = "Pricing")]
    public CoursePricing? Pricing { get; set; }

    /// <summary>
    /// True once the three satellites exist — the Java workflow refused to move
    /// a course out of DRAFT before that.
    /// </summary>
    [NotMapped]
    public bool IsReadyForReview => Detail is not null && Media is not null && Pricing is not null;
}
