using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;

/// <summary>
/// TransportOrder DB Context that does not use MassTransit sagas
/// </summary>
public class TransportOrderDbContext: ConfigurableDbContext
{
        
    /// <summary>
    /// Configuration section where the ConnectionString for AGV Master Control is located
    /// </summary>
    public const string ConfigSectionName = "AMC";
        
    /// <summary>
    /// Name of the migration table
    /// </summary>
    public const string MigrationTableName = $"__{nameof(TransportOrderDbContext)}Migration";
        
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="options"></param>
    public TransportOrderDbContext(DbContextOptions<TransportOrderDbContext> options) : base(options)
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
    protected override IEnumerable<IClassMap> Configurations
    {
        get
        {
            yield return new NodeLockClassMap();
            yield return new MapClassMap();
            yield return new ActionGroupClassMap();
            yield return new ConfigurationSettingsClassMap();
            yield return new AgvGraphClassMap();
            yield return new GraphEdgeClassMap();
            yield return new GraphNodeClassMap();
            yield return new AgvDataClassMap();
            yield return new AgvErrorClassMap();
        }
    }
}