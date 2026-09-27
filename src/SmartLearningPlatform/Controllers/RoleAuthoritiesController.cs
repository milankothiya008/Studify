using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using SmartLearningPlatform.Models.Users;
using SmartLearningPlatform.Repositories.Users;
using SmartLearningPlatform.Security;

namespace SmartLearningPlatform.Controllers;

/// <summary>Grants authorities to roles — the <c>role_authority</c> join table.</summary>
[Authorize(Policy = Authorities.RoleRead)]
public class RoleAuthoritiesController : Controller
{
    private readonly IRoleAuthorityRepository _roleAuthorities;
    private readonly IRoleRepository _roles;
    private readonly IAuthorityRepository _authorities;

    public RoleAuthoritiesController(
        IRoleAuthorityRepository roleAuthorities,
        IRoleRepository roles,
        IAuthorityRepository authorities)
    {
        _roleAuthorities = roleAuthorities;
        _roles = roles;
        _authorities = authorities;
    }

    // GET: /RoleAuthorities
    public async Task<IActionResult> Index(
        long? roleId, long? authorityId, CancellationToken cancellationToken)
    {
        ViewData["FilterRoleId"] = roleId;
        ViewData["FilterAuthorityId"] = authorityId;
        await PopulatePickersAsync(roleId, authorityId, cancellationToken);

        return View(await _roleAuthorities.ListWithRelationsAsync(roleId, authorityId, cancellationToken));
    }

    // GET: /RoleAuthorities/Details/5
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        var grant = await _roleAuthorities.GetWithRelationsAsync(id, cancellationToken);
        return grant is null ? NotFound() : View(grant);
    }

    // GET: /RoleAuthorities/Create
    [Authorize(Policy = Authorities.RoleWrite)]
    public async Task<IActionResult> Create(
        long? roleId, long? authorityId, CancellationToken cancellationToken)
    {
        await PopulatePickersAsync(roleId, authorityId, cancellationToken);
        return View(new RoleAuthority { RoleId = roleId ?? 0, AuthorityId = authorityId ?? 0 });
    }

    // POST: /RoleAuthorities/Create
    [Authorize(Policy = Authorities.RoleWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("RoleId,AuthorityId")] RoleAuthority grant, CancellationToken cancellationToken)
    {
        await ValidateAsync(grant, null, cancellationToken);

        if (!ModelState.IsValid)
        {
            await PopulatePickersAsync(grant.RoleId, grant.AuthorityId, cancellationToken);
            return View(grant);
        }

        await _roleAuthorities.AddAsync(grant, cancellationToken);

        TempData["Success"] = "Authority granted.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /RoleAuthorities/Edit/5
    [Authorize(Policy = Authorities.RoleWrite)]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var grant = await _roleAuthorities.GetWithRelationsAsync(id, cancellationToken);
        if (grant is null) return NotFound();

        await PopulatePickersAsync(grant.RoleId, grant.AuthorityId, cancellationToken);
        return View(grant);
    }

    // POST: /RoleAuthorities/Edit/5
    [Authorize(Policy = Authorities.RoleWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        long id, [Bind("Id,RoleId,AuthorityId")] RoleAuthority grant,
        CancellationToken cancellationToken)
    {
        if (id != grant.Id) return BadRequest();

        await ValidateAsync(grant, id, cancellationToken);

        if (!ModelState.IsValid)
        {
            await PopulatePickersAsync(grant.RoleId, grant.AuthorityId, cancellationToken);
            return View(grant);
        }

        if (!await _roleAuthorities.ExistsAsync(id, cancellationToken)) return NotFound();

        await _roleAuthorities.UpdateAsync(grant, cancellationToken);

        TempData["Success"] = "Grant updated.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /RoleAuthorities/Delete/5
    [Authorize(Policy = Authorities.RoleWrite)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var grant = await _roleAuthorities.GetWithRelationsAsync(id, cancellationToken);
        return grant is null ? NotFound() : View(grant);
    }

    // POST: /RoleAuthorities/Delete/5
    [Authorize(Policy = Authorities.RoleWrite)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id, CancellationToken cancellationToken)
    {
        if (!await _roleAuthorities.DeleteAsync(id, cancellationToken)) return NotFound();

        TempData["Success"] = "Authority revoked from role.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Surfaces the <c>uk_role_authority</c> constraint as a field error.</summary>
    private async Task ValidateAsync(
        RoleAuthority grant, long? excludeId, CancellationToken cancellationToken)
    {
        if (grant.RoleId == 0) ModelState.AddModelError(nameof(grant.RoleId), "Select a role.");
        if (grant.AuthorityId == 0)
        {
            ModelState.AddModelError(nameof(grant.AuthorityId), "Select an authority.");
        }

        if (!ModelState.IsValid) return;

        if (await _roleAuthorities.GrantExistsAsync(
                grant.RoleId, grant.AuthorityId, excludeId, cancellationToken))
        {
            ModelState.AddModelError(
                nameof(grant.AuthorityId), "That role already has this authority.");
        }
    }

    private async Task PopulatePickersAsync(
        long? selectedRoleId, long? selectedAuthorityId, CancellationToken cancellationToken)
    {
        var roles = await _roles.ListAsync(cancellationToken);
        var authorities = await _authorities.ListAsync(cancellationToken);

        ViewData["RoleList"] = new SelectList(roles, nameof(Role.Id), nameof(Role.Name), selectedRoleId);
        ViewData["AuthorityList"] =
            new SelectList(authorities, nameof(Authority.Id), nameof(Authority.Name), selectedAuthorityId);
    }
}
