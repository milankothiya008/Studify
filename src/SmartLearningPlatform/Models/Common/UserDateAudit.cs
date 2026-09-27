using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartLearningPlatform.Models.Users;

namespace SmartLearningPlatform.Models.Common;

/// <summary>
/// Adds "who touched it" columns on top of <see cref="DateAudit"/>.
/// Port of the Java <c>@MappedSuperclass UserDateAudit</c> whose
/// <c>@CreatedBy</c> / <c>@LastModifiedBy</c> were filled by Spring Data
/// auditing; here <c>AuditInterceptor</c> fills them.
/// </summary>
public abstract class UserDateAudit : DateAudit, IUserAudit
{
    [Display(Name = "Created by")]
    public long? CreatedById { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public User? CreatedBy { get; set; }

    [Display(Name = "Updated by")]
    public long? UpdatedById { get; set; }

    [ForeignKey(nameof(UpdatedById))]
    public User? UpdatedBy { get; set; }
}
