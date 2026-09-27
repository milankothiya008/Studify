using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartLearningPlatform.Models.Common;
using SmartLearningPlatform.Models.Courses;
using SmartLearningPlatform.Models.Users;

namespace SmartLearningPlatform.Data;

/// <summary>
/// EF Core mapping for the eleven ported entities, standing in for the Java
/// side's JPA annotations plus Hibernate's schema generation.
/// </summary>
/// <remarks>
/// Derives from <see cref="IdentityDbContext{TUser,TRole,TKey,TUserClaim,TUserRole,TUserLogin,TRoleClaim,TUserToken}"/>
/// so ASP.NET Core Identity's stores can work against this context.
/// <c>Users</c>, <c>Roles</c> and <c>UserRoles</c> come from that base and map to
/// the port's own <c>users</c>, <c>roles</c> and <c>user_roles</c> tables —
/// <see cref="ConfigureIdentity"/> puts every table and index name back, because
/// the base class would otherwise rename them to <c>AspNetUsers</c> and friends.
/// </remarks>
public class ApplicationDbContext : IdentityDbContext<
    User, Role, long,
    IdentityUserClaim<long>, UserRole, IdentityUserLogin<long>,
    IdentityRoleClaim<long>, IdentityUserToken<long>>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // ─── User domain (Users, Roles and UserRoles are inherited) ───
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserSocialLink> UserSocialLinks => Set<UserSocialLink>();
    public DbSet<Authority> Authorities => Set<Authority>();
    public DbSet<RoleAuthority> RoleAuthorities => Set<RoleAuthority>();

    // ─── Course domain ───
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseDetail> CourseDetails => Set<CourseDetail>();
    public DbSet<CourseMedia> CourseMedia => Set<CourseMedia>();
    public DbSet<CoursePricing> CoursePricings => Set<CoursePricing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureIdentity(modelBuilder);
        ConfigureUsers(modelBuilder);
        ConfigureCourses(modelBuilder);

        StoreEnumsAsText(modelBuilder);
        StoreDateTimesWithoutTimeZone(modelBuilder);
        ApplySoftDeleteFilters(modelBuilder);

        // Last: rewrite every generated identifier into snake_case. Owned types
        // are skipped, so the address column names configured above survive.
        modelBuilder.ApplySnakeCaseNames();
    }

    /// <summary>
    /// Puts back everything the Identity base class renamed, and reconciles the
    /// one place where Identity's shape and the port's shape disagree.
    /// </summary>
    private static void ConfigureIdentity(ModelBuilder modelBuilder)
    {
        // The base class points these at AspNetUsers / AspNetRoles / AspNetUserRoles.
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<UserRole>().ToTable("user_roles");

        // No screen here uses external logins, per-user claims or reset tokens,
        // but the Identity stores resolve these sets, so they are mapped with
        // names that match the rest of the schema rather than Identity's defaults.
        modelBuilder.Entity<IdentityUserClaim<long>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<long>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityUserToken<long>>().ToTable("user_tokens");
        modelBuilder.Entity<IdentityRoleClaim<long>>().ToTable("role_claims");

        modelBuilder.Entity<User>(entity =>
        {
            // Identity calls these UserNameIndex and EmailIndex. Renamed to match
            // the schema, and filtered on deleted_at for the same reason
            // uk_users_email is: a retired account must not keep a login handle
            // reserved forever.
            entity.HasIndex(u => u.NormalizedUserName)
                  .IsUnique()
                  .HasDatabaseName("uk_users_normalized_user_name")
                  .HasFilter("deleted_at IS NULL");

            entity.HasIndex(u => u.NormalizedEmail)
                  .HasDatabaseName("ix_users_normalized_email");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            // Identity widens a role name to 256 characters through the fluent
            // API, which outranks the [StringLength(64)] on the property. The
            // port's column is 64, so it is set back here.
            entity.Property(r => r.Name).HasMaxLength(64);
            entity.Property(r => r.NormalizedName).HasMaxLength(64);

            entity.HasIndex(r => r.NormalizedName)
                  .IsUnique()
                  .HasDatabaseName("uk_roles_normalized_name")
                  .HasFilter("deleted_at IS NULL");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            // Identity keys this table on (user_id, role_id). The port gave it a
            // surrogate id and an assigned_at stamp, and its CRUD screens address
            // rows by that id, so the surrogate key wins. The pair stays unique
            // through uk_user_role below, and ApplicationUserStore overrides the
            // one store method that assumed the composite key.
            entity.HasKey(ur => ur.Id);
            entity.Property(ur => ur.Id).ValueGeneratedOnAdd();
        });
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            // Email is the login handle. Filtered so soft-deleted rows do not
            // hold an address hostage — Postgres skips nulls-excluded rows.
            entity.HasIndex(u => u.Email)
                  .IsUnique()
                  .HasDatabaseName("uk_users_email")
                  .HasFilter("deleted_at IS NULL");

            entity.HasIndex(u => u.Status).HasDatabaseName("ix_users_status");
            entity.HasIndex(u => u.DeletedAt).HasDatabaseName("ix_users_deleted_at");

            // created_by / updated_by point back at users. Cascading here would
            // create a cycle, so the FKs simply block deletion of an auditor.
            entity.HasOne(u => u.CreatedBy)
                  .WithMany()
                  .HasForeignKey(u => u.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(u => u.UpdatedBy)
                  .WithMany()
                  .HasForeignKey(u => u.UpdatedById)
                  .OnDelete(DeleteBehavior.Restrict);

            // Shared primary key: user_profiles.user_id is both PK and FK.
            entity.HasOne(u => u.Profile)
                  .WithOne(p => p.User)
                  .HasForeignKey<UserProfile>(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.SocialLinks)
                  .WithOne(l => l.User)
                  .HasForeignKey(l => l.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.UserRoles)
                  .WithOne(ur => ur.User)
                  .HasForeignKey(ur => ur.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            // The Java @Embeddable + @AttributeOverrides pair, restated. Both
            // addresses are required-in-shape so a profile always has the
            // objects, even when every component is null.
            entity.OwnsOne(p => p.HomeAddress, address =>
            {
                address.Property(a => a.Street).HasColumnName("home_street").HasMaxLength(200);
                address.Property(a => a.City).HasColumnName("home_city").HasMaxLength(100);
                address.Property(a => a.State).HasColumnName("home_state").HasMaxLength(100);
                address.Property(a => a.Country).HasColumnName("home_country").HasMaxLength(100);
                address.Property(a => a.PostalCode).HasColumnName("home_postal_code").HasMaxLength(20);
                address.Property(a => a.FullAddress).HasColumnName("home_full_address").HasMaxLength(500);
            });
            entity.Navigation(p => p.HomeAddress).IsRequired();

            entity.OwnsOne(p => p.WorkAddress, address =>
            {
                address.Property(a => a.Street).HasColumnName("work_street").HasMaxLength(200);
                address.Property(a => a.City).HasColumnName("work_city").HasMaxLength(100);
                address.Property(a => a.State).HasColumnName("work_state").HasMaxLength(100);
                address.Property(a => a.Country).HasColumnName("work_country").HasMaxLength(100);
                address.Property(a => a.PostalCode).HasColumnName("work_postal_code").HasMaxLength(20);
                address.Property(a => a.FullAddress).HasColumnName("work_full_address").HasMaxLength(500);
            });
            entity.Navigation(p => p.WorkAddress).IsRequired();

            // Shared PK is never generated — it is copied from the user.
            entity.Property(p => p.UserId).ValueGeneratedNever();
            entity.Property(p => p.AboutMe).HasColumnType("text");
        });

        modelBuilder.Entity<UserSocialLink>(entity =>
        {
            // One link per platform per user, ignoring soft-deleted rows.
            entity.HasIndex(l => new { l.UserId, l.Platform })
                  .IsUnique()
                  .HasDatabaseName("uk_user_social_link_platform")
                  .HasFilter("deleted_at IS NULL");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(r => r.Name)
                  .IsUnique()
                  .HasDatabaseName("uk_roles_name")
                  .HasFilter("deleted_at IS NULL");
        });

        modelBuilder.Entity<Authority>(entity =>
        {
            entity.HasIndex(a => a.Name)
                  .IsUnique()
                  .HasDatabaseName("uk_authority_name")
                  .HasFilter("deleted_at IS NULL");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            // The Java @UniqueConstraint(name = "uk_user_role").
            entity.HasIndex(ur => new { ur.UserId, ur.RoleId })
                  .IsUnique()
                  .HasDatabaseName("uk_user_role");

            entity.HasOne(ur => ur.Role)
                  .WithMany(r => r.UserRoles)
                  .HasForeignKey(ur => ur.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoleAuthority>(entity =>
        {
            // The Java @UniqueConstraint(name = "uk_role_authority").
            entity.HasIndex(ra => new { ra.RoleId, ra.AuthorityId })
                  .IsUnique()
                  .HasDatabaseName("uk_role_authority");

            entity.HasOne(ra => ra.Role)
                  .WithMany(r => r.RoleAuthorities)
                  .HasForeignKey(ra => ra.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ra => ra.Authority)
                  .WithMany(a => a.RoleAuthorities)
                  .HasForeignKey(ra => ra.AuthorityId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCourses(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasIndex(c => c.InstructorId).HasDatabaseName("ix_course_instructor_id");
            entity.HasIndex(c => c.CourseStatus).HasDatabaseName("ix_course_status");
            entity.HasIndex(c => c.DeletedAt).HasDatabaseName("ix_course_deleted_at");

            // An instructor with courses cannot be hard-deleted; retire the
            // account instead (that is what deleted_at is for).
            entity.HasOne(c => c.Instructor)
                  .WithMany(u => u.CoursesTaught)
                  .HasForeignKey(c => c.InstructorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.CreatedBy)
                  .WithMany()
                  .HasForeignKey(c => c.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.UpdatedBy)
                  .WithMany()
                  .HasForeignKey(c => c.UpdatedById)
                  .OnDelete(DeleteBehavior.Restrict);

            // The three satellites share the course's primary key, exactly as
            // the Java @MapsId one-to-ones did.
            entity.HasOne(c => c.Detail)
                  .WithOne(d => d.Course)
                  .HasForeignKey<CourseDetail>(d => d.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Media)
                  .WithOne(m => m.Course)
                  .HasForeignKey<CourseMedia>(m => m.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Pricing)
                  .WithOne(p => p.Course)
                  .HasForeignKey<CoursePricing>(p => p.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CourseDetail>(entity =>
        {
            entity.Property(d => d.CourseId).ValueGeneratedNever();
            entity.Property(d => d.Description).HasColumnType("text");
            entity.Property(d => d.Requirement).HasColumnType("text");
            entity.Property(d => d.LearningOutcome).HasColumnType("text");

            // The Java column was named certification_available.
            entity.Property(d => d.HasCertificate).HasColumnName("certification_available");
        });

        modelBuilder.Entity<CourseMedia>(entity => entity.Property(m => m.CourseId).ValueGeneratedNever());

        modelBuilder.Entity<CoursePricing>(entity =>
        {
            entity.Property(p => p.CourseId).ValueGeneratedNever();
            entity.Property(p => p.Price).HasPrecision(10, 2);
            entity.Property(p => p.DiscountPrice).HasPrecision(10, 2);
        });
    }

    /// <summary>
    /// Persists every enum by name rather than ordinal, matching the Java
    /// <c>@Enumerated(EnumType.STRING)</c> so the column stays readable and
    /// re-ordering a member cannot silently reinterpret existing rows.
    /// </summary>
    private static void StoreEnumsAsText(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (!type.IsEnum) continue;

                // Longest member name decides the width; leave room to grow.
                var longest = Enum.GetNames(type).Max(n => n.Length);
                property.SetMaxLength(Math.Max(longest + 16, 32));
                property.SetProviderClrType(typeof(string));
            }
        }
    }

    /// <summary>
    /// Maps DateTime to <c>timestamp without time zone</c>, the same shape Java's
    /// <c>LocalDateTime</c> produced. It also sidesteps Npgsql's rule that a
    /// <c>timestamptz</c> parameter must carry <c>DateTimeKind.Utc</c>, which a
    /// value posted from an HTML form never does.
    /// </summary>
    private static void StoreDateTimesWithoutTimeZone(ModelBuilder modelBuilder)
    {
        // Npgsql refuses to write a DateTime whose Kind is Utc into a
        // `timestamp without time zone` column. The audit interceptor and the
        // soft-delete path both stamp DateTime.UtcNow, and a value posted from a
        // form arrives as Unspecified, so the two Kinds would otherwise have to
        // be reconciled at every call site. Stripping the Kind on the way to the
        // database does it once: what lands in the column is the same naive UTC
        // wall-clock value Java's LocalDateTime stored.
        var stripKind = new ValueConverter<DateTime, DateTime>(
            toDatabase => DateTime.SpecifyKind(toDatabase, DateTimeKind.Unspecified),
            fromDatabase => DateTime.SpecifyKind(fromDatabase, DateTimeKind.Unspecified));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (type != typeof(DateTime)) continue;

                property.SetColumnType("timestamp without time zone");
                property.SetValueConverter(stripKind);
            }
        }
    }

    /// <summary>
    /// Adds <c>WHERE deleted_at IS NULL</c> to every query over a soft-deletable
    /// entity. The Java services repeated that predicate by hand in each
    /// repository method; a global filter cannot be forgotten, and
    /// <c>IgnoreQueryFilters()</c> opts a single query back out.
    /// </summary>
    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)) continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var deletedAt = Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
            var body = Expression.Equal(deletedAt, Expression.Constant(null, typeof(DateTime?)));

            modelBuilder.Entity(entityType.ClrType)
                        .HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }
}
