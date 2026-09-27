using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartLearningPlatform.Data;
using SmartLearningPlatform.Models.Users;
using SmartLearningPlatform.Security;
using SmartLearningPlatform.Services;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────── Services ───────────────────────────

builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, ClaimsCurrentUserAccessor>();
builder.Services.AddScoped<AuditInterceptor>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is missing. Set it in appsettings.json "
        + "or the ConnectionStrings__DefaultConnection environment variable.");

builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history"));
    options.AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>());

    // Join rows such as user_roles have a required FK to soft-deletable users.
    // The filter on the principal is deliberate — the warning describes exactly
    // the behaviour wanted here, so it is acknowledged rather than thrown.
    options.ConfigureWarnings(w =>
        w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// ─────────────────────────── Identity ───────────────────────────

// ASP.NET Core Identity over the port's own users / roles / user_roles tables.
// It replaces the hand-rolled pieces — the direct IPasswordHasher calls and the
// session key that named an "acting user" — without changing any rule: the
// password hash format is the one already in the database (PBKDF2 through
// Identity's PasswordHasher<User>), the lockout counters are the port's existing
// columns, and who may sign in is still decided by UserStatus.
builder.Services
    .AddIdentity<User, Role>(options =>
    {
        // The port's only password rule was a minimum of eight characters, set
        // on the user form. Identity's default also demands mixed case, a digit
        // and a symbol; keeping the looser rule leaves existing accounts valid.
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;

        // Drives failed_login_attempt and account_locked_until, which User maps
        // onto Identity's AccessFailedCount and LockoutEnd.
        options.Lockout.MaxFailedAccessAttempts =
            builder.Configuration.GetValue("Authentication:MaxFailedAccessAttempts", 5);
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(
            builder.Configuration.GetValue("Authentication:LockoutMinutes", 15));
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = true;

        // Confirmation is not this app's gate — UserStatus is, and
        // ApplicationSignInManager enforces it.
        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedPhoneNumber = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddUserStore<ApplicationUserStore>()
    .AddSignInManager<ApplicationSignInManager>()
    .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".SmartLearningPlatform.Identity";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ReturnUrlParameter = "returnUrl";

    // Same four hours the acting-user session used to last.
    options.ExpireTimeSpan = TimeSpan.FromHours(4);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization(options =>
{
    // Every screen in this app is administrative, so the default is "signed in".
    // Anonymous access is opted into explicitly, with [AllowAnonymous].
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Turns "course:publish" and friends into policies on demand — see the class.
builder.Services.AddSingleton<IAuthorizationPolicyProvider, AuthorityPolicyProvider>();

builder.Services.AddApplicationRepositories();

var app = builder.Build();

// ─────────────────────────── Pipeline ───────────────────────────

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ──────────────────── Database migrate + seed ────────────────────

// Off by default: an app that migrates its own database on boot is convenient
// locally and a hazard in production, where migrations belong in a deploy step.
if (app.Configuration.GetValue("Database:AutoMigrate", false))
{
    await using var scope = app.Services.CreateAsyncScope();
    await DbInitializer.InitializeAsync(scope.ServiceProvider);
}

app.Run();
