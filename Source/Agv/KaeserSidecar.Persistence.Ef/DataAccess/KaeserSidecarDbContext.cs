using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess;

/// <summary>
/// Db Context for Kaesersidecar
/// </summary>
public class KaeserSidecarDbContext: DbContext
{
    /// <summary>
    /// Configuration section where the ConnectionString for the statistics service is located
    /// </summary>
    public const string ConfigSectionName = "KAESERSIDECAR";
    
    /// <summary>
    /// Name of the migration table
    /// </summary>
    public const string MigrationTableName = $"__{nameof(KaeserSidecarDbContext)}Migration";
   
    /// <inheritdoc />
    public KaeserSidecarDbContext(DbContextOptions<KaeserSidecarDbContext> options): base(options){}

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new StationOccupancyEntityConfiguration());
        modelBuilder.ApplyConfiguration(new JamStateEntityConfiguration());
        modelBuilder.ApplyConfiguration(new OrderReportEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DecisionPointActionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new WaitingPointActionEntityConfiguration());
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