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
public class UserProfilesController : Controller
{
    private readonly IUserProfileRepository _profiles;

    public UserProfilesController(IUserProfileRepository profiles) => _profiles = profiles;

    // GET: /UserProfiles
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _profiles.ListWithUserAsync(cancellationToken));

    // GET: /UserProfiles/Details/5
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetWithUserAsync(id, cancellationToken);
        return profile is null ? NotFound() : View(profile);
    }

    // GET: /UserProfiles/Create
    [Authorize(Policy = Authorities.UserWrite)]
    public async Task<IActionResult> Create(long? userId, CancellationToken cancellationToken)
    {
        await PopulateUserListAsync(userId, cancellationToken);
        return View(new UserProfile { UserId = userId ?? 0 });
    }

    // POST: /UserProfiles/Create
    [Authorize(Policy = Authorities.UserWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("UserId,FirstName,LastName,AboutMe,EducationLevel,Profession,Gender,DateOfBirth,"
              + "HomeAddress,WorkAddress,InstituteName,ProfilePictureUrl")]
        UserProfile profile,
        CancellationToken cancellationToken)
    {
        if (profile.UserId == 0)
        {
            ModelState.AddModelError(nameof(profile.UserId), "Select the user this profile belongs to.");
        }
        else if (await _profiles.ExistsAsync(profile.UserId, cancellationToken))
        {
            ModelState.AddModelError(nameof(profile.UserId), "That user already has a profile.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateUserListAsync(profile.UserId, cancellationToken);
            return View(profile);
        }

        await _profiles.AddAsync(profile, cancellationToken);

        TempData["Success"] = $"Profile for {profile.FullName} created.";
        return RedirectToAction(nameof(Details), new { id = profile.UserId });
    }

    // GET: /UserProfiles/Edit/5
    [Authorize(Policy = Authorities.UserWrite)]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetWithUserAsync(id, cancellationToken);
        if (profile is null) return NotFound();

        await PopulateUserListAsync(id, cancellationToken);
        return View(profile);
    }

    // POST: /UserProfiles/Edit/5
    [Authorize(Policy = Authorities.UserWrite)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        long id,
        [Bind("UserId,FirstName,LastName,AboutMe,EducationLevel,Profession,Gender,DateOfBirth,"
              + "HomeAddress,WorkAddress,InstituteName,ProfilePictureUrl")]
        UserProfile profile,
        CancellationToken cancellationToken)
    {
        if (id != profile.UserId) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateUserListAsync(id, cancellationToken);
            return View(profile);
        }

        if (!await _profiles.ExistsAsync(id, cancellationToken)) return NotFound();

        await _profiles.UpdateAsync(profile, cancellationToken);

        TempData["Success"] = $"Profile for {profile.FullName} updated.";
        return RedirectToAction(nameof(Details), new { id = profile.UserId });
    }

    // GET: /UserProfiles/Delete/5
    [Authorize(Policy = Authorities.UserDelete)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetWithUserAsync(id, cancellationToken);
        return profile is null ? NotFound() : View(profile);
    }

    // POST: /UserProfiles/Delete/5
    [Authorize(Policy = Authorities.UserDelete)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id, CancellationToken cancellationToken)
    {
        // No deleted_at column here — the profile row really is removed, and the
        // owning user keeps existing without one.
        if (!await _profiles.DeleteAsync(id, cancellationToken)) return NotFound();

        TempData["Success"] = "Profile deleted.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Fills the user picker with accounts that have no profile yet, plus the
    /// one being edited so its own row stays selectable.
    /// </summary>
    private async Task PopulateUserListAsync(long? selectedUserId, CancellationToken cancellationToken)
    {
        var users = await _profiles.ListUsersWithoutProfileAsync(selectedUserId, cancellationToken);
        ViewData["UserList"] = new SelectList(users, nameof(UserEntity.Id), nameof(UserEntity.Email), selectedUserId);
    }
}
