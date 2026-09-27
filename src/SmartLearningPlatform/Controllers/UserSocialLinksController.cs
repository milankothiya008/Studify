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

[Authorize(Policy = Authorities.UserRead)]
public class UserSocialLinksController : Controller
{
    private readonly IUserSocialLinkRepository _links;
    private readonly IUserRepository _users;

    public UserSocialLinksController(IUserSocialLinkRepository links, IUserRepository users)
    {
        _links = links;
        _users = users;
    }

    // GET: /UserSocialLinks
    public async Task<IActionResult> Index(long? userId, CancellationToken cancellationToken)
    {
        ViewData["FilterUserId"] = userId;
        await PopulateUserListAsync(userId, cancellationToken);
        return View(await _links.ListWithUserAsync(userId, cancellationToken));
    }

    // GET: /UserSocialLinks/Details/5
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        var link = await _links.GetWithUserAsync(id, cancellationToken);
        return link is null ? NotFound() : View(link);
    }

    // GET: /UserSocialLinks/Create
    [Authorize(Policy = Authorities.UserWrite)]
    public async Task<IActionResult> Create(long? userId, CancellationToken cancellationToken)
    {
        await PopulateUserListAsync(userId, cancellationToken);
        return View(new UserSocialLink { UserId = userId ?? 0 });
    }

    // POST: /UserSocialLinks/Create
    [Authorize(Policy = Authorities.UserWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("UserId,Platform,Url")] UserSocialLink link, CancellationToken cancellationToken)
    {
        await ValidateAsync(link, null, cancellationToken);

        if (!ModelState.IsValid)
        {
            await PopulateUserListAsync(link.UserId, cancellationToken);
            return View(link);
        }

        await _links.AddAsync(link, cancellationToken);

        TempData["Success"] = $"{link.Platform} link added.";
        return RedirectToAction(nameof(Index), new { userId = link.UserId });
    }

    // GET: /UserSocialLinks/Edit/5
    [Authorize(Policy = Authorities.UserWrite)]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var link = await _links.GetWithUserAsync(id, cancellationToken);
        if (link is null) return NotFound();

        await PopulateUserListAsync(link.UserId, cancellationToken);
        return View(link);
    }

    // POST: /UserSocialLinks/Edit/5
    [Authorize(Policy = Authorities.UserWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        long id, [Bind("Id,UserId,Platform,Url")] UserSocialLink link,
        CancellationToken cancellationToken)
    {
        if (id != link.Id) return BadRequest();

        await ValidateAsync(link, id, cancellationToken);

        if (!ModelState.IsValid)
        {
            await PopulateUserListAsync(link.UserId, cancellationToken);
            return View(link);
        }

        if (!await _links.ExistsAsync(id, cancellationToken)) return NotFound();

        await _links.UpdateAsync(link, cancellationToken);

        TempData["Success"] = $"{link.Platform} link updated.";
        return RedirectToAction(nameof(Index), new { userId = link.UserId });
    }

    // GET: /UserSocialLinks/Delete/5
    [Authorize(Policy = Authorities.UserDelete)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var link = await _links.GetWithUserAsync(id, cancellationToken);
        return link is null ? NotFound() : View(link);
    }

    // POST: /UserSocialLinks/Delete/5
    [Authorize(Policy = Authorities.UserDelete)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id, CancellationToken cancellationToken)
    {
        if (!await _links.DeleteAsync(id, cancellationToken)) return NotFound();

        TempData["Success"] = "Social link removed.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Enforces the one-link-per-platform-per-user rule in the form, so the user
    /// sees a field error instead of a unique-constraint violation.
    /// </summary>
    private async Task ValidateAsync(
        UserSocialLink link, long? excludeId, CancellationToken cancellationToken)
    {
        if (link.UserId == 0)
        {
            ModelState.AddModelError(nameof(link.UserId), "Select a user.");
            return;
        }

        if (await _links.LinkExistsAsync(link.UserId, link.Platform, excludeId, cancellationToken))
        {
            ModelState.AddModelError(
                nameof(link.Platform), "That user already has a link for this platform.");
        }
    }

    private async Task PopulateUserListAsync(long? selectedUserId, CancellationToken cancellationToken)
    {
        var users = await _users.ListWithProfileAsync(cancellationToken);
        ViewData["UserList"] = new SelectList(users, nameof(UserEntity.Id), nameof(UserEntity.Email), selectedUserId);
    }
}
