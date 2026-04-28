using Gebhardt.Acc.Common.Domain.Exceptions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Common.Persistence.Ef;

/// <summary>
/// Abstract base class for EF Core DbContext.
/// </summary>
/// <typeparam name="TContext">DbContext class.</typeparam>
public abstract class BaseDbContext<TContext> : DbContext
    where TContext : DbContext, IDbContext<TContext>
{
    private const int CommandTimeOutMin = 5;

    /// <inheritdoc />
    protected BaseDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// Configure the DbContext
    /// The default EF Logging is very chatty and prints each CommandExecution using LogLevel.Information.
    /// This configuration changes this to LogLevel.Debug
    /// Source: https://github.com/dotnet/efcore/issues/14523#issuecomment-491532384
    /// </summary>
    /// <param name="optionsBuilder"></param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(c =>
        {
            // Logs from Microsoft.EntityFrameworkCore.Database.Command
            c.Log((RelationalEventId.CommandExecuted, LogLevel.Debug));
            // Logs from Microsoft.EntityFrameworkCore.Infrastructure
            c.Log((CoreEventId.ContextInitialized, LogLevel.Debug));
        });
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // EF Core 10: Avoid OPENJSON in SQL by translating parameterized collections to constants
        // This prevents dynamic SQL generation and improves query plan reuse
        // See: https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/breaking-changes#openjson
        modelBuilder.Model.SetMaxIdentifierLength(128);
    }

    /// <summary>
    /// Removes all migrations from the database and inserts a new initial migration.
    /// If there is no migration table at all, it will do nothing.
    /// </summary>
    /// <param name="initialMigrationName">Initial migration to set.</param>
    /// <param name="version">Product version of the initial migration.</param>
    [PublicAPI]
    public async Task ConsolidateMigrationsAsync(string initialMigrationName, string version)
    {
        string sql = $"""
                      IF OBJECT_ID(N'dbo.{IDbContext<TContext>.MigrationTableName}', 'U') IS NOT NULL
                      BEGIN
                          EXEC(N'
                            IF NOT EXISTS (
                                SELECT 1
                                  FROM dbo.{IDbContext<TContext>.MigrationTableName}
                                 WHERE MigrationId = N''{initialMigrationName}''
                            )
                            BEGIN
                                TRUNCATE TABLE dbo.{IDbContext<TContext>.MigrationTableName};
                                INSERT INTO dbo.{IDbContext<TContext>.MigrationTableName} (MigrationId, ProductVersion)
                                VALUES (N''{initialMigrationName}'', N''{version}'');
                            END
                          ');
                      END
                      """;
        await Database.ExecuteSqlRawAsync(sql);
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateStringLengths();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ValidateStringLengths();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ValidateStringLengths()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            foreach (var property in entry.Metadata.GetProperties())
            {
                if (property.ClrType != typeof(string))
                {
                    continue;
                }

                int? maxLength = property.GetMaxLength();
                if (maxLength is null)
                {
                    continue;
                }

                if (entry.CurrentValues[property] is not string value)
                {
                    continue;
                }

                if (value.Length > maxLength.Value)
                {
                    throw new StringLengthExceededException(value.Length, maxLength.Value, property.Name, entry.Metadata.DisplayName());
                }
            }
        }
    }

    /// <summary>
    /// Sets the timeout to use for commands with this context.
    /// </summary>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public BaseDbContext<TContext> SetCommandTimeOut()
    {
        Database.SetCommandTimeout(TimeSpan.FromMinutes(CommandTimeOutMin));
        return this;
    }
}