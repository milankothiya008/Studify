using System.Text;
using Microsoft.EntityFrameworkCore;

namespace SmartLearningPlatform.Data;

/// <summary>
/// Rewrites the model's generated identifiers into snake_case so the PostgreSQL
/// schema reads the way the Java/Hibernate original did (<c>password_hash</c>,
/// <c>instructor_id</c>, ...) instead of PascalCase, which Postgres would fold
/// to lower case anyway and which would force quoting everywhere.
/// </summary>
public static class NamingConventions
{
    /// <summary>
    /// Applies snake_case to table, column, key, foreign-key and index names for
    /// every non-owned entity in the model. Owned types are skipped on purpose:
    /// their columns are named explicitly in
    /// <see cref="ApplicationDbContext.OnModelCreating"/> so the address
    /// prefixes match the Java <c>@AttributeOverrides</c>.
    /// </summary>
    public static void ApplySnakeCaseNames(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (entity.IsOwned()) continue;

            // [Table("...")] already supplies snake_case names; only fill the gaps.
            if (entity.GetTableName() is { } table)
            {
                entity.SetTableName(ToSnakeCase(table));
            }

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }

            foreach (var key in entity.GetKeys())
            {
                if (key.GetName() is { } name) key.SetName(ToSnakeCase(name));
            }

            foreach (var fk in entity.GetForeignKeys())
            {
                if (fk.GetConstraintName() is { } name) fk.SetConstraintName(ToSnakeCase(name));
            }

            foreach (var index in entity.GetIndexes())
            {
                if (index.GetDatabaseName() is { } name) index.SetDatabaseName(ToSnakeCase(name));
            }
        }
    }

    /// <summary>
    /// "PasswordHash" -> "password_hash", "UserId" -> "user_id",
    /// "IX_Course_InstructorId" -> "ix_course_instructor_id". Runs of capitals
    /// are kept together ("URL" -> "url", not "u_r_l") and an underscore is
    /// never doubled.
    /// </summary>
    public static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var builder = new StringBuilder(name.Length + 8);

        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];

            if (current == '_')
            {
                if (builder.Length > 0 && builder[^1] != '_') builder.Append('_');
                continue;
            }

            if (char.IsUpper(current))
            {
                var startsNewWord =
                    i > 0
                    && name[i - 1] != '_'
                    && (!char.IsUpper(name[i - 1])                                  // aB   -> a_b
                        || (i + 1 < name.Length && char.IsLower(name[i + 1])));     // ABc  -> a_bc

                if (startsNewWord && builder.Length > 0 && builder[^1] != '_') builder.Append('_');
                builder.Append(char.ToLowerInvariant(current));
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}
