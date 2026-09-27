using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartLearningPlatform.Models.Users;
using SmartLearningPlatform.Repositories.Users;
using SmartLearningPlatform.Security;

namespace SmartLearningPlatform.Controllers;

[Authorize(Policy = Authorities.RoleRead)]
public class RolesController : Controller
{
    private readonly IRoleRepository _roles;
    private readonly RoleManager<Role> _roleManager;

    public RolesController(IRoleRepository roles, RoleManager<Role> roleManager)
    {
        _roles = roles;
        _roleManager = roleManager;
    }

    // GET: /Roles
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["UsageCounts"] = await _roles.GetUsageCountsAsync(cancellationToken);
        return View(await _roles.ListAsync(cancellationToken));
    }

    // GET: /Roles/Details/5
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        var role = await _roles.GetWithAuthoritiesAsync(id, cancellationToken);
        return role is null ? NotFound() : View(role);
    }

    // GET: /Roles/Create
    [Authorize(Policy = Authorities.RoleWrite)]
    public IActionResult Create() => View(new Role());

    // POST: /Roles/Create
    [Authorize(Policy = Authorities.RoleWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,Description")] Role role, CancellationToken cancellationToken)
    {
        if (await _roles.NameExistsAsync(role.Name, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(role.Name), "A role with that name already exists.");
        }

        if (!ModelState.IsValid) return View(role);

        // RoleManager, not the repository: a role is an Identity entity now, and
        // only RoleManager fills in normalized_name, which is what
        // UserManager.AddToRoleAsync and the uk_roles_normalized_name index use.
        var created = await _roleManager.CreateAsync(role);
        if (!created.Succeeded) return ViewWithErrors(created, role);

        TempData["Success"] = $"Role {role.Name} created.";
        return RedirectToAction(nameof(Details), new { id = role.Id });
    }

    // GET: /Roles/Edit/5
    [Authorize(Policy = Authorities.RoleWrite)]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var role = await _roles.GetByIdAsync(id, cancellationToken);
        return role is null ? NotFound() : View(role);
    }

    // POST: /Roles/Edit/5
    [Authorize(Policy = Authorities.RoleWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        long id, [Bind("Id,Name,Description")] Role role, CancellationToken cancellationToken)
    {
        if (id != role.Id) return BadRequest();

        if (await _roles.NameExistsAsync(role.Name, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(role.Name), "A role with that name already exists.");
        }

        if (!ModelState.IsValid) return View(role);

        // Load, then copy the two editable fields across. Saving the posted
        // instance directly would write its blank normalized_name and its
        // freshly minted concurrency stamp over the stored ones, and the stamp
        // is a concurrency token, so the update would match no row at all.
        var existing = await _roles.GetForUpdateAsync(id, cancellationToken);
        if (existing is null) return NotFound();

        existing.Name = role.Name;
        existing.Description = role.Description;

        var updated = await _roleManager.UpdateAsync(existing);
        if (!updated.Succeeded) return ViewWithErrors(updated, role);

        TempData["Success"] = $"Role {existing.Name} updated.";
        return RedirectToAction(nameof(Details), new { id = existing.Id });
    }

    // GET: /Roles/Delete/5
    [Authorize(Policy = Authorities.RoleWrite)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var role = await _roles.GetWithAuthoritiesAsync(id, cancellationToken);
        return role is null ? NotFound() : View(role);
    }

    // POST: /Roles/Delete/5
    [Authorize(Policy = Authorities.RoleWrite)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id, CancellationToken cancellationToken)
    {
        // Soft delete, so existing user_roles rows keep pointing somewhere real.
        if (!await _roles.DeleteAsync(id, cancellationToken)) return NotFound();

        TempData["Success"] = "Role retired.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Re-renders the form with Identity's complaints attached.</summary>
    private IActionResult ViewWithErrors(IdentityResult result, Role role)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(role);
    }
}
