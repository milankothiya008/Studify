using System.Security.Claims;

namespace SmartLearningPlatform.Services;

/// <summary>
/// Claims-backed <see cref="ICurrentUserAccessor"/>: the audit columns record
/// whoever the Identity cookie says is signed in.
/// </summary>
public class ClaimsCurrentUserAccessor : ICurrentUserAccessor
{
    /// <summary>Where <see cref="UseUserForRequest"/> parks its id.</summary>
    private const string RequestUserKey = "slp:request-user-id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClaimsCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public long? GetCurrentUserId()
    {
        // No HttpContext during migrations, seeding or background work.
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) return null;

        var claim = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(claim, out var signedInId)) return signedInId;

        return httpContext.Items.TryGetValue(RequestUserKey, out var pending) && pending is long pendingId
            ? pendingId
            : null;
    }

    public void UseUserForRequest(long userId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) return;

        httpContext.Items[RequestUserKey] = userId;
    }
}
