using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using SmartLearningPlatform.Models.Common;

namespace SmartLearningPlatform.Models.Users;

/// <summary>
/// Join row assigning a <see cref="Role"/> to a <see cref="User"/>. Port of the
/// Java <c>UserRole</c>; the <c>uk_user_role</c> unique constraint over
/// (user_id, role_id) is reproduced in <c>ApplicationDbContext</c>.
/// </summary>
/// <remarks>
/// Doubles as Identity's user-role join by deriving from
/// <see cref="IdentityUserRole{TKey}"/>, so <c>UserManager.GetRolesAsync</c>
/// reads the rows this screen maintains. Identity's own shape for this table is
/// a composite (user_id, role_id) key with no navigations; the port's surrogate
/// <see cref="Id"/>, its navigations and its <c>assigned_at</c> stamp are kept
/// instead, and <c>ApplicationUserStore</c> adjusts the one store method that
/// assumed the composite key.
/// </remarks>
[Table("user_roles")]
public class UserRole : IdentityUserRole<long>, ICreationAudit
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Select a user.")]
    [Display(Name = "User")]
    public override long UserId
    {
        get => base.UserId;
        set => base.UserId = value;
    }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required(ErrorMessage = "Select a role.")]
    [Display(Name = "Role")]
    public override long RoleId
    {
        get => base.RoleId;
        set => base.RoleId = value;
    }

    [ForeignKey(nameof(RoleId))]
    public Role? Role { get; set; }

    [Display(Name = "Assigned at")]
    public DateTime CreatedAt { get; set; }
}
