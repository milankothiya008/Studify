using System.ComponentModel.DataAnnotations;

namespace SmartLearningPlatform.Models.Courses.Enums;

/// <summary>
/// Difficulty band. Collapsed from the Java <c>course_levels</c> lookup table,
/// which carried only a name — the requested model list has no CourseLevel
/// entity, so it becomes an enum column on <c>course</c>.
/// </summary>
public enum CourseLevel
{
    [Display(Name = "Beginner")] Beginner,
    [Display(Name = "Intermediate")] Intermediate,
    [Display(Name = "Advanced")] Advanced,
    [Display(Name = "All levels")] AllLevels,
}
