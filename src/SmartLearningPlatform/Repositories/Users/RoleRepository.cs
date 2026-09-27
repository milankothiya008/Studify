using Microsoft.EntityFrameworkCore;
using SmartLearningPlatform.Data;
using SmartLearningPlatform.Models.Users;

namespace SmartLearningPlatform.Repositories.Users;

public class RoleRepository : Repository<Role, long>, IRoleRepository
{
    public RoleRepository(ApplicationDbContext context) : base(context) { }

    protected override IQueryable<Role> ApplyDefaultSort(IQueryable<Role> query) =>
        query.OrderBy(r => r.Name);

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var normalized = name.Trim().ToLower();
        return await Query().FirstOrDefaultAsync(r => r.Name!.ToLower() == normalized, cancellationToken);
    }

    public async Task<Role?> GetWithAuthoritiesAsync(
        long id, CancellationToken cancellationToken = default) =>
        await Query()
            .Include(r => r.RoleAuthorities).ThenInclude(ra => ra.Authority)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(
        string? name, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return Task.FromResult(false);

        var normalized = name.Trim().ToLower();
        return Query().AnyAsync(
            r => r.Name!.ToLower() == normalized && (excludeId == null || r.Id != excludeId),
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<long, (int Users, int Authorities)>> GetUsageCountsAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await Query()
            .Select(r => new
            {
                r.Id,
                Users = r.UserRoles.Count(),
                Authorities = r.RoleAuthorities.Count(),
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Id, r => (r.Users, r.Authorities));
    }
}
