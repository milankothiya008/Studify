using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartLearningPlatform.Models.Common;
using SmartLearningPlatform.Models.Users.Enums;

namespace SmartLearningPlatform.Models.Users;

/// <summary>
/// Descriptive half of an account. Port of the Java <c>UserProfile</c>
/// (<c>user_profiles</c> table): <c>@MapsId</c> becomes a shared primary key,
/// so <see cref="UserId"/> is both the PK and the FK to <c>users</c>.
/// </summary>
[Table("user_profiles")]
public class UserProfile : ITimestampAudit
{
    /// <summary>Shared primary key — equals the owning user's id.</summary>
    [Key]
    [Display(Name = "User")]
    public long UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "About me")]
    [DataType(DataType.MultilineText)]
    public string? AboutMe { get; set; }

    [Display(Name = "Education level")]
    public EducationLevel? EducationLevel { get; set; }

    [Display(Name = "Profession")]
    public Profession? Profession { get; set; }

    [Display(Name = "Gender")]
    public Gender? Gender { get; set; }

    [Display(Name = "Date of birth")]
    [DataType(DataType.Date)]
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>Owned type — columns prefixed <c>home_</c>.</summary>
    [Display(Name = "Home address")]
    public Address HomeAddress { get; set; } = new();

    /// <summary>Owned type — columns prefixed <c>work_</c>.</summary>
    [Display(Name = "Work address")]
    public Address WorkAddress { get; set; } = new();

    [Display(Name = "Institute name")]
    [StringLength(200)]
    public string? InstituteName { get; set; }

    /// <summary>
    /// The Java side pointed at a <c>files</c> row managed by Cloudinary. With
    /// no File entity in the requested model list the URL is stored directly.
    /// </summary>
    [Display(Name = "Profile picture URL")]
    [Url(ErrorMessage = "Enter a valid URL.")]
    [StringLength(1000)]
    public string? ProfilePictureUrl { get; set; }

    [Display(Name = "Created at")]
    public DateTime CreatedAt { get; set; }

    [Display(Name = "Updated at")]
    public DateTime UpdatedAt { get; set; }

    [NotMapped]
    [Display(Name = "Full name")]
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>Whole years elapsed since <see cref="DateOfBirth"/>.</summary>
    [NotMapped]
    public int? Age
    {
        get
        {
            if (DateOfBirth is not { } dob) return null;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var age = today.Year - dob.Year;
            if (dob > today.AddYears(-age)) age--;
            return age < 0 ? null : age;
        }
    }
}
