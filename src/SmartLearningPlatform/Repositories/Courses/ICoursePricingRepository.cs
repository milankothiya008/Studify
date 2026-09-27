using SmartLearningPlatform.Models.Courses;

namespace SmartLearningPlatform.Repositories.Courses;

public interface ICoursePricingRepository : IRepository<CoursePricing, long>
{
    Task<IReadOnlyList<CoursePricing>> ListWithCourseAsync(CancellationToken cancellationToken = default);

    Task<CoursePricing?> GetWithCourseAsync(long courseId, CancellationToken cancellationToken = default);

    /// <summary>Rows whose effective price is zero, for a "free courses" view.</summary>
    Task<IReadOnlyList<CoursePricing>> ListFreeAsync(CancellationToken cancellationToken = default);
}
