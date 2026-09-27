using SmartLearningPlatform.Models.Courses;
using SmartLearningPlatform.Models.Courses.Enums;

namespace SmartLearningPlatform.Repositories.Courses;

public interface ICourseRepository : IRepository<Course, long>
{
    Task<PagedResult<Course>> SearchAsync(
        string? term, CourseStatus? status, CourseLevel? level, long? instructorId,
        int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Course with instructor, detail, media and pricing all loaded.</summary>
    Task<Course?> GetWithEverythingAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Instructor plus the three satellites, for list rows.</summary>
    Task<IReadOnlyList<Course>> ListWithRelationsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Course>> ListPublishedAsync(
        int take = 6, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<CourseStatus, int>> CountByStatusAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Courses with no detail/media/pricing row yet — the satellite Create pickers.</summary>
    Task<IReadOnlyList<Course>> ListWithoutDetailAsync(
        long? includeCourseId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Course>> ListWithoutMediaAsync(
        long? includeCourseId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Course>> ListWithoutPricingAsync(
        long? includeCourseId = null, CancellationToken cancellationToken = default);
}
