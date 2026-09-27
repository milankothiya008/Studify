using SmartLearningPlatform.Models.Users;

namespace SmartLearningPlatform.Repositories.Users;

public interface IUserRoleRepository : IRepository<UserRole, long>
{
    Task<IReadOnlyList<UserRole>> ListWithRelationsAsync(
        long? userId = null, long? roleId = null, CancellationToken cancellationToken = default);

    Task<UserRole?> GetWithRelationsAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Guards the <c>uk_user_role</c> constraint before saving.</summary>
    Task<bool> AssignmentExistsAsync(
        long userId, long roleId, long? excludeId = null,
        CancellationToken cancellationToken = default);
}
