using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartLearningPlatform.Models.Common;
using SmartLearningPlatform.Services;

namespace SmartLearningPlatform.Services;

/// <summary>
/// Fills the audit columns on the way into the database, replacing Hibernate's
/// <c>@CreationTimestamp</c> / <c>@UpdateTimestamp</c> and Spring Data's
/// <c>AuditingEntityListener</c>. Doing it in an interceptor means no repository
/// or controller has to remember, and a hand-written timestamp in a test or a
/// seed run is left alone.
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserAccessor _currentUser;

    public AuditInterceptor(ICurrentUserAccessor currentUser) => _currentUser = currentUser;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null) return;

        // One timestamp for the whole SaveChanges, so rows written together agree.
        var now = DateTime.UtcNow;
        var actingUserId = _currentUser.GetCurrentUserId();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;

            if (entry.Entity is ICreationAudit creation && entry.State == EntityState.Added)
            {
                creation.CreatedAt = now;
            }

            if (entry.Entity is ITimestampAudit timestamped)
            {
                timestamped.UpdatedAt = now;
            }

            if (entry.Entity is IUserAudit userAudited)
            {
                if (entry.State == EntityState.Added) userAudited.CreatedById = actingUserId;
                userAudited.UpdatedById = actingUserId;

                // Never let an update blank out who created the row.
                if (entry.State == EntityState.Modified)
                {
                    entry.Property(nameof(IUserAudit.CreatedById)).IsModified = false;
                }
            }

            // created_at is immutable once written.
            if (entry.State == EntityState.Modified && entry.Entity is ICreationAudit)
            {
                entry.Property(nameof(ICreationAudit.CreatedAt)).IsModified = false;
            }
        }
    }
}
