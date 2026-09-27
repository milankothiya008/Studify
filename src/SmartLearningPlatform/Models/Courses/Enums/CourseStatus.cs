using System.ComponentModel.DataAnnotations;

namespace SmartLearningPlatform.Models.Courses.Enums;

/// <summary>
/// Editorial state of a course. Direct port of the Java <c>CourseStatus</c>
/// enum, stored by name to mirror <c>@Enumerated(EnumType.STRING)</c>.
/// </summary>
public enum CourseStatus
{
    [Display(Name = "Draft")] Draft,
    [Display(Name = "Pending review")] PendingReview,
    [Display(Name = "Published")] Published,
    [Display(Name = "Rejected")] Rejected,
    [Display(Name = "Archived")] Archived,
}

public static class CourseStatusExtensions
{
    /// <summary>Only a published course is visible to learners.</summary>
    public static bool IsVisibleToLearners(this CourseStatus status) =>
        status == CourseStatus.Published;

    public static string BadgeClass(this CourseStatus status) => status switch
    {
        CourseStatus.Published => "success",
        CourseStatus.PendingReview => "warning text-dark",
        CourseStatus.Rejected => "danger",
        CourseStatus.Archived => "secondary",
        _ => "light text-dark border",
    };
}
