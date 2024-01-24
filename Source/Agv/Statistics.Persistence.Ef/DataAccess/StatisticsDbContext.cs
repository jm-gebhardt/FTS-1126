using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Statistics.Persistence.Ef.DataAccess.Configuration;

namespace Statistics.Persistence.Ef.DataAccess;

public class StatisticsDbContext: DbContext
{
    /// <summary>
    /// Configuration section where the ConnectionString for the statistics service is located
    /// </summary>
    public const string ConfigSectionName = "STAT";

    /// <summary>
    /// Name of the migration table
    /// </summary>
    public const string MigrationTableName = $"__{nameof(StatisticsDbContext)}Migration";

    /// <inheritdoc />
    public StatisticsDbContext(DbContextOptions<StatisticsDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new MapEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AgvGridPositionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new GridEntityConfiguration());
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