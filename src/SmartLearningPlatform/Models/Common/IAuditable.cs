namespace SmartLearningPlatform.Models.Common;

/// <summary>Entity that records when its row was first written.</summary>
public interface ICreationAudit
{
    DateTime CreatedAt { get; set; }
}

/// <summary>Entity that records both creation and last-modification time.</summary>
public interface ITimestampAudit : ICreationAudit
{
    DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Entity that records which account wrote and last changed the row — the
/// Spring Data <c>@CreatedBy</c> / <c>@LastModifiedBy</c> pair.
/// </summary>
public interface IUserAudit
{
    long? CreatedById { get; set; }
    long? UpdatedById { get; set; }
}
