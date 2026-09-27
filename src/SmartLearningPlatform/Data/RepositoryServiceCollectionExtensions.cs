using SmartLearningPlatform.Repositories.Courses;
using SmartLearningPlatform.Repositories.Users;

namespace SmartLearningPlatform.Data;

/// <summary>
/// Registers one repository per entity. Spring found these by scanning for
/// <c>@Repository</c>; here they are listed explicitly, which keeps the set
/// visible and startup free of reflection.
/// </summary>
public static class RepositoryServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationRepositories(this IServiceCollection services)
    {
        // ─── User domain ───
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IUserSocialLinkRepository, UserSocialLinkRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IAuthorityRepository, AuthorityRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IRoleAuthorityRepository, RoleAuthorityRepository>();

        // ─── Course domain ───
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ICourseDetailRepository, CourseDetailRepository>();
        services.AddScoped<ICourseMediaRepository, CourseMediaRepository>();
        services.AddScoped<ICoursePricingRepository, CoursePricingRepository>();

        return services;
    }
}
