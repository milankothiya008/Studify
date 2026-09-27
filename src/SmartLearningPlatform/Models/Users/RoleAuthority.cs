using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartLearningPlatform.Models.Common;

namespace SmartLearningPlatform.Models.Users;

/// <summary>
/// Join row granting an <see cref="Authority"/> to a <see cref="Role"/>. Port of
/// the Java <c>RoleAuthority</c>; the <c>uk_role_authority</c> unique constraint
/// over (role_id, authority_id) is reproduced in <c>ApplicationDbContext</c>.
/// </summary>
[Table("role_authority")]
public class RoleAuthority : ICreationAudit
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Select a role.")]
    [Display(Name = "Role")]
    public long RoleId { get; set; }

    [ForeignKey(nameof(RoleId))]
    public Role? Role { get; set; }

    [Required(ErrorMessage = "Select an authority.")]
    [Display(Name = "Authority")]
    public long AuthorityId { get; set; }

    [ForeignKey(nameof(AuthorityId))]
    public Authority? Authority { get; set; }

    [Display(Name = "Granted at")]
    public DateTime CreatedAt { get; set; }
}
