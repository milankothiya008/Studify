using System.ComponentModel.DataAnnotations;

namespace SmartLearningPlatform.Models.Users.Enums;

/// <summary>
/// Collapsed from the Java <c>professions</c> lookup table.
/// </summary>
public enum Profession
{
    [Display(Name = "Student")] Student,
    [Display(Name = "Software engineer")] SoftwareEngineer,
    [Display(Name = "Data scientist")] DataScientist,
    [Display(Name = "Designer")] Designer,
    [Display(Name = "Teacher / lecturer")] Teacher,
    [Display(Name = "Researcher")] Researcher,
    [Display(Name = "Manager")] Manager,
    [Display(Name = "Entrepreneur")] Entrepreneur,
    [Display(Name = "Other")] Other,
}
