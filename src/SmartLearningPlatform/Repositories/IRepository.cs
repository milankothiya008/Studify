namespace SmartLearningPlatform.Repositories;

/// <summary>
/// The CRUD surface every entity repository shares, equivalent to Spring Data's
/// <c>JpaRepository</c>. Entity-specific queries live on the derived interfaces.
/// </summary>
/// <typeparam name="TEntity">Mapped entity type.</typeparam>
/// <typeparam name="TKey">Primary key type.</typeparam>
public interface IRepository<TEntity, TKey> where TEntity : class
{
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<TEntity>> ListPagedAsync(
        int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the row <em>tracked</em>, for a read-modify-save edit. The plain
    /// <c>GetByIdAsync</c> returns a detached graph meant for rendering; saving
    /// that back would mark every loaded navigation modified as well.
    /// </summary>
    Task<TEntity?> GetForUpdateAsync(TKey id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the row, or stamps <c>DeletedAt</c> when the entity is
    /// soft-deletable. Returns false when no such row exists.
    /// </summary>
    Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default);
}
