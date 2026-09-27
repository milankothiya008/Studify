using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SmartLearningPlatform.Data;
using SmartLearningPlatform.Models.Common;

namespace SmartLearningPlatform.Repositories;

/// <summary>
/// Shared EF Core implementation of <see cref="IRepository{TEntity,TKey}"/>.
/// Each entity repository derives from this and adds only its own queries.
/// </summary>
public abstract class Repository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class
{
    protected Repository(ApplicationDbContext context)
    {
        Context = context;
        Set = context.Set<TEntity>();
    }

    protected ApplicationDbContext Context { get; }

    protected DbSet<TEntity> Set { get; }

    /// <summary>
    /// Base query for read paths. Tracking is off because MVC read actions
    /// render and discard; write paths use <see cref="Set"/> directly.
    /// </summary>
    protected virtual IQueryable<TEntity> Query() => Set.AsNoTracking();

    /// <summary>Stable ordering so paging never repeats or skips a row.</summary>
    protected abstract IQueryable<TEntity> ApplyDefaultSort(IQueryable<TEntity> query);

    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await ApplyDefaultSort(Query()).ToListAsync(cancellationToken);

    public virtual async Task<PagedResult<TEntity>> ListPagedAsync(
        int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = ApplyDefaultSort(Query());
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = total,
        };
    }

    public virtual Task<TEntity?> GetByIdAsync(
        TKey id, CancellationToken cancellationToken = default) =>
        // Deliberately not Set.FindAsync: that attaches the row to the change
        // tracker, and a controller which checks existence and then saves the
        // instance it got from model binding would hit "another instance with the
        // key value is already being tracked". Read paths stay untracked; writes
        // go through GetForUpdateAsync.
        Query().FirstOrDefaultAsync(KeyEquals(id), cancellationToken);

    public virtual async Task<TEntity?> GetForUpdateAsync(
        TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await Set.FindAsync(new object?[] { id }, cancellationToken);
        return entity is null || IsSoftDeleted(entity) ? null : entity;
    }

    public virtual Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default) =>
        // An existence check has no business materialising the entity.
        Query().AnyAsync(KeyEquals(id), cancellationToken);

    public virtual Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        Query().CountAsync(cancellationToken);

    public virtual async Task<TEntity> AddAsync(
        TEntity entity, CancellationToken cancellationToken = default)
    {
        await Set.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task<TEntity> UpdateAsync(
        TEntity entity, CancellationToken cancellationToken = default)
    {
        // A detached entity came straight from model binding, so every scalar it
        // carries is authoritative and the whole thing is marked modified. An
        // already-tracked entity was loaded for editing, and the change tracker
        // knows precisely which columns moved — calling Update() on that would
        // needlessly rewrite untouched rows reached through its navigations.
        if (Context.Entry(entity).State == EntityState.Detached) Set.Update(entity);

        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await Set.FindAsync(new object?[] { id }, cancellationToken);
        if (entity is null || IsSoftDeleted(entity)) return false;

        if (entity is ISoftDeletable soft)
        {
            // Retire rather than remove, so audit history and FKs survive.
            soft.DeletedAt = DateTime.UtcNow;
            Set.Update(entity);
        }
        else
        {
            Set.Remove(entity);
        }

        await Context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Builds <c>e =&gt; e.&lt;primary key&gt; == id</c> from the model's own key
    /// metadata, so the generic base can filter by id without each of the eleven
    /// repositories having to restate which property that is. Every entity here
    /// has a single-column key.
    /// </summary>
    protected Expression<Func<TEntity, bool>> KeyEquals(TKey id)
    {
        var key = Context.Model.FindEntityType(typeof(TEntity))?.FindPrimaryKey()
            ?? throw new InvalidOperationException(
                $"{typeof(TEntity).Name} has no primary key in the EF model.");

        if (key.Properties.Count != 1)
        {
            throw new InvalidOperationException(
                $"{typeof(TEntity).Name} has a composite key; override GetByIdAsync instead.");
        }

        var parameter = Expression.Parameter(typeof(TEntity), "e");
        Expression left = Expression.Property(parameter, key.Properties[0].Name);

        // The id is read off a captured object rather than baked in as a
        // constant, so EF emits a parameter and the query plan stays reusable.
        Expression right = Expression.Field(
            Expression.Constant(new KeyHolder { Value = id }), nameof(KeyHolder.Value));

        if (left.Type != right.Type) right = Expression.Convert(right, left.Type);

        return Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(left, right), parameter);
    }

    private static bool IsSoftDeleted(TEntity entity) =>
        entity is ISoftDeletable { DeletedAt: not null };

    private sealed class KeyHolder
    {
        public TKey Value = default!;
    }
}
