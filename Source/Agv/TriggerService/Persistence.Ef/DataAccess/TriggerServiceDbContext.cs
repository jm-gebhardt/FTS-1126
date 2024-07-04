using Gebhardt.Acc.TriggerService.Persistence.Ef.DataAccess.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TriggerService.Persistence.Ef.DataAccess;

/// <summary>
/// 
/// </summary>
public class TriggerServiceDbContext : DbContext
{
    /// <summary>
    /// Configuration section where the ConnectionString for the trigger service is located
    /// </summary>
    public const string ConfigSectionName = "TRIG";

    /// <summary>
    /// Name of the migration table
    /// </summary>
    public const string MigrationTableName = $"__{nameof(TriggerServiceDbContext)}Migration";

    /// <inheritdoc />
    public TriggerServiceDbContext(DbContextOptions<TriggerServiceDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TriggerEntityConfiguration());
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
}