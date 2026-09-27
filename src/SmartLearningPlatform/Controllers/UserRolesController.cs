using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using SmartLearningPlatform.Models.Users;
using SmartLearningPlatform.Security;
// Controller.User is a ClaimsPrincipal and shadows the entity type inside
// a controller, so the entity is referred to through an explicit alias.
using UserEntity = SmartLearningPlatform.Models.Users.User;
using SmartLearningPlatform.Repositories.Users;

namespace SmartLearningPlatform.Controllers;

/// <summary>Assigns roles to users — the <c>user_roles</c> join table.</summary>
[Authorize(Policy = Authorities.UserRead)]
public class UserRolesController : Controller
{
    private readonly IUserRoleRepository _userRoles;
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;

    public UserRolesController(
        IUserRoleRepository userRoles, IUserRepository users, IRoleRepository roles)
    {
        _userRoles = userRoles;
        _users = users;
        _roles = roles;
    }

    // GET: /UserRoles
    public async Task<IActionResult> Index(
        long? userId, long? roleId, CancellationToken cancellationToken)
    {
        ViewData["FilterUserId"] = userId;
        ViewData["FilterRoleId"] = roleId;
        await PopulatePickersAsync(userId, roleId, cancellationToken);

        return View(await _userRoles.ListWithRelationsAsync(userId, roleId, cancellationToken));
    }

    // GET: /UserRoles/Details/5
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        var assignment = await _userRoles.GetWithRelationsAsync(id, cancellationToken);
        return assignment is null ? NotFound() : View(assignment);
    }

    // GET: /UserRoles/Create
    [Authorize(Policy = Authorities.RoleWrite)]
    public async Task<IActionResult> Create(
        long? userId, long? roleId, CancellationToken cancellationToken)
    {
        await PopulatePickersAsync(userId, roleId, cancellationToken);
        return View(new UserRole { UserId = userId ?? 0, RoleId = roleId ?? 0 });
    }

    // POST: /UserRoles/Create
    [Authorize(Policy = Authorities.RoleWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("UserId,RoleId")] UserRole assignment, CancellationToken cancellationToken)
    {
        await ValidateAsync(assignment, null, cancellationToken);

        if (!ModelState.IsValid)
        {
            await PopulatePickersAsync(assignment.UserId, assignment.RoleId, cancellationToken);
            return View(assignment);
        }

        await _userRoles.AddAsync(assignment, cancellationToken);

        TempData["Success"] = "Role assigned.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /UserRoles/Edit/5
    [Authorize(Policy = Authorities.RoleWrite)]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var assignment = await _userRoles.GetWithRelationsAsync(id, cancellationToken);
        if (assignment is null) return NotFound();

        await PopulatePickersAsync(assignment.UserId, assignment.RoleId, cancellationToken);
        return View(assignment);
    }

    // POST: /UserRoles/Edit/5
    [Authorize(Policy = Authorities.RoleWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        long id, [Bind("Id,UserId,RoleId")] UserRole assignment, CancellationToken cancellationToken)
    {
        if (id != assignment.Id) return BadRequest();

        await ValidateAsync(assignment, id, cancellationToken);

        if (!ModelState.IsValid)
        {
            await PopulatePickersAsync(assignment.UserId, assignment.RoleId, cancellationToken);
            return View(assignment);
        }

        if (!await _userRoles.ExistsAsync(id, cancellationToken)) return NotFound();

        await _userRoles.UpdateAsync(assignment, cancellationToken);

        TempData["Success"] = "Assignment updated.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /UserRoles/Delete/5
    [Authorize(Policy = Authorities.RoleWrite)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var assignment = await _userRoles.GetWithRelationsAsync(id, cancellationToken);
        return assignment is null ? NotFound() : View(assignment);
    }

    // POST: /UserRoles/Delete/5
    [Authorize(Policy = Authorities.RoleWrite)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id, CancellationToken cancellationToken)
    {
        // A join row carries no history worth keeping, so this is a real delete.
        if (!await _userRoles.DeleteAsync(id, cancellationToken)) return NotFound();

        TempData["Success"] = "Role unassigned.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Surfaces the <c>uk_user_role</c> constraint as a field error.</summary>
    private async Task ValidateAsync(
        UserRole assignment, long? excludeId, CancellationToken cancellationToken)
    {
        if (assignment.UserId == 0) ModelState.AddModelError(nameof(assignment.UserId), "Select a user.");
        if (assignment.RoleId == 0) ModelState.AddModelError(nameof(assignment.RoleId), "Select a role.");
        if (!ModelState.IsValid) return;

        if (await _userRoles.AssignmentExistsAsync(
                assignment.UserId, assignment.RoleId, excludeId, cancellationToken))
        {
            ModelState.AddModelError(
                nameof(assignment.RoleId), "That user already has this role.");
        }
    }

    private async Task PopulatePickersAsync(
        long? selectedUserId, long? selectedRoleId, CancellationToken cancellationToken)
    {
        var users = await _users.ListWithProfileAsync(cancellationToken);
        var roles = await _roles.ListAsync(cancellationToken);

        ViewData["UserList"] = new SelectList(users, nameof(UserEntity.Id), nameof(UserEntity.Email), selectedUserId);
        ViewData["RoleList"] = new SelectList(roles, nameof(Role.Id), nameof(Role.Name), selectedRoleId);
    }
}
