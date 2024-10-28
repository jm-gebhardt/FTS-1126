using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.SqlServer;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.AgvSettings;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.Trigger;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;

/// <summary>
/// TransportOrder DB Context 
/// </summary>
public class TransportOrderDbContext : DbContext
{
    /// <summary>
    /// Configuration section where the ConnectionString for AGV Master Control is located
    /// </summary>
    public const string ConfigSectionName = "AMC";

    /// <summary>
    /// Name of the migration table
    /// </summary>
    public const string MigrationTableName = $"__{nameof(TransportOrderDbContext)}Migration";

    /// <inheritdoc />
    public TransportOrderDbContext(DbContextOptions<TransportOrderDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ActionGroupEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AgvEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AgvDataEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AgvErrorEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AgvGraphEntityConfiguration());
        modelBuilder.ApplyConfiguration(new GraphEdgeEntityConfiguration());
        modelBuilder.ApplyConfiguration(new GraphNodeEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MapEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MapAgvEntityConfiguration());
        modelBuilder.ApplyConfiguration(new CuboidEntityConfiguration());
        modelBuilder.ApplyConfiguration(new Point3DEntityConfiguration());
        modelBuilder.ApplyConfiguration(new CargoEntityConfiguration());
        modelBuilder.ApplyConfiguration(new NodeLockEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AgvSettingsGroupEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AgvSettingEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SettingEntityConfiguration());
        modelBuilder.ApplyConfiguration(new FactoryTimeEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TriggerEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TriggerLocationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TriggerPolygonEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RestrictedAreaEntityConfiguration());
        modelBuilder.ApplyConfiguration(new StationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new QueueEntityConfiguration());
        // Adds Quartz.NET SqlServer schema to EntityFrameworkCore
        modelBuilder.AddQuartz(builder => builder.UseSqlServer(schema: "dbo"));
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