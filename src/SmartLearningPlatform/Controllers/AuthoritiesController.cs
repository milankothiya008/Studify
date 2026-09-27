using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartLearningPlatform.Models.Users;
using SmartLearningPlatform.Repositories.Users;
using SmartLearningPlatform.Security;

namespace SmartLearningPlatform.Controllers;

[Authorize(Policy = Authorities.RoleRead)]
public class AuthoritiesController : Controller
{
    private readonly IAuthorityRepository _authorities;

    public AuthoritiesController(IAuthorityRepository authorities) => _authorities = authorities;

    // GET: /Authorities
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _authorities.ListGroupedByResourceAsync(cancellationToken));

    // GET: /Authorities/Details/5
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        var authority = await _authorities.GetWithRolesAsync(id, cancellationToken);
        return authority is null ? NotFound() : View(authority);
    }

    // GET: /Authorities/Create
    [Authorize(Policy = Authorities.RoleWrite)]
    public IActionResult Create() => View(new Authority());

    // POST: /Authorities/Create
    [Authorize(Policy = Authorities.RoleWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,Description")] Authority authority, CancellationToken cancellationToken)
    {
        if (await _authorities.NameExistsAsync(authority.Name, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(authority.Name), "That authority already exists.");
        }

        if (!ModelState.IsValid) return View(authority);

        await _authorities.AddAsync(authority, cancellationToken);

        TempData["Success"] = $"Authority {authority.Name} created.";
        return RedirectToAction(nameof(Details), new { id = authority.Id });
    }

    // GET: /Authorities/Edit/5
    [Authorize(Policy = Authorities.RoleWrite)]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var authority = await _authorities.GetByIdAsync(id, cancellationToken);
        return authority is null ? NotFound() : View(authority);
    }

    // POST: /Authorities/Edit/5
    [Authorize(Policy = Authorities.RoleWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        long id, [Bind("Id,Name,Description")] Authority authority,
        CancellationToken cancellationToken)
    {
        if (id != authority.Id) return BadRequest();

        if (await _authorities.NameExistsAsync(authority.Name, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(authority.Name), "That authority already exists.");
        }

        if (!ModelState.IsValid) return View(authority);
        if (!await _authorities.ExistsAsync(id, cancellationToken)) return NotFound();

        await _authorities.UpdateAsync(authority, cancellationToken);

        TempData["Success"] = $"Authority {authority.Name} updated.";
        return RedirectToAction(nameof(Details), new { id = authority.Id });
    }

    // GET: /Authorities/Delete/5
    [Authorize(Policy = Authorities.RoleWrite)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var authority = await _authorities.GetWithRolesAsync(id, cancellationToken);
        return authority is null ? NotFound() : View(authority);
    }

    // POST: /Authorities/Delete/5
    [Authorize(Policy = Authorities.RoleWrite)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id, CancellationToken cancellationToken)
    {
        if (!await _authorities.DeleteAsync(id, cancellationToken)) return NotFound();

        TempData["Success"] = "Authority retired.";
        return RedirectToAction(nameof(Index));
    }
}
