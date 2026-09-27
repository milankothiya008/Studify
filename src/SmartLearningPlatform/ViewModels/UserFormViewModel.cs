using System.ComponentModel.DataAnnotations;
using SmartLearningPlatform.Models.Users;
using SmartLearningPlatform.Models.Users.Enums;

namespace SmartLearningPlatform.ViewModels;

/// <summary>
/// Create/Edit form for <see cref="User"/>. The entity is deliberately not bound
/// straight to the form: <c>PasswordHash</c> must never travel to the browser or
/// back, so the form carries a plaintext password that the controller hashes and
/// discards. On edit the password is optional — blank means "leave it alone".
/// </summary>
public class UserFormViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [StringLength(32)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Password)]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Use at least 8 characters.")]
    [Display(Name = "Password")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string? ConfirmPassword { get; set; }

    [Display(Name = "Status")]
    public UserStatus Status { get; set; } = UserStatus.PendingVerification;

    [Display(Name = "Enabled")]
    public bool Enabled { get; set; } = true;

    [Display(Name = "Failed login attempts")]
    [Range(0, int.MaxValue)]
    public int FailedLoginAttempt { get; set; }

    [Display(Name = "Account locked until")]
    [DataType(DataType.DateTime)]
    public DateTime? AccountLockedUntil { get; set; }

    /// <summary>False on Create, where a password is mandatory.</summary>
    public bool IsEdit => Id != 0;

    public static UserFormViewModel FromEntity(User user) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        PhoneNumber = user.PhoneNumber,
        Status = user.Status,
        Enabled = user.Enabled,
        FailedLoginAttempt = user.FailedLoginAttempt,
        AccountLockedUntil = user.AccountLockedUntil,
    };

    /// <summary>Copies the editable fields onto a tracked entity.</summary>
    public void ApplyTo(User user)
    {
        user.Email = Email.Trim();

        // Identity keys its lookups on UserName; this app has no separate handle,
        // so the two are kept equal. UserManager derives the normalised columns
        // from these on the way to the database.
        user.UserName = user.Email;
        user.PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim();
        user.Status = Status;
        user.Enabled = Enabled;
        user.FailedLoginAttempt = FailedLoginAttempt;
        user.AccountLockedUntil = AccountLockedUntil;
    }
}
