using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.DataAccess.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.DataAccess;

/// <summary>
/// Provides a db context.
/// </summary>
public class Vda5050SimulatorDbContext : BaseDbContext<Vda5050SimulatorDbContext>, IDbContext<Vda5050SimulatorDbContext>
{
    /// <inheritdoc />
    public static string ConfigSectionName => "DEVICECOMMUNICATIONVDA5050SIMULATOR";

    /// <inheritdoc />
    public Vda5050SimulatorDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SimulatedAgvTypeConfigurationEntityConfiguration());
    }
}