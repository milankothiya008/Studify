using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using SmartLearningPlatform.Models.Courses;
using SmartLearningPlatform.Repositories.Courses;
using SmartLearningPlatform.Security;

namespace SmartLearningPlatform.Controllers;

/// <summary>Long-form copy for a course — the <c>course_details</c> satellite.</summary>
[Authorize(Policy = Authorities.CourseRead)]
public class CourseDetailsController : Controller
{
    private readonly ICourseDetailRepository _details;
    private readonly ICourseRepository _courses;

    public CourseDetailsController(ICourseDetailRepository details, ICourseRepository courses)
    {
        _details = details;
        _courses = courses;
    }

    // GET: /CourseDetails
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _details.ListWithCourseAsync(cancellationToken));

    // GET: /CourseDetails/Details/5
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        var detail = await _details.GetWithCourseAsync(id, cancellationToken);
        return detail is null ? NotFound() : View(detail);
    }

    // GET: /CourseDetails/Create
    [Authorize(Policy = Authorities.CourseWrite)]
    public async Task<IActionResult> Create(long? courseId, CancellationToken cancellationToken)
    {
        await PopulateCourseListAsync(courseId, cancellationToken);
        return View(new CourseDetail { CourseId = courseId ?? 0 });
    }

    // POST: /CourseDetails/Create
    [Authorize(Policy = Authorities.CourseWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("CourseId,Description,Requirement,LearningOutcome,HasCertificate,HasAssignment,"
              + "HasProject,HasQuiz")]
        CourseDetail detail,
        CancellationToken cancellationToken)
    {
        if (detail.CourseId == 0)
        {
            ModelState.AddModelError(nameof(detail.CourseId), "Select a course.");
        }
        else if (await _details.ExistsAsync(detail.CourseId, cancellationToken))
        {
            ModelState.AddModelError(nameof(detail.CourseId), "That course already has a details row.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateCourseListAsync(detail.CourseId, cancellationToken);
            return View(detail);
        }

        await _details.AddAsync(detail, cancellationToken);

        TempData["Success"] = "Course details saved.";
        return RedirectToAction("Details", "Courses", new { id = detail.CourseId });
    }

    // GET: /CourseDetails/Edit/5
    [Authorize(Policy = Authorities.CourseWrite)]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var detail = await _details.GetWithCourseAsync(id, cancellationToken);
        if (detail is null) return NotFound();

        await PopulateCourseListAsync(id, cancellationToken);
        return View(detail);
    }

    // POST: /CourseDetails/Edit/5
    [Authorize(Policy = Authorities.CourseWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        long id,
        [Bind("CourseId,Description,Requirement,LearningOutcome,HasCertificate,HasAssignment,"
              + "HasProject,HasQuiz")]
        CourseDetail detail,
        CancellationToken cancellationToken)
    {
        if (id != detail.CourseId) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateCourseListAsync(id, cancellationToken);
            return View(detail);
        }

        if (!await _details.ExistsAsync(id, cancellationToken)) return NotFound();

        await _details.UpdateAsync(detail, cancellationToken);

        TempData["Success"] = "Course details updated.";
        return RedirectToAction("Details", "Courses", new { id = detail.CourseId });
    }

    // GET: /CourseDetails/Delete/5
    [Authorize(Policy = Authorities.CourseDelete)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var detail = await _details.GetWithCourseAsync(id, cancellationToken);
        return detail is null ? NotFound() : View(detail);
    }

    // POST: /CourseDetails/Delete/5
    [Authorize(Policy = Authorities.CourseDelete)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id, CancellationToken cancellationToken)
    {
        if (!await _details.DeleteAsync(id, cancellationToken)) return NotFound();

        TempData["Success"] = "Course details deleted.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Courses without a details row, plus the one being edited.</summary>
    private async Task PopulateCourseListAsync(long? selectedId, CancellationToken cancellationToken)
    {
        var courses = await _courses.ListWithoutDetailAsync(selectedId, cancellationToken);
        ViewData["CourseList"] =
            new SelectList(courses, nameof(Course.Id), nameof(Course.Title), selectedId);
    }
}
