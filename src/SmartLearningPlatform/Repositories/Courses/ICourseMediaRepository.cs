using SmartLearningPlatform.Models.Courses;

namespace SmartLearningPlatform.Repositories.Courses;

public interface ICourseMediaRepository : IRepository<CourseMedia, long>
{
    Task<IReadOnlyList<CourseMedia>> ListWithCourseAsync(CancellationToken cancellationToken = default);

    Task<CourseMedia?> GetWithCourseAsync(long courseId, CancellationToken cancellationToken = default);
}
