using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.AgvSettings;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.LogEntry;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.Trigger;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.Zone;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;

/// <summary>
/// TransportOrder DB Context 
/// </summary>
public class TransportOrderDbContext : BaseDbContext<TransportOrderDbContext>, IDbContext<TransportOrderDbContext>
{
    /// <inheritdoc />
    public static string ConfigSectionName => "AMC";

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
        modelBuilder.ApplyConfiguration(new InstantActionDefinitionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ZoneEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MaintenanceLogEntityConfiguration());
        modelBuilder.ApplyConfiguration(new LogEntryEntityConfiguration());
    }
}