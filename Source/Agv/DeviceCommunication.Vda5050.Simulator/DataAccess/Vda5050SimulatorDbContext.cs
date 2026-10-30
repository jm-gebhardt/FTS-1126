using Gebhardt.Acc.Common.Persistence.Ef;
using Microsoft.EntityFrameworkCore;

namespace DeviceCommunication.Vda5050.Simulator.DataAccess;

public class Vda5050SimulatorDbContext : BaseDbContext<Vda5050SimulatorDbContext>, IDbContext<Vda5050SimulatorDbContext>
{
    public static string ConfigSectionName => "DEVICECOMMUNICATIONVDA5050SIMULATOR";

    public Vda5050SimulatorDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AgvTypeConfigurationEntityConfiguration());
    }
}