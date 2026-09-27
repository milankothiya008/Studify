using Microsoft.EntityFrameworkCore;
using SmartLearningPlatform.Data;
using SmartLearningPlatform.Models.Users;

namespace SmartLearningPlatform.Repositories.Users;

public class RoleAuthorityRepository : Repository<RoleAuthority, long>, IRoleAuthorityRepository
{
    public RoleAuthorityRepository(ApplicationDbContext context) : base(context) { }

    protected override IQueryable<RoleAuthority> ApplyDefaultSort(IQueryable<RoleAuthority> query) =>
        query.OrderBy(ra => ra.RoleId).ThenBy(ra => ra.AuthorityId);

    public override async Task<RoleAuthority?> GetByIdAsync(
        long id, CancellationToken cancellationToken = default) =>
        await GetWithRelationsAsync(id, cancellationToken);

    public async Task<IReadOnlyList<RoleAuthority>> ListWithRelationsAsync(
        long? roleId = null, long? authorityId = null, CancellationToken cancellationToken = default)
    {
        var query = Query().Include(ra => ra.Role).Include(ra => ra.Authority).AsQueryable();

        if (roleId.HasValue) query = query.Where(ra => ra.RoleId == roleId.Value);
        if (authorityId.HasValue) query = query.Where(ra => ra.AuthorityId == authorityId.Value);

        return await ApplyDefaultSort(query).ToListAsync(cancellationToken);
    }

    public async Task<RoleAuthority?> GetWithRelationsAsync(
        long id, CancellationToken cancellationToken = default) =>
        await Query()
            .Include(ra => ra.Role)
            .Include(ra => ra.Authority)
            .FirstOrDefaultAsync(ra => ra.Id == id, cancellationToken);

    public Task<bool> GrantExistsAsync(
        long roleId, long authorityId, long? excludeId = null,
        CancellationToken cancellationToken = default) =>
        Query().AnyAsync(
            ra => ra.RoleId == roleId && ra.AuthorityId == authorityId
                  && (excludeId == null || ra.Id != excludeId),
            cancellationToken);
}
