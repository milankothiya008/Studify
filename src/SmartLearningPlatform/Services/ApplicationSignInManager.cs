using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SmartLearningPlatform.Models.Users;
using SmartLearningPlatform.Models.Users.Enums;

namespace SmartLearningPlatform.Services;

/// <summary>
/// <see cref="SignInManager{TUser}"/> with the port's own rule about who may
/// sign in.
/// </summary>
/// <remarks>
/// Identity's default answer is "any user whose email/account is confirmed".
/// The domain already had an answer — <c>UserStatus.IsLoginAllowed()</c> plus the
/// <c>Enabled</c> flag and the soft-delete stamp — so that is what this returns,
/// and Identity's password, lockout and cookie machinery runs on top of it
/// unchanged.
///
/// The live-lockout half of <see cref="User.CanSignIn"/> is deliberately left
/// out here: Identity checks lockout immediately afterwards, and letting it do
/// so is what tells a locked-out user they are locked out rather than simply
/// refused.
/// </remarks>
public class ApplicationSignInManager : SignInManager<User>
{
    public ApplicationSignInManager(
        UserManager<User> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<User> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<User>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<User> confirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
    }

    public override Task<bool> CanSignInAsync(User user) =>
        Task.FromResult(user.Enabled && user.DeletedAt is null && user.Status.IsLoginAllowed());
}
