using SmartLearningPlatform.Models.Users;

namespace SmartLearningPlatform.Repositories.Users;

public interface IRoleRepository : IRepository<Role, long>
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Role with the authorities granted to it.</summary>
    Task<Role?> GetWithAuthoritiesAsync(long id, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        string? name, long? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>Assigned-user and granted-authority counts, keyed by role id.</summary>
    Task<IReadOnlyDictionary<long, (int Users, int Authorities)>> GetUsageCountsAsync(
        CancellationToken cancellationToken = default);
}
