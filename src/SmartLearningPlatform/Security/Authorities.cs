namespace SmartLearningPlatform.Security;

/// <summary>
/// The authority names seeded into the <c>authority</c> table, as constants so a
/// controller's <c>[Authorize(Policy = ...)]</c> cannot drift from the row it
/// depends on. These are policy names too — see <see cref="AuthorityPolicyProvider"/>.
/// </summary>
public static class Authorities
{
    public const string UserRead = "user:read";
    public const string UserWrite = "user:write";
    public const string UserDelete = "user:delete";

    public const string RoleRead = "role:read";
    public const string RoleWrite = "role:write";

    public const string CourseRead = "course:read";
    public const string CourseWrite = "course:write";
    public const string CoursePublish = "course:publish";
    public const string CourseDelete = "course:delete";
}
