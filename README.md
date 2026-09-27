# .NET Smart Learning Platform

An ASP.NET Core MVC port of the user and course domains from the Spring Boot
[Smart-Learning-Platform](https://github.com/hetfaldu11/Smart-Learning-Platform),
backed by PostgreSQL through Entity Framework Core.

Eleven entities were ported — exactly the ones requested — each with a repository,
a controller and a full set of CRUD views.

## What is in here

| Java entity (JPA)                | C# entity (EF Core)  | Table               |
| -------------------------------- | -------------------- | ------------------- |
| `User`                           | `User`               | `users`             |
| `UserProfile`                    | `UserProfile`        | `user_profiles`     |
| `UserSocialLink`                 | `UserSocialLink`     | `user_social_links` |
| `Role`                           | `Role`               | `roles`             |
| `Authority`                      | `Authority`          | `authority`         |
| `UserRole`                       | `UserRole`           | `user_roles`        |
| `RoleAuthority`                  | `RoleAuthority`      | `role_authority`    |
| `Course`                         | `Course`             | `course`            |
| `CourseDetail`                   | `CourseDetail`       | `course_details`    |
| `CourseMedia`                    | `CourseMedia`        | `course_media`      |
| `CoursePricing`                  | `CoursePricing`      | `course_pricing`    |

Each entity gets `Index`, `Details`, `Create`, `Edit` and `Delete` views (55 in
total), a typed repository interface and implementation, and an MVC controller.

## Running it

Requirements: .NET 8 SDK and PostgreSQL 13+.

```bash
# 1. create the database
createdb smart_learning_platform

# 2. point the app at it (or edit appsettings.json)
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=smart_learning_platform;Username=postgres;Password=postgres"

# 3. apply the schema
dotnet tool install --global dotnet-ef        # once
dotnet ef database update --project src/SmartLearningPlatform

# 4. run
dotnet run --project src/SmartLearningPlatform
```

Then open the URL the console prints.

In the `Development` environment `appsettings.Development.json` turns on
`Database:AutoMigrate` and `Database:SeedSampleData`, so the app migrates itself
on boot and seeds three users, three roles, nine authorities and three courses.
Both switches default to **off** everywhere else — migrating on boot is
convenient locally and a hazard in production, where migrations belong in a
deploy step.

Every screen requires a sign-in. With the sample data seeded, use:

| Account                     | Role              | Can reach                        |
| --------------------------- | ----------------- | -------------------------------- |
| `admin@smartlearning.dev`   | `ROLE_ADMIN`      | every screen                     |
| `grace@smartlearning.dev`   | `ROLE_INSTRUCTOR` | courses, and users read-only     |
| `linus@smartlearning.dev`   | `ROLE_STUDENT`    | nothing — see below              |

The password is `Password123!` for all three. The student account is seeded as
*Pending verification*, so it is refused at sign-in — that is `UserStatus`
working, not a bug.

## How the port was made

The goal was to keep the database shape recognisable to anyone who knows the Java
original, while writing C# that reads like C#.

**Kept as-is.** Table and column names (`password_hash`, `instructor_id`,
`certification_available`), the `uk_user_role` and `uk_role_authority` unique
constraints, `numeric(10,2)` prices, `timestamp without time zone` columns to
match Java's `LocalDateTime`, enums stored by name rather than ordinal (the
`@Enumerated(EnumType.STRING)` behaviour), and the `deleted_at` soft-delete
column wherever the Java entity had one.

**Translated.**

- `@MappedSuperclass DateAudit` / `UserDateAudit` → abstract base classes. EF Core
  has no mapped-superclass concept, so they stay unmapped and their properties are
  inherited into each table. `User` is the exception: it derives from
  `IdentityUser<long>` instead, C# allowing only one base, so it declares the same
  four audit properties itself. `AuditInterceptor` works off the
  `ITimestampAudit` / `IUserAudit` interfaces, so it never noticed.
- `@Embeddable Address` with `@AttributeOverrides` → an EF Core **owned type**,
  landing in the same `home_`/`work_` prefixed columns.
- `@MapsId` one-to-ones (`UserProfile`, `CourseDetail`, `CourseMedia`,
  `CoursePricing`) → shared primary keys, so the satellite's id *is* the owner's id.
- Hibernate's `@CreationTimestamp` / `@UpdateTimestamp` and Spring Data's
  `AuditingEntityListener` → a single `AuditInterceptor` on `SaveChanges`, so no
  repository or controller has to remember. It also makes `created_at` and
  `created_by` immutable after insert.
- Spring Security's `BCryptPasswordEncoder` → `PasswordHasher<User>` (PBKDF2),
  the hasher ASP.NET Core Identity uses by default.
- Spring Security's filter chain, `UserDetailsService` and `@PreAuthorize` →
  ASP.NET Core Identity with cookie authentication and claims-based policies.
  See *Authentication* below.
- Per-repository `WHERE deleted_at IS NULL` → one global query filter per
  soft-deletable entity, applied in `ApplicationDbContext`. A single query opts
  out with `IgnoreQueryFilters()`.
- `JpaRepository<T, ID>` → `IRepository<TEntity, TKey>` plus a shared
  `Repository<TEntity, TKey>` base; each entity's repository adds only its own
  queries.

**Collapsed.** The requested model list contains no lookup entities, so the Java
tables whose only real column was a name became enums on the owning entity:
`genders`, `education_levels`, `professions`, `platforms`, `course_levels` and
`currency`. That removes six tables and six joins without losing any data the
original stored. `Course.courseStatus` was already an enum and stayed one.

**Dropped.** `CourseMedia` and `UserProfile` pointed at a Cloudinary-backed
`files` table. With no `File` entity in the requested set, each of those foreign
keys became the URL it resolved to (`ThumbnailUrl`, `ProfilePictureUrl`, …).

## Notes on the .NET version

The request said ".NET Framework". Classic .NET Framework 4.8 is Windows-only and
its last feature release was 2019, so this targets **.NET 8 (LTS)** with ASP.NET
Core MVC — the current cross-platform successor, and the only option that runs the
same code on Linux, macOS and Windows. The MVC programming model (controllers,
Razor views, model binding, validation) is the same one Framework developers know.

## Authentication

Sign-in is **ASP.NET Core Identity**, mapped onto the tables this port already
had rather than onto Identity's own. `User` derives from `IdentityUser<long>` and
`Role` from `IdentityRole<long>`, but they still map to `users` and `roles`, and
every rule the port defined still decides the outcome:

| Identity concept        | What it is wired to here                                          |
| ----------------------- | ----------------------------------------------------------------- |
| `PasswordHash`          | the existing `password_hash` column, same PBKDF2 hasher as before  |
| `AccessFailedCount`     | overridden onto the existing `failed_login_attempt` column         |
| `LockoutEnd`            | overridden onto the existing `account_locked_until` column         |
| "may this user sign in" | `ApplicationSignInManager` returns the port's `UserStatus` gate    |
| roles                   | the existing `user_roles` rows, surrogate key and `assigned_at` kept |
| authorities             | flattened into claims at sign-in by `ApplicationUserClaimsPrincipalFactory` |

So `UserManager.AccessFailedAsync` increments the column the Java entity already
declared, and a locked or suspended account is refused for the reason
`UserStatus.IsLoginAllowed()` has always given.

**Authorization.** Every controller carries `[Authorize(Policy = ...)]` naming an
authority — `user:read`, `course:publish` and the rest — and the fallback policy
makes "signed in" the default for anything that does not say otherwise. Policies
are not registered one by one: `AuthorityPolicyProvider` builds one on demand for
any `resource:action` name, so an authority added on the Authorities screen and
granted on the Role → authority screen starts working without a code change. It
takes effect at the user's next sign-in, when their claims are rebuilt.

**What the audit columns record.** `created_by` / `updated_by` used to come from
an "acting user" the operator picked in the UI; they now come from the sign-in
cookie via `ClaimsCurrentUserAccessor`. `POST /Home/SetActingUser` is gone.

**Still not in scope**, as in the original port: email verification, OTP and the
two-factor flows the Java application had. The columns and Identity's token
providers are in place, but no screen drives them.

### Migrating an existing database

`AddIdentityAuthentication` only adds columns and tables — nothing is dropped,
renamed or retyped. It ends with a backfill that fills the new lookup columns for
rows written before Identity existed (`user_name`, the normalised columns, a
security stamp and `lockout_enabled`); without it those accounts could not sign
in. Existing password hashes keep working, because the hasher did not change.

## Layout

```
src/SmartLearningPlatform/
  Models/
    Common/      DateAudit, UserDateAudit, Address, audit + soft-delete interfaces
    Users/       the seven user-domain entities, with their enums
    Courses/     the four course-domain entities, with their enums
  Data/          ApplicationDbContext, migrations, snake_case naming, seeding
  Repositories/  IRepository + base, then one interface/implementation per entity
  Controllers/   one per entity, plus Home and Account
  Views/         one folder per controller, five views each, plus shared layout
  Security/      authority names, the on-demand authorization policy provider
  Services/      AuditInterceptor, ICurrentUserAccessor, the Identity overrides
```
