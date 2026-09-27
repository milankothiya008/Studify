using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using SmartLearningPlatform.Models.Courses;
using SmartLearningPlatform.Repositories.Courses;
using SmartLearningPlatform.Security;

namespace SmartLearningPlatform.Controllers;

/// <summary>Price and discount for a course — the <c>course_pricing</c> satellite.</summary>
[Authorize(Policy = Authorities.CourseRead)]
public class CoursePricingController : Controller
{
    private readonly ICoursePricingRepository _pricing;
    private readonly ICourseRepository _courses;

    public CoursePricingController(ICoursePricingRepository pricing, ICourseRepository courses)
    {
        _pricing = pricing;
        _courses = courses;
    }

    // GET: /CoursePricing
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _pricing.ListWithCourseAsync(cancellationToken));

    // GET: /CoursePricing/Details/5
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        var pricing = await _pricing.GetWithCourseAsync(id, cancellationToken);
        return pricing is null ? NotFound() : View(pricing);
    }

    // GET: /CoursePricing/Create
    [Authorize(Policy = Authorities.CourseWrite)]
    public async Task<IActionResult> Create(long? courseId, CancellationToken cancellationToken)
    {
        await PopulateCourseListAsync(courseId, cancellationToken);
        return View(new CoursePricing { CourseId = courseId ?? 0 });
    }

    // POST: /CoursePricing/Create
    [Authorize(Policy = Authorities.CourseWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("CourseId,Price,DiscountPrice,Currency")] CoursePricing pricing,
        CancellationToken cancellationToken)
    {
        if (pricing.CourseId == 0)
        {
            ModelState.AddModelError(nameof(pricing.CourseId), "Select a course.");
        }
        else if (await _pricing.ExistsAsync(pricing.CourseId, cancellationToken))
        {
            ModelState.AddModelError(nameof(pricing.CourseId), "That course already has a pricing row.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateCourseListAsync(pricing.CourseId, cancellationToken);
            return View(pricing);
        }

        await _pricing.AddAsync(pricing, cancellationToken);

        TempData["Success"] = "Course pricing saved.";
        return RedirectToAction("Details", "Courses", new { id = pricing.CourseId });
    }

    // GET: /CoursePricing/Edit/5
    [Authorize(Policy = Authorities.CourseWrite)]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var pricing = await _pricing.GetWithCourseAsync(id, cancellationToken);
        if (pricing is null) return NotFound();

        await PopulateCourseListAsync(id, cancellationToken);
        return View(pricing);
    }

    // POST: /CoursePricing/Edit/5
    [Authorize(Policy = Authorities.CourseWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        long id,
        [Bind("CourseId,Price,DiscountPrice,Currency")] CoursePricing pricing,
        CancellationToken cancellationToken)
    {
        if (id != pricing.CourseId) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateCourseListAsync(id, cancellationToken);
            return View(pricing);
        }

        if (!await _pricing.ExistsAsync(id, cancellationToken)) return NotFound();

        await _pricing.UpdateAsync(pricing, cancellationToken);

        TempData["Success"] = "Course pricing updated.";
        return RedirectToAction("Details", "Courses", new { id = pricing.CourseId });
    }

    // GET: /CoursePricing/Delete/5
    [Authorize(Policy = Authorities.CourseDelete)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var pricing = await _pricing.GetWithCourseAsync(id, cancellationToken);
        return pricing is null ? NotFound() : View(pricing);
    }

    // POST: /CoursePricing/Delete/5
    [Authorize(Policy = Authorities.CourseDelete)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id, CancellationToken cancellationToken)
    {
        if (!await _pricing.DeleteAsync(id, cancellationToken)) return NotFound();

        TempData["Success"] = "Course pricing deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCourseListAsync(long? selectedId, CancellationToken cancellationToken)
    {
        var courses = await _courses.ListWithoutPricingAsync(selectedId, cancellationToken);
        ViewData["CourseList"] =
            new SelectList(courses, nameof(Course.Id), nameof(Course.Title), selectedId);
    }
}
