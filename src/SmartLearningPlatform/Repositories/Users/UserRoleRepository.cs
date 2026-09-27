using Microsoft.EntityFrameworkCore;
using SmartLearningPlatform.Data;
using SmartLearningPlatform.Models.Users;

namespace SmartLearningPlatform.Repositories.Users;

public class UserRoleRepository : Repository<UserRole, long>, IUserRoleRepository
{
    public UserRoleRepository(ApplicationDbContext context) : base(context) { }

    protected override IQueryable<UserRole> ApplyDefaultSort(IQueryable<UserRole> query) =>
        query.OrderByDescending(ur => ur.CreatedAt).ThenBy(ur => ur.Id);

    public override async Task<UserRole?> GetByIdAsync(
        long id, CancellationToken cancellationToken = default) =>
        await GetWithRelationsAsync(id, cancellationToken);

    public async Task<IReadOnlyList<UserRole>> ListWithRelationsAsync(
        long? userId = null, long? roleId = null, CancellationToken cancellationToken = default)
    {
        var query = Query()
            .Include(ur => ur.User).ThenInclude(u => u!.Profile)
            .Include(ur => ur.Role)
            .AsQueryable();

        if (userId.HasValue) query = query.Where(ur => ur.UserId == userId.Value);
        if (roleId.HasValue) query = query.Where(ur => ur.RoleId == roleId.Value);

        return await ApplyDefaultSort(query).ToListAsync(cancellationToken);
    }

    public async Task<UserRole?> GetWithRelationsAsync(
        long id, CancellationToken cancellationToken = default) =>
        await Query()
            .Include(ur => ur.User).ThenInclude(u => u!.Profile)
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.Id == id, cancellationToken);

    public Task<bool> AssignmentExistsAsync(
        long userId, long roleId, long? excludeId = null,
        CancellationToken cancellationToken = default) =>
        Query().AnyAsync(
            ur => ur.UserId == userId && ur.RoleId == roleId
                  && (excludeId == null || ur.Id != excludeId),
            cancellationToken);
}
