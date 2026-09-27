using Microsoft.EntityFrameworkCore;
using SmartLearningPlatform.Data;
using SmartLearningPlatform.Models.Users;
using SmartLearningPlatform.Models.Users.Enums;

namespace SmartLearningPlatform.Repositories.Users;

public class UserSocialLinkRepository : Repository<UserSocialLink, long>, IUserSocialLinkRepository
{
    public UserSocialLinkRepository(ApplicationDbContext context) : base(context) { }

    protected override IQueryable<UserSocialLink> ApplyDefaultSort(IQueryable<UserSocialLink> query) =>
        query.OrderBy(l => l.UserId).ThenBy(l => l.Platform).ThenBy(l => l.Id);

    public override async Task<UserSocialLink?> GetByIdAsync(
        long id, CancellationToken cancellationToken = default) =>
        await GetWithUserAsync(id, cancellationToken);

    public async Task<IReadOnlyList<UserSocialLink>> ListWithUserAsync(
        long? userId = null, CancellationToken cancellationToken = default)
    {
        var query = Query().Include(l => l.User).ThenInclude(u => u!.Profile);
        if (userId.HasValue) query = query.Where(l => l.UserId == userId.Value)
                                          .Include(l => l.User).ThenInclude(u => u!.Profile);

        return await ApplyDefaultSort(query).ToListAsync(cancellationToken);
    }

    public async Task<UserSocialLink?> GetWithUserAsync(
        long id, CancellationToken cancellationToken = default) =>
        await Query().Include(l => l.User).ThenInclude(u => u!.Profile)
                     .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public Task<bool> LinkExistsAsync(
        long userId, SocialPlatform platform, long? excludeId = null,
        CancellationToken cancellationToken = default) =>
        Query().AnyAsync(
            l => l.UserId == userId && l.Platform == platform
                 && (excludeId == null || l.Id != excludeId),
            cancellationToken);
}
