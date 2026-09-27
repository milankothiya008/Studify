using Microsoft.EntityFrameworkCore;
using SmartLearningPlatform.Data;
using SmartLearningPlatform.Models.Courses;

namespace SmartLearningPlatform.Repositories.Courses;

public class CourseMediaRepository : Repository<CourseMedia, long>, ICourseMediaRepository
{
    public CourseMediaRepository(ApplicationDbContext context) : base(context) { }

    protected override IQueryable<CourseMedia> ApplyDefaultSort(IQueryable<CourseMedia> query) =>
        query.OrderBy(m => m.CourseId);

    public override async Task<CourseMedia?> GetByIdAsync(
        long id, CancellationToken cancellationToken = default) =>
        await GetWithCourseAsync(id, cancellationToken);

    public async Task<IReadOnlyList<CourseMedia>> ListWithCourseAsync(
        CancellationToken cancellationToken = default) =>
        await ApplyDefaultSort(Query().Include(m => m.Course)).ToListAsync(cancellationToken);

    public async Task<CourseMedia?> GetWithCourseAsync(
        long courseId, CancellationToken cancellationToken = default) =>
        await Query()
            .Include(m => m.Course).ThenInclude(c => c!.Instructor).ThenInclude(u => u!.Profile)
            .FirstOrDefaultAsync(m => m.CourseId == courseId, cancellationToken);
}
