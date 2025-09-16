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
}