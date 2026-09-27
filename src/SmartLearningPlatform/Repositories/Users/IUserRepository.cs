using SmartLearningPlatform.Models.Users;
using SmartLearningPlatform.Models.Users.Enums;

namespace SmartLearningPlatform.Repositories.Users;

public interface IUserRepository : IRepository<User, long>
{
    /// <summary>Rows joined to their profile, for list and picker screens.</summary>
    Task<IReadOnlyList<User>> ListWithProfileAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<User>> SearchAsync(
        string? term, UserStatus? status, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Case-insensitive lookup by login handle.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Profile, social links, roles and each role's authorities.</summary>
    Task<User?> GetWithEverythingAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the address is taken. <paramref name="excludeUserId"/> lets an
    /// edit form ignore the row being edited.
    /// </summary>
    Task<bool> EmailExistsAsync(
        string email, long? excludeUserId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<UserStatus, int>> CountByStatusAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Every distinct authority name reachable from the user's roles. Feeds the
    /// authority claims issued at sign-in, so soft-deleted roles and authorities
    /// drop out through the usual global filters.
    /// </summary>
    Task<IReadOnlyList<string>> ListAuthorityNamesAsync(
        long userId, CancellationToken cancellationToken = default);
}
