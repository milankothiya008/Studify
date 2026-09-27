namespace SmartLearningPlatform.Services;

/// <summary>
/// Supplies the id stamped into <c>created_by</c> / <c>updated_by</c>, standing
/// in for Spring Security's <c>AuditorAware</c>.
/// </summary>
/// <remarks>
/// Backed by <see cref="ClaimsCurrentUserAccessor"/>, which reads the signed-in
/// user out of the Identity cookie. Before authentication existed the same
/// interface was served from a session key the operator set by hand; the
/// interceptor that consumes it never had to change.
/// </remarks>
public interface ICurrentUserAccessor
{
    /// <summary>Signed-in user's id, or null when the request is anonymous.</summary>
    long? GetCurrentUserId();

    /// <summary>
    /// Names the acting user for the remainder of this request, before a sign-in
    /// cookie exists. Sign-in itself writes to the user row — resetting the
    /// failed-attempt counter, restamping the security token — and without this
    /// those writes would record no author at all.
    /// </summary>
    void UseUserForRequest(long userId);
}
