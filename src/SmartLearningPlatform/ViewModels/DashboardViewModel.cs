using SmartLearningPlatform.Models.Courses;
using SmartLearningPlatform.Models.Courses.Enums;
using SmartLearningPlatform.Models.Users.Enums;

namespace SmartLearningPlatform.ViewModels;

/// <summary>Counters and recent rows rendered on the home dashboard.</summary>
public class DashboardViewModel
{
    public int UserCount { get; init; }
    public int CourseCount { get; init; }
    public int RoleCount { get; init; }
    public int AuthorityCount { get; init; }

    public IReadOnlyDictionary<UserStatus, int> UsersByStatus { get; init; }
        = new Dictionary<UserStatus, int>();

    public IReadOnlyDictionary<CourseStatus, int> CoursesByStatus { get; init; }
        = new Dictionary<CourseStatus, int>();

    public IReadOnlyList<Course> RecentPublishedCourses { get; init; } = Array.Empty<Course>();

    /// <summary>True when the database is reachable; false puts a setup hint on screen.</summary>
    public bool DatabaseAvailable { get; init; } = true;

    public string? DatabaseError { get; init; }
}
