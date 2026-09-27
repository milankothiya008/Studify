using Microsoft.EntityFrameworkCore;
using SmartLearningPlatform.Data;
using SmartLearningPlatform.Models.Courses;

namespace SmartLearningPlatform.Repositories.Courses;

public class CoursePricingRepository : Repository<CoursePricing, long>, ICoursePricingRepository
{
    public CoursePricingRepository(ApplicationDbContext context) : base(context) { }

    protected override IQueryable<CoursePricing> ApplyDefaultSort(IQueryable<CoursePricing> query) =>
        query.OrderBy(p => p.CourseId);

    public override async Task<CoursePricing?> GetByIdAsync(
        long id, CancellationToken cancellationToken = default) =>
        await GetWithCourseAsync(id, cancellationToken);

    public async Task<IReadOnlyList<CoursePricing>> ListWithCourseAsync(
        CancellationToken cancellationToken = default) =>
        await ApplyDefaultSort(Query().Include(p => p.Course)).ToListAsync(cancellationToken);

    public async Task<CoursePricing?> GetWithCourseAsync(
        long courseId, CancellationToken cancellationToken = default) =>
        await Query()
            .Include(p => p.Course).ThenInclude(c => c!.Instructor).ThenInclude(u => u!.Profile)
            .FirstOrDefaultAsync(p => p.CourseId == courseId, cancellationToken);

    public async Task<IReadOnlyList<CoursePricing>> ListFreeAsync(
        CancellationToken cancellationToken = default) =>
        // EffectivePrice is [NotMapped], so the rule is restated in SQL terms.
        // A discount only applies when it is above zero and below the price, so
        // it can never bring the effective price to zero: free means Price == 0.
        await Query()
            .Include(p => p.Course)
            .Where(p => p.Price == 0m)
            .OrderBy(p => p.CourseId)
            .ToListAsync(cancellationToken);
}
