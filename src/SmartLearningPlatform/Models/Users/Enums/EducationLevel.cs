using System.ComponentModel.DataAnnotations;

namespace SmartLearningPlatform.Models.Users.Enums;

/// <summary>
/// Collapsed from the Java <c>education_levels</c> lookup table.
/// </summary>
public enum EducationLevel
{
    [Display(Name = "High school")] HighSchool,
    [Display(Name = "Diploma")] Diploma,
    [Display(Name = "Associate degree")] Associate,
    [Display(Name = "Bachelor's degree")] Bachelors,
    [Display(Name = "Master's degree")] Masters,
    [Display(Name = "Doctorate")] Doctorate,
    [Display(Name = "Self-taught")] SelfTaught,
    [Display(Name = "Other")] Other,
}
