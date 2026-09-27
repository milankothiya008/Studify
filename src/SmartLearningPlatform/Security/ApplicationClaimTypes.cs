namespace SmartLearningPlatform.Security;

/// <summary>
/// Claim types this application issues on top of Identity's standard set.
/// </summary>
public static class ApplicationClaimTypes
{
    /// <summary>
    /// One claim per <c>Authority</c> reachable through the user's roles — the
    /// fine-grained permission the <c>authority</c> / <c>role_authority</c>
    /// tables have always described. Authorization policies test this claim, so
    /// a grant made on the Role &rarr; authority screen becomes an access rule
    /// the next time that user signs in.
    /// </summary>
    public const string Authority = "slp:authority";

    /// <summary>
    /// The user's profile name, so the layout can greet them without a query.
    /// Identity's own Name claim carries the login handle instead.
    /// </summary>
    public const string DisplayName = "slp:display-name";
}
