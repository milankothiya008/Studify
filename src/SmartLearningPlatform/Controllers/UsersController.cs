using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using SmartLearningPlatform.Models.Users.Enums;
using SmartLearningPlatform.Models.Users;
using SmartLearningPlatform.Repositories.Users;
using SmartLearningPlatform.Security;
using SmartLearningPlatform.ViewModels;

namespace SmartLearningPlatform.Controllers;

[Authorize(Policy = Authorities.UserRead)]
public class UsersController : Controller
{
    private const int PageSize = 15;

    private readonly IUserRepository _users;
    private readonly UserManager<User> _userManager;

    public UsersController(IUserRepository users, UserManager<User> userManager)
    {
        _users = users;
        _userManager = userManager;
    }

    // GET: /Users
    public async Task<IActionResult> Index(
        string? q, UserStatus? status, int page = 1, CancellationToken cancellationToken = default)
    {
        ViewData["Query"] = q;
        ViewData["Status"] = status;
        ViewData["StatusList"] = new SelectList(
            Enum.GetValues<UserStatus>().Select(s => new { Value = s, Text = s.ToString() }),
            "Value", "Text", status);

        var model = await _users.SearchAsync(q, status, page, PageSize, cancellationToken);
        return View(model);
    }

    // GET: /Users/Details/5
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        var user = await _users.GetWithEverythingAsync(id, cancellationToken);
        return user is null ? NotFound() : View(user);
    }

    // GET: /Users/Create
    [Authorize(Policy = Authorities.UserWrite)]
    public IActionResult Create() => View(new UserFormViewModel());

    // POST: /Users/Create
    [Authorize(Policy = Authorities.UserWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel form, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.Password))
        {
            ModelState.AddModelError(nameof(form.Password), "A password is required for a new user.");
        }

        if (await _users.EmailExistsAsync(form.Email, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(form.Email), "That email address is already registered.");
        }

        if (!ModelState.IsValid) return View(form);

        var user = new User();
        form.ApplyTo(user);
        user.PasswordChangedAt = DateTime.UtcNow;

        // UserManager hashes the password, stamps the security token, enables
        // lockout and writes the normalised lookup columns — the bookkeeping the
        // controller used to do by hand with IPasswordHasher.
        var created = await _userManager.CreateAsync(user, form.Password!);
        if (!created.Succeeded) return ViewWithErrors(created, form);

        TempData["Success"] = $"User {user.Email} created.";
        return RedirectToAction(nameof(Details), new { id = user.Id });
    }

    // GET: /Users/Edit/5
    [Authorize(Policy = Authorities.UserWrite)]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);
        return user is null ? NotFound() : View(UserFormViewModel.FromEntity(user));
    }

    // POST: /Users/Edit/5
    [Authorize(Policy = Authorities.UserWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        long id, UserFormViewModel form, CancellationToken cancellationToken)
    {
        if (id != form.Id) return BadRequest();

        if (await _users.EmailExistsAsync(form.Email, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(form.Email), "That email address is already registered.");
        }

        if (!ModelState.IsValid) return View(form);

        var user = await _users.GetForUpdateAsync(id, cancellationToken);
        if (user is null) return NotFound();

        form.ApplyTo(user);

        // A blank password box means "leave the existing password alone".
        if (!string.IsNullOrWhiteSpace(form.Password))
        {
            user.PasswordChangedAt = DateTime.UtcNow;

            // An administrator setting someone else's password is a reset, so it
            // goes through the reset path: it validates against the configured
            // password rules and rolls the security stamp, which signs the user
            // out of any session opened with the old password.
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var reset = await _userManager.ResetPasswordAsync(user, token, form.Password);
            if (!reset.Succeeded) return ViewWithErrors(reset, form);
        }

        var updated = await _userManager.UpdateAsync(user);
        if (!updated.Succeeded) return ViewWithErrors(updated, form);

        TempData["Success"] = $"User {user.Email} updated.";
        return RedirectToAction(nameof(Details), new { id = user.Id });
    }

    // GET: /Users/Delete/5
    [Authorize(Policy = Authorities.UserDelete)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var user = await _users.GetWithEverythingAsync(id, cancellationToken);
        return user is null ? NotFound() : View(user);
    }

    // POST: /Users/Delete/5
    [Authorize(Policy = Authorities.UserDelete)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id, CancellationToken cancellationToken)
    {
        // Soft delete: the repository stamps deleted_at, so audit trails and any
        // course this account authored keep resolving.
        var deleted = await _users.DeleteAsync(id, cancellationToken);
        if (!deleted) return NotFound();

        TempData["Success"] = "User retired.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Re-renders the form with Identity's complaints attached. They are model
    /// errors like any other — a password that fails the configured rules, or an
    /// email Identity considers taken.
    /// </summary>
    private IActionResult ViewWithErrors(IdentityResult result, UserFormViewModel form)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(form);
    }
}
