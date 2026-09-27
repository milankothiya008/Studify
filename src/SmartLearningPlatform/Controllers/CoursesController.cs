using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using SmartLearningPlatform.Models.Courses.Enums;
using SmartLearningPlatform.Models.Courses;
using SmartLearningPlatform.Models.Users;
using SmartLearningPlatform.Security;
// Controller.User is a ClaimsPrincipal and shadows the entity type inside
// a controller, so the entity is referred to through an explicit alias.
using UserEntity = SmartLearningPlatform.Models.Users.User;
using SmartLearningPlatform.Repositories.Courses;
using SmartLearningPlatform.Repositories.Users;

namespace SmartLearningPlatform.Controllers;

[Authorize(Policy = Authorities.CourseRead)]
public class CoursesController : Controller
{
    private const int PageSize = 12;

    private readonly ICourseRepository _courses;
    private readonly IUserRepository _users;

    public CoursesController(ICourseRepository courses, IUserRepository users)
    {
        _courses = courses;
        _users = users;
    }

    // GET: /Courses
    public async Task<IActionResult> Index(
        string? q, CourseStatus? status, CourseLevel? level, long? instructorId,
        int page = 1, CancellationToken cancellationToken = default)
    {
        ViewData["Query"] = q;
        ViewData["Status"] = status;
        ViewData["Level"] = level;
        ViewData["InstructorId"] = instructorId;
        await PopulateInstructorListAsync(instructorId, cancellationToken);

        var model = await _courses.SearchAsync(
            q, status, level, instructorId, page, PageSize, cancellationToken);

        return View(model);
    }

    // GET: /Courses/Details/5
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        var course = await _courses.GetWithEverythingAsync(id, cancellationToken);
        return course is null ? NotFound() : View(course);
    }

    // GET: /Courses/Create
    [Authorize(Policy = Authorities.CourseWrite)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await PopulateInstructorListAsync(null, cancellationToken);
        return View(new Course());
    }

    // POST: /Courses/Create
    [Authorize(Policy = Authorities.CourseWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("InstructorId,Title,Subtitle,CourseLevel,CourseStatus")] Course course,
        CancellationToken cancellationToken)
    {
        if (course.InstructorId == 0)
        {
            ModelState.AddModelError(nameof(course.InstructorId), "Select an instructor.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateInstructorListAsync(course.InstructorId, cancellationToken);
            return View(course);
        }

        await _courses.AddAsync(course, cancellationToken);

        TempData["Success"] =
            $"Course \"{course.Title}\" created. Add its details, media and pricing next.";
        return RedirectToAction(nameof(Details), new { id = course.Id });
    }

    // GET: /Courses/Edit/5
    [Authorize(Policy = Authorities.CourseWrite)]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var course = await _courses.GetByIdAsync(id, cancellationToken);
        if (course is null) return NotFound();

        await PopulateInstructorListAsync(course.InstructorId, cancellationToken);
        return View(course);
    }

    // POST: /Courses/Edit/5
    [Authorize(Policy = Authorities.CourseWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        long id,
        [Bind("Id,InstructorId,Title,Subtitle,CourseLevel,CourseStatus")] Course course,
        CancellationToken cancellationToken)
    {
        if (id != course.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateInstructorListAsync(course.InstructorId, cancellationToken);
            return View(course);
        }

        if (!await _courses.ExistsAsync(id, cancellationToken)) return NotFound();

        await _courses.UpdateAsync(course, cancellationToken);

        TempData["Success"] = $"Course \"{course.Title}\" updated.";
        return RedirectToAction(nameof(Details), new { id = course.Id });
    }

    // GET: /Courses/Delete/5
    [Authorize(Policy = Authorities.CourseDelete)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var course = await _courses.GetWithEverythingAsync(id, cancellationToken);
        return course is null ? NotFound() : View(course);
    }

    // POST: /Courses/Delete/5
    [Authorize(Policy = Authorities.CourseDelete)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id, CancellationToken cancellationToken)
    {
        // Soft delete — the satellites stay on disk and come back with the course.
        if (!await _courses.DeleteAsync(id, cancellationToken)) return NotFound();

        TempData["Success"] = "Course retired.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Moves a course along the editorial workflow from the Details page.
    /// </summary>
    [Authorize(Policy = Authorities.CoursePublish)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(
        long id, CourseStatus status, CancellationToken cancellationToken)
    {
        var course = await _courses.GetWithEverythingAsync(id, cancellationToken);
        if (course is null) return NotFound();

        // Matches the Java workflow: a course cannot leave Draft until it has
        // details, media and pricing.
        if (status != CourseStatus.Draft && !course.IsReadyForReview)
        {
            TempData["Error"] =
                "Add details, media and pricing before moving this course out of Draft.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var tracked = await _courses.GetForUpdateAsync(id, cancellationToken);
        if (tracked is null) return NotFound();

        tracked.CourseStatus = status;
        await _courses.UpdateAsync(tracked, cancellationToken);

        TempData["Success"] = $"Course moved to {status}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateInstructorListAsync(
        long? selectedId, CancellationToken cancellationToken)
    {
        var users = await _users.ListWithProfileAsync(cancellationToken);
        ViewData["InstructorList"] =
            new SelectList(users, nameof(UserEntity.Id), nameof(UserEntity.Email), selectedId);
    }
}
