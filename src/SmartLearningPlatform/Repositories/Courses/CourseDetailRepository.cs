using Microsoft.EntityFrameworkCore;
using SmartLearningPlatform.Data;
using SmartLearningPlatform.Models.Courses;

namespace SmartLearningPlatform.Repositories.Courses;

public class CourseDetailRepository : Repository<CourseDetail, long>, ICourseDetailRepository
{
    public CourseDetailRepository(ApplicationDbContext context) : base(context) { }

    protected override IQueryable<CourseDetail> ApplyDefaultSort(IQueryable<CourseDetail> query) =>
        query.OrderBy(d => d.CourseId);

    public override async Task<CourseDetail?> GetByIdAsync(
        long id, CancellationToken cancellationToken = default) =>
        await GetWithCourseAsync(id, cancellationToken);

    public async Task<IReadOnlyList<CourseDetail>> ListWithCourseAsync(
        CancellationToken cancellationToken = default) =>
        await ApplyDefaultSort(Query().Include(d => d.Course)).ToListAsync(cancellationToken);

    public async Task<CourseDetail?> GetWithCourseAsync(
        long courseId, CancellationToken cancellationToken = default) =>
        await Query()
            .Include(d => d.Course).ThenInclude(c => c!.Instructor).ThenInclude(u => u!.Profile)
            .FirstOrDefaultAsync(d => d.CourseId == courseId, cancellationToken);
}
