using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using SmartLearningPlatform.Models.Courses;
using SmartLearningPlatform.Repositories.Courses;
using SmartLearningPlatform.Security;

namespace SmartLearningPlatform.Controllers;

/// <summary>Artwork and media for a course — the <c>course_media</c> satellite.</summary>
[Authorize(Policy = Authorities.CourseRead)]
public class CourseMediaController : Controller
{
    private readonly ICourseMediaRepository _media;
    private readonly ICourseRepository _courses;

    public CourseMediaController(ICourseMediaRepository media, ICourseRepository courses)
    {
        _media = media;
        _courses = courses;
    }

    // GET: /CourseMedia
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _media.ListWithCourseAsync(cancellationToken));

    // GET: /CourseMedia/Details/5
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        var media = await _media.GetWithCourseAsync(id, cancellationToken);
        return media is null ? NotFound() : View(media);
    }

    // GET: /CourseMedia/Create
    [Authorize(Policy = Authorities.CourseWrite)]
    public async Task<IActionResult> Create(long? courseId, CancellationToken cancellationToken)
    {
        await PopulateCourseListAsync(courseId, cancellationToken);
        return View(new CourseMedia { CourseId = courseId ?? 0 });
    }

    // POST: /CourseMedia/Create
    [Authorize(Policy = Authorities.CourseWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("CourseId,ThumbnailUrl,PromotionalLessonUrl,CertificateTemplateUrl")] CourseMedia media,
        CancellationToken cancellationToken)
    {
        if (media.CourseId == 0)
        {
            ModelState.AddModelError(nameof(media.CourseId), "Select a course.");
        }
        else if (await _media.ExistsAsync(media.CourseId, cancellationToken))
        {
            ModelState.AddModelError(nameof(media.CourseId), "That course already has a media row.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateCourseListAsync(media.CourseId, cancellationToken);
            return View(media);
        }

        await _media.AddAsync(media, cancellationToken);

        TempData["Success"] = "Course media saved.";
        return RedirectToAction("Details", "Courses", new { id = media.CourseId });
    }

    // GET: /CourseMedia/Edit/5
    [Authorize(Policy = Authorities.CourseWrite)]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var media = await _media.GetWithCourseAsync(id, cancellationToken);
        if (media is null) return NotFound();

        await PopulateCourseListAsync(id, cancellationToken);
        return View(media);
    }

    // POST: /CourseMedia/Edit/5
    [Authorize(Policy = Authorities.CourseWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        long id,
        [Bind("CourseId,ThumbnailUrl,PromotionalLessonUrl,CertificateTemplateUrl")] CourseMedia media,
        CancellationToken cancellationToken)
    {
        if (id != media.CourseId) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateCourseListAsync(id, cancellationToken);
            return View(media);
        }

        if (!await _media.ExistsAsync(id, cancellationToken)) return NotFound();

        await _media.UpdateAsync(media, cancellationToken);

        TempData["Success"] = "Course media updated.";
        return RedirectToAction("Details", "Courses", new { id = media.CourseId });
    }

    // GET: /CourseMedia/Delete/5
    [Authorize(Policy = Authorities.CourseDelete)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var media = await _media.GetWithCourseAsync(id, cancellationToken);
        return media is null ? NotFound() : View(media);
    }

    // POST: /CourseMedia/Delete/5
    [Authorize(Policy = Authorities.CourseDelete)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id, CancellationToken cancellationToken)
    {
        if (!await _media.DeleteAsync(id, cancellationToken)) return NotFound();

        TempData["Success"] = "Course media deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCourseListAsync(long? selectedId, CancellationToken cancellationToken)
    {
        var courses = await _courses.ListWithoutMediaAsync(selectedId, cancellationToken);
        ViewData["CourseList"] =
            new SelectList(courses, nameof(Course.Id), nameof(Course.Title), selectedId);
    }
}
