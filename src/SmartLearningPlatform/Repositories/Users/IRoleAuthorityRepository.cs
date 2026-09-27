using SmartLearningPlatform.Models.Users;

namespace SmartLearningPlatform.Repositories.Users;

public interface IRoleAuthorityRepository : IRepository<RoleAuthority, long>
{
    Task<IReadOnlyList<RoleAuthority>> ListWithRelationsAsync(
        long? roleId = null, long? authorityId = null, CancellationToken cancellationToken = default);

    Task<RoleAuthority?> GetWithRelationsAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Guards the <c>uk_role_authority</c> constraint before saving.</summary>
    Task<bool> GrantExistsAsync(
        long roleId, long authorityId, long? excludeId = null,
        CancellationToken cancellationToken = default);
}
