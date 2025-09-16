using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.DataAccess.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.DataAccess;

/// <summary>
/// Db Context for DeviceCommunication.KarisAgv
/// </summary>
public class DeviceCommunicationDbContext : BaseDbContext<DeviceCommunicationDbContext>, IDbContext<DeviceCommunicationDbContext>
{
    /// <inheritdoc />
    public static string ConfigSectionName => "AMC";

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
}