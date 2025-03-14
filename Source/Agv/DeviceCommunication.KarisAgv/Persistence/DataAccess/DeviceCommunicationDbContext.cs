using Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.DataAccess.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.DataAccess;

/// <summary>
/// Db Context for DeviceCommunication.KarisAgv
/// </summary>
public class DeviceCommunicationDbContext : DbContext
{
    /// <summary>
    /// Configuration section where the ConnectionString for the statistics service is located
    /// </summary>
    public const string ConfigSectionName = "AMC";

    /// <summary>
    /// Name of the migration table
    /// </summary>
    public const string MigrationTableName = $"__{nameof(DeviceCommunicationDbContext)}Migration";

    /// <inheritdoc />
    public DeviceCommunicationDbContext(DbContextOptions<DeviceCommunicationDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OrderIdMappingEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MqttNodeEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MqttEdgeMappingEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AgvEntityConfiguration());
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