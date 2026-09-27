namespace SmartLearningPlatform.Models.Common;

/// <summary>
/// Marks an entity that is retired by stamping <c>DeletedAt</c> rather than
/// being removed. The Java side expressed this as a nullable
/// <c>deleted_at</c> column on each entity; here a single interface lets
/// <c>ApplicationDbContext</c> attach one global query filter per entity and
/// lets the generic repository turn deletes into soft deletes.
/// </summary>
public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
}
