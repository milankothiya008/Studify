using Microsoft.EntityFrameworkCore;
using SmartLearningPlatform.Data;
using SmartLearningPlatform.Models.Courses;
using SmartLearningPlatform.Models.Courses.Enums;

namespace SmartLearningPlatform.Repositories.Courses;

public class CourseRepository : Repository<Course, long>, ICourseRepository
{
    public CourseRepository(ApplicationDbContext context) : base(context) { }

    protected override IQueryable<Course> ApplyDefaultSort(IQueryable<Course> query) =>
        query.OrderByDescending(c => c.CreatedAt).ThenBy(c => c.Id);

    public override async Task<Course?> GetByIdAsync(
        long id, CancellationToken cancellationToken = default) =>
        await Query().Include(c => c.Instructor).ThenInclude(u => u!.Profile)
                     .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<PagedResult<Course>> SearchAsync(
        string? term, CourseStatus? status, CourseLevel? level, long? instructorId,
        int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = Query()
            .Include(c => c.Instructor).ThenInclude(u => u!.Profile)
            .Include(c => c.Pricing)
            .Include(c => c.Media)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(term))
        {
            var pattern = $"%{term.Trim()}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.Title, pattern) || EF.Functions.ILike(c.Subtitle, pattern));
        }

        if (status.HasValue) query = query.Where(c => c.CourseStatus == status.Value);
        if (level.HasValue) query = query.Where(c => c.CourseLevel == level.Value);
        if (instructorId.HasValue) query = query.Where(c => c.InstructorId == instructorId.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await ApplyDefaultSort(query)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Course>
        {
            Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total,
        };
    }

    public async Task<Course?> GetWithEverythingAsync(
        long id, CancellationToken cancellationToken = default) =>
        await Query()
            .Include(c => c.Instructor).ThenInclude(u => u!.Profile)
            .Include(c => c.Detail)
            .Include(c => c.Media)
            .Include(c => c.Pricing)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Course>> ListWithRelationsAsync(
        CancellationToken cancellationToken = default) =>
        await ApplyDefaultSort(
                Query()
                    .Include(c => c.Instructor).ThenInclude(u => u!.Profile)
                    .Include(c => c.Detail)
                    .Include(c => c.Media)
                    .Include(c => c.Pricing))
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Course>> ListPublishedAsync(
        int take = 6, CancellationToken cancellationToken = default) =>
        await Query()
            .Where(c => c.CourseStatus == CourseStatus.Published)
            .Include(c => c.Instructor).ThenInclude(u => u!.Profile)
            .Include(c => c.Media)
            .Include(c => c.Pricing)
            .OrderByDescending(c => c.CreatedAt)
            .Take(Math.Clamp(take, 1, 50))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<CourseStatus, int>> CountByStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await Query()
            .GroupBy(c => c.CourseStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Status, r => r.Count);
    }

    public async Task<IReadOnlyList<Course>> ListWithoutDetailAsync(
        long? includeCourseId = null, CancellationToken cancellationToken = default) =>
        await Query()
            .Where(c => c.Detail == null || (includeCourseId != null && c.Id == includeCourseId))
            .OrderBy(c => c.Title)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Course>> ListWithoutMediaAsync(
        long? includeCourseId = null, CancellationToken cancellationToken = default) =>
        await Query()
            .Where(c => c.Media == null || (includeCourseId != null && c.Id == includeCourseId))
            .OrderBy(c => c.Title)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Course>> ListWithoutPricingAsync(
        long? includeCourseId = null, CancellationToken cancellationToken = default) =>
        await Query()
            .Where(c => c.Pricing == null || (includeCourseId != null && c.Id == includeCourseId))
            .OrderBy(c => c.Title)
            .ToListAsync(cancellationToken);
}
