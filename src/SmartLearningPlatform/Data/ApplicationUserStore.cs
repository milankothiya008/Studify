using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartLearningPlatform.Models.Users;

namespace SmartLearningPlatform.Data;

/// <summary>
/// The stock EF Core user store, with one adjustment.
/// </summary>
/// <remarks>
/// Identity keys <c>user_roles</c> on the (user_id, role_id) pair, so the stock
/// store looks a row up with <c>FindAsync(userId, roleId)</c>. This schema keeps
/// the port's surrogate <c>UserRole.Id</c> as the primary key — its CRUD screens
/// address rows by it — which would make that call throw. Querying the pair
/// instead is the whole difference; every other store method is unchanged.
/// </remarks>
public class ApplicationUserStore : UserStore<
    User, Role, ApplicationDbContext, long,
    IdentityUserClaim<long>, UserRole, IdentityUserLogin<long>,
    IdentityUserToken<long>, IdentityRoleClaim<long>>
{
    public ApplicationUserStore(ApplicationDbContext context, IdentityErrorDescriber? describer = null)
        : base(context, describer) { }

    protected override Task<UserRole?> FindUserRoleAsync(
        long userId, long roleId, CancellationToken cancellationToken) =>
        Context.Set<UserRole>().SingleOrDefaultAsync(
            ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
}
