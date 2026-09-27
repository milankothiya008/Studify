using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace SmartLearningPlatform.Security;

/// <summary>
/// Turns any <c>resource:action</c> policy name into a policy requiring the
/// matching authority claim.
/// </summary>
/// <remarks>
/// Authorities are rows, not code: the Authorities screen can add one and the
/// Role &rarr; authority screen can grant it. Registering a fixed list of
/// policies at startup would mean a new row is unusable until someone edits
/// <c>Program.cs</c>, so policies are produced on demand instead. Names that do
/// not look like an authority fall through to the default provider, which is
/// what serves the fallback policy and any explicitly registered one.
/// </remarks>
public class AuthorityPolicyProvider : DefaultAuthorizationPolicyProvider
{
    /// <summary>The same shape the <c>Authority.Name</c> validation enforces.</summary>
    private static readonly Regex AuthorityName =
        new("^[a-z][a-z0-9_]*:[a-z][a-z0-9_]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public AuthorityPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // An explicitly registered policy always wins.
        var registered = await base.GetPolicyAsync(policyName);
        if (registered is not null) return registered;

        if (!AuthorityName.IsMatch(policyName)) return null;

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim(ApplicationClaimTypes.Authority, policyName)
            .Build();
    }
}
