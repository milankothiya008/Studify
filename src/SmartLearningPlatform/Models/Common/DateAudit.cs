using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLearningPlatform.Models.Common;

/// <summary>
/// Creation / modification timestamps shared by every entity.
/// Port of the Java <c>@MappedSuperclass DateAudit</c>. EF Core has no
/// mapped-superclass concept, so this stays unmapped and its properties are
/// inherited into each derived entity's own table.
/// </summary>
public abstract class DateAudit : ITimestampAudit
{
    [Display(Name = "Created at")]
    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; }

    [Display(Name = "Updated at")]
    [DataType(DataType.DateTime)]
    public DateTime UpdatedAt { get; set; }
}
