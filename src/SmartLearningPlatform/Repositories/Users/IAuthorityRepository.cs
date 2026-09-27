using SmartLearningPlatform.Models.Users;

namespace SmartLearningPlatform.Repositories.Users;

public interface IAuthorityRepository : IRepository<Authority, long>
{
    Task<Authority?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Authority with the roles it has been granted to.</summary>
    Task<Authority?> GetWithRolesAsync(long id, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        string name, long? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authorities bucketed by the resource half of their <c>resource:action</c>
    /// name, which is how the index view groups them.
    /// </summary>
    Task<IReadOnlyList<IGrouping<string, Authority>>> ListGroupedByResourceAsync(
        CancellationToken cancellationToken = default);
}
