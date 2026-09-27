using SmartLearningPlatform.Models.Users;

namespace SmartLearningPlatform.Repositories.Users;

public interface IUserProfileRepository : IRepository<UserProfile, long>
{
    Task<IReadOnlyList<UserProfile>> ListWithUserAsync(CancellationToken cancellationToken = default);

    Task<UserProfile?> GetWithUserAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>Users that have no profile row yet — the Create form's picker.</summary>
    Task<IReadOnlyList<User>> ListUsersWithoutProfileAsync(
        long? includeUserId = null, CancellationToken cancellationToken = default);
}
