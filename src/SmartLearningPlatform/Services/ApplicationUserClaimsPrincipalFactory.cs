using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SmartLearningPlatform.Models.Users;
using SmartLearningPlatform.Repositories.Users;
using SmartLearningPlatform.Security;

namespace SmartLearningPlatform.Services;

/// <summary>
/// Adds the port's two domain-specific claims to the principal Identity builds.
/// </summary>
/// <remarks>
/// The base factory already issues the user id, the login handle, the email and
/// one <c>ClaimTypes.Role</c> claim per assigned role. What it cannot know about
/// is <c>Authority</c>: the fine-grained permissions each role grants through
/// <c>role_authority</c>. Flattening them into claims at sign-in is what lets
/// <c>[Authorize(Policy = "course:publish")]</c> mean what the Role &rarr;
/// authority screen says it means.
/// </remarks>
public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, Role>
{
    private readonly IUserRepository _users;

    public ApplicationUserClaimsPrincipalFactory(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IOptions<IdentityOptions> options,
        IUserRepository users)
        : base(userManager, roleManager, options)
    {
        _users = users;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        foreach (var authority in await _users.ListAuthorityNamesAsync(user.Id))
        {
            identity.AddClaim(new Claim(ApplicationClaimTypes.Authority, authority));
        }

        // The profile may not be loaded on the instance Identity handed us.
        var displayName = (await _users.GetByIdAsync(user.Id))?.DisplayName ?? user.DisplayName;
        identity.AddClaim(new Claim(ApplicationClaimTypes.DisplayName, displayName));

        return identity;
    }
}
