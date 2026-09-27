using SmartLearningPlatform.Models.Courses;

namespace SmartLearningPlatform.Repositories.Courses;

public interface ICourseDetailRepository : IRepository<CourseDetail, long>
{
    Task<IReadOnlyList<CourseDetail>> ListWithCourseAsync(CancellationToken cancellationToken = default);

    Task<CourseDetail?> GetWithCourseAsync(long courseId, CancellationToken cancellationToken = default);
}
