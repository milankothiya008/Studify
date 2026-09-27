using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using SmartLearningPlatform.Models.Common;
using SmartLearningPlatform.Models.Courses;
using SmartLearningPlatform.Models.Users.Enums;

namespace SmartLearningPlatform.Models.Users;

/// <summary>
/// An account. Port of the Java <c>User</c> entity (<c>users</c> table): the
/// credential and lockout columns live here, everything descriptive lives on
/// <see cref="UserProfile"/>.
/// </summary>
/// <remarks>
/// Derives from <see cref="IdentityUser{TKey}"/> so ASP.NET Core Identity owns
/// the credential handling, but every domain rule and every column name from the
/// port is preserved:
/// <list type="bullet">
/// <item>Identity's <c>AccessFailedCount</c> and <c>LockoutEnd</c> are overridden
/// to project onto the existing <c>failed_login_attempt</c> and
/// <c>account_locked_until</c> columns, so <c>UserManager</c>'s lockout
/// bookkeeping writes the same two fields the port already had.</item>
/// <item><see cref="Status"/>, <see cref="Enabled"/> and <see cref="CanSignIn"/>
/// stay the authority on whether a login is permitted;
/// <c>ApplicationSignInManager</c> defers to them.</item>
/// </list>
/// <see cref="UserDateAudit"/> cannot be a base class any more — C# allows one —
/// so the four audit properties are declared here and the same
/// <c>ITimestampAudit</c> / <c>IUserAudit</c> interfaces drive
/// <c>AuditInterceptor</c> exactly as before.
/// </remarks>
[Table("users")]
public class User : IdentityUser<long>, ITimestampAudit, IUserAudit, ISoftDeletable
{
    /// <summary>
    /// Login handle. Identity also keeps <c>UserName</c>, which this app holds
    /// equal to the email — the port had no separate user name.
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(256)]
    [Display(Name = "Email")]
    public override string? Email
    {
        get => base.Email;
        set => base.Email = value;
    }

    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [StringLength(32)]
    [Display(Name = "Phone number")]
    public override string? PhoneNumber
    {
        get => base.PhoneNumber;
        set => base.PhoneNumber = value;
    }

    /// <summary>
    /// Hash only — never the password itself. Written through
    /// <c>UserManager&lt;User&gt;</c>, which uses the same
    /// <c>IPasswordHasher&lt;User&gt;</c> (PBKDF2) the port used directly, so
    /// hashes written before Identity was introduced still verify.
    /// </summary>
    [Required]
    [StringLength(512)]
    [Display(Name = "Password hash")]
    public override string? PasswordHash
    {
        get => base.PasswordHash;
        set => base.PasswordHash = value;
    }

    [Display(Name = "Enabled")]
    public bool Enabled { get; set; } = true;

    [Display(Name = "Failed login attempts")]
    [Range(0, int.MaxValue)]
    public int FailedLoginAttempt { get; set; }

    /// <summary>
    /// Identity's counter, projected onto <see cref="FailedLoginAttempt"/> so
    /// <c>UserManager.AccessFailedAsync</c> increments the column the port
    /// already defined instead of adding a second one.
    /// </summary>
    [NotMapped]
    public override int AccessFailedCount
    {
        get => FailedLoginAttempt;
        set => FailedLoginAttempt = value;
    }

    [Display(Name = "Password changed at")]
    public DateTime? PasswordChangedAt { get; set; }

    [Display(Name = "Account locked until")]
    public DateTime? AccountLockedUntil { get; set; }

    /// <summary>
    /// Identity's lockout stamp, projected onto <see cref="AccountLockedUntil"/>.
    /// The stored value is naive UTC (Java's <c>LocalDateTime</c> shape), so it
    /// is read back as a UTC offset and written back stripped of its offset.
    /// </summary>
    [NotMapped]
    public override DateTimeOffset? LockoutEnd
    {
        get => AccountLockedUntil is null
            ? null
            : new DateTimeOffset(DateTime.SpecifyKind(AccountLockedUntil.Value, DateTimeKind.Utc));
        set => AccountLockedUntil = value?.UtcDateTime;
    }

    [Display(Name = "Status")]
    public UserStatus Status { get; set; } = UserStatus.PendingVerification;

    [Display(Name = "Deleted at")]
    public DateTime? DeletedAt { get; set; }

    // ─── Audit (was the UserDateAudit base class) ───

    [Display(Name = "Created at")]
    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; }

    [Display(Name = "Updated at")]
    [DataType(DataType.DateTime)]
    public DateTime UpdatedAt { get; set; }

    [Display(Name = "Created by")]
    public long? CreatedById { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public User? CreatedBy { get; set; }

    [Display(Name = "Updated by")]
    public long? UpdatedById { get; set; }

    [ForeignKey(nameof(UpdatedById))]
    public User? UpdatedBy { get; set; }

    // ─── Navigation ───

    /// <summary>One-to-one, profile side owns the shared primary key.</summary>
    public UserProfile? Profile { get; set; }

    public ICollection<UserSocialLink> SocialLinks { get; set; } = new List<UserSocialLink>();

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>Courses this user teaches (inverse of <c>Course.Instructor</c>).</summary>
    public ICollection<Course> CoursesTaught { get; set; } = new List<Course>();

    // ─── Derived ───

    /// <summary>Profile name when there is a profile, otherwise the email.</summary>
    [NotMapped]
    [Display(Name = "Display name")]
    public string DisplayName =>
        Profile is null || string.IsNullOrWhiteSpace(Profile.FullName)
            ? Email ?? string.Empty
            : Profile.FullName;

    /// <summary>True while a lockout stamped by failed logins is still in force.</summary>
    [NotMapped]
    public bool IsCurrentlyLockedOut =>
        AccountLockedUntil.HasValue && AccountLockedUntil.Value > DateTime.UtcNow;

    /// <summary>
    /// Mirrors the Java <c>UserStatus.isLoginAllowed()</c> gate and additionally
    /// honours the <c>Enabled</c> flag, the soft-delete stamp and any live lockout.
    /// <c>ApplicationSignInManager.CanSignInAsync</c> returns exactly this, so
    /// Identity's sign-in path enforces the port's rule rather than its own.
    /// </summary>
    [NotMapped]
    public bool CanSignIn =>
        Enabled && DeletedAt is null && Status.IsLoginAllowed() && !IsCurrentlyLockedOut;
}
