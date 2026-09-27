using Microsoft.EntityFrameworkCore;
using SmartLearningPlatform.Data;
using SmartLearningPlatform.Models.Users;

namespace SmartLearningPlatform.Repositories.Users;

public class UserProfileRepository : Repository<UserProfile, long>, IUserProfileRepository
{
    public UserProfileRepository(ApplicationDbContext context) : base(context) { }

    protected override IQueryable<UserProfile> ApplyDefaultSort(IQueryable<UserProfile> query) =>
        query.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ThenBy(p => p.UserId);

    public override async Task<UserProfile?> GetByIdAsync(
        long id, CancellationToken cancellationToken = default) =>
        await GetWithUserAsync(id, cancellationToken);

    public async Task<IReadOnlyList<UserProfile>> ListWithUserAsync(
        CancellationToken cancellationToken = default) =>
        await ApplyDefaultSort(Query().Include(p => p.User)).ToListAsync(cancellationToken);

    public async Task<UserProfile?> GetWithUserAsync(
        long userId, CancellationToken cancellationToken = default) =>
        await Query().Include(p => p.User)
                     .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<User>> ListUsersWithoutProfileAsync(
        long? includeUserId = null, CancellationToken cancellationToken = default) =>
        await Context.Users
            .AsNoTracking()
            .Where(u => u.Profile == null || (includeUserId != null && u.Id == includeUserId))
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);
}
