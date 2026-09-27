using Microsoft.EntityFrameworkCore;
using SmartLearningPlatform.Data;
using SmartLearningPlatform.Models.Users;

namespace SmartLearningPlatform.Repositories.Users;

public class AuthorityRepository : Repository<Authority, long>, IAuthorityRepository
{
    public AuthorityRepository(ApplicationDbContext context) : base(context) { }

    protected override IQueryable<Authority> ApplyDefaultSort(IQueryable<Authority> query) =>
        query.OrderBy(a => a.Name);

    public async Task<Authority?> GetByNameAsync(
        string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var normalized = name.Trim().ToLower();
        return await Query().FirstOrDefaultAsync(a => a.Name.ToLower() == normalized, cancellationToken);
    }

    public async Task<Authority?> GetWithRolesAsync(
        long id, CancellationToken cancellationToken = default) =>
        await Query()
            .Include(a => a.RoleAuthorities).ThenInclude(ra => ra.Role)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(
        string name, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return Task.FromResult(false);

        var normalized = name.Trim().ToLower();
        return Query().AnyAsync(
            a => a.Name.ToLower() == normalized && (excludeId == null || a.Id != excludeId),
            cancellationToken);
    }

    public async Task<IReadOnlyList<IGrouping<string, Authority>>> ListGroupedByResourceAsync(
        CancellationToken cancellationToken = default)
    {
        // Group in memory: splitting on ':' has no clean SQL translation and the
        // authority table is small by nature.
        var all = await ApplyDefaultSort(Query()).ToListAsync(cancellationToken);

        return all
            .GroupBy(a => a.Name.Contains(':') ? a.Name.Split(':')[0] : "other")
            .OrderBy(g => g.Key)
            .ToList();
    }
}
