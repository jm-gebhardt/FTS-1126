using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;

/// <summary>
/// Persistence DBContext used for sagas
/// </summary>
public class SagaPersistenceDbContext : SagaDbContext
{
    /// <summary>
    /// Configuration section where the ConnectionString for AGV Master Control is located
    /// </summary>
    public const string ConfigSectionName = "AMC";

    /// <summary>
    /// Name of the migration table
    /// </summary>
    public const string MigrationTableName = $"__{nameof(SagaPersistenceDbContext)}Migration";

    /// <summary>
    /// Persistence DBContext used for sagas 
    /// </summary>
    /// <param name="options"></param>
    public SagaPersistenceDbContext(DbContextOptions<SagaPersistenceDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Class to Entity mappings
    /// This is called by <see cref="SagaDbContext.OnModelCreating"/>
    /// </summary>
    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get
        {
            yield return new AgvSagaClassMap();
        }
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