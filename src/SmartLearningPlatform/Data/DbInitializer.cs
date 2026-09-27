using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartLearningPlatform.Models.Common;
using SmartLearningPlatform.Models.Courses;
using SmartLearningPlatform.Models.Courses.Enums;
using SmartLearningPlatform.Models.Users;
using SmartLearningPlatform.Models.Users.Enums;

namespace SmartLearningPlatform.Data;

/// <summary>
/// Applies pending migrations and, when asked, seeds enough data to click
/// through every screen. Replaces the Java side's <c>data.sql</c> plus the
/// <c>CommandLineRunner</c> that created the baseline roles.
/// </summary>
/// <remarks>
/// Roles and users go in through <c>RoleManager</c> and <c>UserManager</c>
/// rather than straight through the context, so every seeded row carries what
/// Identity needs to sign someone in: the normalised lookup columns, a security
/// stamp, a hashed password and the lockout flag.
/// </remarks>
public static class DbInitializer
{
    /// <summary>Password given to every seeded account.</summary>
    public const string SeedPassword = "Password123!";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbInitializer));
        var configuration = services.GetRequiredService<IConfiguration>();
        var roleManager = services.GetRequiredService<RoleManager<Role>>();

        logger.LogInformation("Applying database migrations...");
        await context.Database.MigrateAsync();

        // Roles and authorities are structural, not sample data: the app is not
        // usable without them, so they are seeded whether or not sample data is.
        await SeedRolesAndAuthoritiesAsync(context, roleManager, logger);

        if (configuration.GetValue("Database:SeedSampleData", false))
        {
            var userManager = services.GetRequiredService<UserManager<User>>();
            await SeedSampleDataAsync(context, userManager, logger);
        }
    }

    private static async Task SeedRolesAndAuthoritiesAsync(
        ApplicationDbContext context, RoleManager<Role> roleManager, ILogger logger)
    {
        if (await context.Roles.AnyAsync()) return;

        logger.LogInformation("Seeding roles and authorities...");

        var authorityNames = new (string Name, string Description)[]
        {
            ("user:read", "View user accounts"),
            ("user:write", "Create and edit user accounts"),
            ("user:delete", "Retire user accounts"),
            ("role:read", "View roles and their authorities"),
            ("role:write", "Create and edit roles, grant authorities"),
            ("course:read", "View courses"),
            ("course:write", "Create and edit courses"),
            ("course:publish", "Move a course to Published"),
            ("course:delete", "Retire courses"),
        };

        var authorities = authorityNames
            .Select(a => new Authority { Name = a.Name, Description = a.Description })
            .ToList();
        context.Authorities.AddRange(authorities);

        var roles = new List<Role>
        {
            new() { Name = "ROLE_ADMIN", Description = "Full access to every screen" },
            new() { Name = "ROLE_INSTRUCTOR", Description = "Authors and publishes their own courses" },
            new() { Name = "ROLE_STUDENT", Description = "Browses and enrols in published courses" },
        };

        // Authorities are this app's own table, so they go in directly. Roles are
        // Identity's, so RoleManager creates them and fills in normalized_name.
        await context.SaveChangesAsync();

        foreach (var role in roles)
        {
            var created = await roleManager.CreateAsync(role);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not seed role {role.Name}: "
                    + string.Join("; ", created.Errors.Select(e => e.Description)));
            }
        }

        var byName = authorities.ToDictionary(a => a.Name);

        var grants = new Dictionary<string, string[]>
        {
            ["ROLE_ADMIN"] = authorityNames.Select(a => a.Name).ToArray(),
            ["ROLE_INSTRUCTOR"] = new[] { "course:read", "course:write", "course:publish", "user:read" },
            ["ROLE_STUDENT"] = new[] { "course:read" },
        };

        foreach (var role in roles)
        {
            foreach (var authorityName in grants[role.Name!])
            {
                context.RoleAuthorities.Add(new RoleAuthority
                {
                    RoleId = role.Id,
                    AuthorityId = byName[authorityName].Id,
                });
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation(
            "Seeded {RoleCount} roles and {AuthorityCount} authorities.",
            roles.Count, authorities.Count);
    }

    private static async Task SeedSampleDataAsync(
        ApplicationDbContext context, UserManager<User> userManager, ILogger logger)
    {
        if (await context.Users.AnyAsync()) return;

        logger.LogInformation("Seeding sample users and courses...");

        var admin = BuildUser(
            "admin@smartlearning.dev", "Ada", "Lovelace", UserStatus.Active,
            "Runs the platform.", Profession.Manager, EducationLevel.Masters);

        var instructor = BuildUser(
            "grace@smartlearning.dev", "Grace", "Hopper", UserStatus.Active,
            "Teaches systems programming and compiler construction.",
            Profession.SoftwareEngineer, EducationLevel.Doctorate);

        var student = BuildUser(
            "linus@smartlearning.dev", "Linus", "Nilsson", UserStatus.PendingVerification,
            "Learning backend engineering.", Profession.Student, EducationLevel.Bachelors);

        foreach (var user in new[] { admin, instructor, student })
        {
            var created = await userManager.CreateAsync(user, SeedPassword);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not seed user {user.Email}: "
                    + string.Join("; ", created.Errors.Select(e => e.Description)));
            }
        }

        // ─── Social links ───
        context.UserSocialLinks.AddRange(
            new UserSocialLink
            {
                UserId = instructor.Id,
                Platform = SocialPlatform.GitHub,
                Url = "https://github.com/example-grace",
            },
            new UserSocialLink
            {
                UserId = instructor.Id,
                Platform = SocialPlatform.LinkedIn,
                Url = "https://www.linkedin.com/in/example-grace",
            },
            new UserSocialLink
            {
                UserId = admin.Id,
                Platform = SocialPlatform.Website,
                Url = "https://smartlearning.dev",
            });

        // ─── Role assignments ───
        var rolesByName = await context.Roles.ToDictionaryAsync(r => r.Name!, r => r.Id);
        context.UserRoles.AddRange(
            new UserRole { UserId = admin.Id, RoleId = rolesByName["ROLE_ADMIN"] },
            new UserRole { UserId = instructor.Id, RoleId = rolesByName["ROLE_INSTRUCTOR"] },
            new UserRole { UserId = student.Id, RoleId = rolesByName["ROLE_STUDENT"] });

        await context.SaveChangesAsync();

        // ─── Courses with all three satellites ───
        var courses = new[]
        {
            BuildCourse(
                instructor.Id,
                "Compilers from Scratch",
                "Build a working compiler for a small language, one pass at a time",
                CourseLevel.Advanced, CourseStatus.Published,
                description: "Lexing, parsing, type checking, IR and code generation, "
                             + "written from an empty file in twelve weeks.",
                requirement: "Comfortable in one systems language. No compiler theory assumed.",
                learningOutcome: "Design a grammar, write a recursive-descent parser, lower an "
                                 + "AST to an IR and emit runnable code.",
                price: 149.00m, discount: 99.00m, currency: CurrencyCode.USD,
                hasCertificate: true, hasAssignment: true, hasProject: true, hasQuiz: false),

            BuildCourse(
                instructor.Id,
                "PostgreSQL for Application Developers",
                "Indexes, transactions and query plans, explained through real workloads",
                CourseLevel.Intermediate, CourseStatus.Published,
                description: "How Postgres actually executes your queries, and what to change "
                             + "when it picks the wrong plan.",
                requirement: "Working knowledge of SQL SELECT, JOIN and GROUP BY.",
                learningOutcome: "Read an EXPLAIN plan, choose an index that helps, and reason "
                                 + "about isolation levels.",
                price: 89.00m, discount: 0m, currency: CurrencyCode.USD,
                hasCertificate: true, hasAssignment: true, hasProject: false, hasQuiz: true),

            BuildCourse(
                instructor.Id,
                "ASP.NET Core MVC End to End",
                "Controllers, views, EF Core and deployment for developers new to .NET",
                CourseLevel.Beginner, CourseStatus.Draft,
                description: "A tour of the MVC pipeline, model binding, validation and "
                             + "Entity Framework Core against PostgreSQL.",
                requirement: "Any object-oriented language. C# is taught as we go.",
                learningOutcome: "Ship a database-backed MVC application with validated forms "
                                 + "and a real migration history.",
                price: 0m, discount: 0m, currency: CurrencyCode.EUR,
                hasCertificate: false, hasAssignment: false, hasProject: true, hasQuiz: true),
        };

        context.Courses.AddRange(courses);
        await context.SaveChangesAsync();

        logger.LogInformation(
            "Seeded {UserCount} users and {CourseCount} courses. Sample password: {Password}",
            3, courses.Length, SeedPassword);
    }

    private static User BuildUser(
        string email, string firstName, string lastName,
        UserStatus status, string aboutMe, Profession profession, EducationLevel education)
    {
        // UserManager.CreateAsync supplies the password hash, the security stamp
        // and the normalised lookup columns.
        return new User
        {
            Email = email,
            UserName = email,
            Status = status,
            Enabled = status.IsEnabled(),
            PasswordChangedAt = DateTime.UtcNow,
            Profile = new UserProfile
            {
                FirstName = firstName,
                LastName = lastName,
                AboutMe = aboutMe,
                Profession = profession,
                EducationLevel = education,
                InstituteName = "Smart Learning Platform",
                HomeAddress = new Address { City = "Stockholm", Country = "Sweden" },
                WorkAddress = new Address(),
            },
        };
    }

    private static Course BuildCourse(
        long instructorId, string title, string subtitle, CourseLevel level, CourseStatus status,
        string description, string requirement, string learningOutcome,
        decimal price, decimal discount, CurrencyCode currency,
        bool hasCertificate, bool hasAssignment, bool hasProject, bool hasQuiz) =>
        new()
        {
            InstructorId = instructorId,
            Title = title,
            Subtitle = subtitle,
            CourseLevel = level,
            CourseStatus = status,
            Detail = new CourseDetail
            {
                Description = description,
                Requirement = requirement,
                LearningOutcome = learningOutcome,
                HasCertificate = hasCertificate,
                HasAssignment = hasAssignment,
                HasProject = hasProject,
                HasQuiz = hasQuiz,
            },
            Media = new CourseMedia
            {
                ThumbnailUrl = "https://placehold.co/600x340/0d6efd/ffffff/png?text="
                               + Uri.EscapeDataString(title),
            },
            Pricing = new CoursePricing
            {
                Price = price,
                DiscountPrice = discount,
                Currency = currency,
            },
        };
}
