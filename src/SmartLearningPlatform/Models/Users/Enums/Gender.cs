using System.ComponentModel.DataAnnotations;

namespace SmartLearningPlatform.Models.Users.Enums;

/// <summary>
/// Collapsed from the Java <c>genders</c> lookup table, which carried nothing
/// but a name.
/// </summary>
public enum Gender
{
    [Display(Name = "Male")] Male,
    [Display(Name = "Female")] Female,
    [Display(Name = "Non-binary")] NonBinary,
    [Display(Name = "Other")] Other,
    [Display(Name = "Prefer not to say")] PreferNotToSay,
}
