using Microsoft.EntityFrameworkCore;
using SmartLearningPlatform.Data;
using SmartLearningPlatform.Models.Users;
using SmartLearningPlatform.Models.Users.Enums;

namespace SmartLearningPlatform.Repositories.Users;

public class UserRepository : Repository<User, long>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    protected override IQueryable<User> ApplyDefaultSort(IQueryable<User> query) =>
        query.OrderBy(u => u.Email).ThenBy(u => u.Id);

    public override async Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await Query().Include(u => u.Profile)
                     .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyList<User>> ListWithProfileAsync(
        CancellationToken cancellationToken = default) =>
        await ApplyDefaultSort(Query().Include(u => u.Profile)).ToListAsync(cancellationToken);

    public async Task<PagedResult<User>> SearchAsync(
        string? term, UserStatus? status, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = Query().Include(u => u.Profile).AsQueryable();

        if (!string.IsNullOrWhiteSpace(term))
        {
            var pattern = $"%{term.Trim()}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.Email!, pattern)
                || (u.PhoneNumber != null && EF.Functions.ILike(u.PhoneNumber, pattern))
                || (u.Profile != null && EF.Functions.ILike(u.Profile.FirstName, pattern))
                || (u.Profile != null && EF.Functions.ILike(u.Profile.LastName, pattern)));
        }

        if (status.HasValue) query = query.Where(u => u.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await ApplyDefaultSort(query)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<User>
        {
            Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total,
        };
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        var normalized = email.Trim();
        return await Query()
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Email!.ToLower() == normalized.ToLower(), cancellationToken);
    }

    public async Task<User?> GetWithEverythingAsync(long id, CancellationToken cancellationToken = default) =>
        await Query()
            .Include(u => u.Profile)
            .Include(u => u.SocialLinks)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .ThenInclude(r => r!.RoleAuthorities).ThenInclude(ra => ra.Authority)
            .Include(u => u.CoursesTaught)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<bool> EmailExistsAsync(
        string email, long? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        var normalized = email.Trim().ToLower();
        return await Query().AnyAsync(
            u => u.Email!.ToLower() == normalized && (excludeUserId == null || u.Id != excludeUserId),
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<UserStatus, int>> CountByStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await Query()
            .GroupBy(u => u.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Status, r => r.Count);
    }

    public async Task<IReadOnlyList<string>> ListAuthorityNamesAsync(
        long userId, CancellationToken cancellationToken = default) =>
        await Query()
            .Where(u => u.Id == userId)
            .SelectMany(u => u.UserRoles)
            .SelectMany(ur => ur.Role!.RoleAuthorities)
            .Select(ra => ra.Authority!.Name)
            .Distinct()
            .ToListAsync(cancellationToken);
}
