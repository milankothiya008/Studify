using System.ComponentModel.DataAnnotations;

namespace SmartLearningPlatform.Models.Users.Enums;

/// <summary>
/// Lifecycle state of an account. Direct port of the Java <c>UserStatus</c>
/// enum, persisted by name (not ordinal) so the column stays readable and
/// survives re-ordering, matching <c>@Enumerated(EnumType.STRING)</c>.
/// </summary>
public enum UserStatus
{
    // ─── Onboarding ───
    [Display(Name = "Pending verification")] PendingVerification,
    [Display(Name = "Verified")] Verified,
    [Display(Name = "Active")] Active,

    // ─── Restricted ───
    [Display(Name = "Suspended")] Suspended,
    [Display(Name = "Blocked")] Blocked,
    [Display(Name = "Banned")] Banned,

    // ─── Inactive ───
    [Display(Name = "Inactive")] Inactive,
    [Display(Name = "Deactivated")] Deactivated,
    [Display(Name = "Deleted")] Deleted,

    // ─── Special ───
    [Display(Name = "Under review")] UnderReview,
    [Display(Name = "Locked")] Locked,
    [Display(Name = "Password reset required")] PasswordResetRequired,
}

/// <summary>
/// The predicates that lived as instance methods on the Java enum. C# enums
/// cannot carry methods, so they become extension methods with identical logic.
/// </summary>
public static class UserStatusExtensions
{
    public static bool IsEnabled(this UserStatus status) =>
        status is UserStatus.Active or UserStatus.Verified;

    public static bool IsAccountNonLocked(this UserStatus status) =>
        status is not (UserStatus.Blocked or UserStatus.Banned
                       or UserStatus.Locked or UserStatus.Suspended);

    public static bool IsLoginAllowed(this UserStatus status) =>
        status.IsEnabled() && status.IsAccountNonLocked();

    public static bool IsSoftDeleted(this UserStatus status) =>
        status is UserStatus.Deleted or UserStatus.Deactivated;

    /// <summary>Bootstrap CSS context used to colour the status badge in views.</summary>
    public static string BadgeClass(this UserStatus status) => status switch
    {
        UserStatus.Active or UserStatus.Verified => "success",
        UserStatus.PendingVerification or UserStatus.UnderReview => "warning text-dark",
        UserStatus.Blocked or UserStatus.Banned or UserStatus.Deleted => "danger",
        UserStatus.Locked or UserStatus.PasswordResetRequired => "dark",
        _ => "secondary",
    };
}
