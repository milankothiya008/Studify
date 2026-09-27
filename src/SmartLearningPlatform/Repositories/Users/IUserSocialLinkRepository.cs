using SmartLearningPlatform.Models.Users;
using SmartLearningPlatform.Models.Users.Enums;

namespace SmartLearningPlatform.Repositories.Users;

public interface IUserSocialLinkRepository : IRepository<UserSocialLink, long>
{
    Task<IReadOnlyList<UserSocialLink>> ListWithUserAsync(
        long? userId = null, CancellationToken cancellationToken = default);

    Task<UserSocialLink?> GetWithUserAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Guards the one-link-per-platform-per-user rule before saving.</summary>
    Task<bool> LinkExistsAsync(
        long userId, SocialPlatform platform, long? excludeId = null,
        CancellationToken cancellationToken = default);
}
