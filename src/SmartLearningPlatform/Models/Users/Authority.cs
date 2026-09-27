using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartLearningPlatform.Models.Common;

namespace SmartLearningPlatform.Models.Users;

/// <summary>
/// A single fine-grained permission granted through a role. Port of the Java
/// <c>Authority</c> (<c>authority</c> table).
/// </summary>
[Table("authority")]
public class Authority : ITimestampAudit, ISoftDeletable
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Authority name is required.")]
    [StringLength(128)]
    [Display(Name = "Name")]
    [RegularExpression("^[a-z][a-z0-9_]*:[a-z][a-z0-9_]*$",
        ErrorMessage = "Use the resource:action form, e.g. course:publish.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(256)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Created at")]
    public DateTime CreatedAt { get; set; }

    [Display(Name = "Updated at")]
    public DateTime UpdatedAt { get; set; }

    [Display(Name = "Deleted at")]
    public DateTime? DeletedAt { get; set; }

    public ICollection<RoleAuthority> RoleAuthorities { get; set; } = new List<RoleAuthority>();
}
