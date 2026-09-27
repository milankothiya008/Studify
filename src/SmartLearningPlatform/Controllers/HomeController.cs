using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartLearningPlatform.Models;
using SmartLearningPlatform.Repositories.Courses;
using SmartLearningPlatform.Repositories.Users;
using SmartLearningPlatform.ViewModels;

namespace SmartLearningPlatform.Controllers;

public class HomeController : Controller
{
    private readonly IUserRepository _users;
    private readonly ICourseRepository _courses;
    private readonly IRoleRepository _roles;
    private readonly IAuthorityRepository _authorities;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IUserRepository users,
        ICourseRepository courses,
        IRoleRepository roles,
        IAuthorityRepository authorities,
        ILogger<HomeController> logger)
    {
        _users = users;
        _courses = courses;
        _roles = roles;
        _authorities = authorities;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var model = new DashboardViewModel
            {
                UserCount = await _users.CountAsync(cancellationToken),
                CourseCount = await _courses.CountAsync(cancellationToken),
                RoleCount = await _roles.CountAsync(cancellationToken),
                AuthorityCount = await _authorities.CountAsync(cancellationToken),
                UsersByStatus = await _users.CountByStatusAsync(cancellationToken),
                CoursesByStatus = await _courses.CountByStatusAsync(cancellationToken),
                RecentPublishedCourses = await _courses.ListPublishedAsync(6, cancellationToken),
            };

            return View(model);
        }
        catch (Exception ex) when (ex is Npgsql.NpgsqlException or InvalidOperationException)
        {
            // A missing database is the single likeliest first-run problem, so
            // the dashboard explains it rather than showing a stack trace.
            _logger.LogWarning(ex, "Dashboard could not reach the database.");
            return View(new DashboardViewModel { DatabaseAvailable = false, DatabaseError = ex.Message });
        }
    }

    [AllowAnonymous]
    public IActionResult Privacy() => View();

    // Reached by UseExceptionHandler, which must work for anonymous requests too.
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
