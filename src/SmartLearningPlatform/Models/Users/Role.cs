using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using SmartLearningPlatform.Models.Common;

namespace SmartLearningPlatform.Models.Users;

/// <summary>
/// A named bundle of authorities. Port of the Java <c>Role</c> (<c>roles</c>).
/// </summary>
/// <remarks>
/// Derives from <see cref="IdentityRole{TKey}"/> so <c>RoleManager</c> and
/// Identity's role claims work against this table. <see cref="Description"/>,
/// the audit stamps, the soft-delete column and the <see cref="RoleAuthorities"/>
/// grant list are the port's own and are untouched; Identity only contributes
/// <c>NormalizedName</c> and <c>ConcurrencyStamp</c>.
/// </remarks>
[Table("roles")]
public class Role : IdentityRole<long>, ITimestampAudit, ISoftDeletable
{
    [Required(ErrorMessage = "Role name is required.")]
    [StringLength(64)]
    [Display(Name = "Name")]
    [RegularExpression("^[A-Z][A-Z0-9_]*$",
        ErrorMessage = "Use upper-case letters, digits and underscores, e.g. ROLE_INSTRUCTOR.")]
    public override string? Name
    {
        get => base.Name;
        set => base.Name = value;
    }

    [StringLength(256)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Created at")]
    public DateTime CreatedAt { get; set; }

    [Display(Name = "Updated at")]
    public DateTime UpdatedAt { get; set; }

    [Display(Name = "Deleted at")]
    public DateTime? DeletedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<RoleAuthority> RoleAuthorities { get; set; } = new List<RoleAuthority>();
}
