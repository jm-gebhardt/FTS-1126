using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess;

/// <summary>
/// Provides a db context.
/// </summary>
public class DeviceCommunicationVda5050DbContext : BaseDbContext<DeviceCommunicationVda5050DbContext>, IDbContext<DeviceCommunicationVda5050DbContext>
{
    /// <inheritdoc />
    public static string ConfigSectionName => "DEVICECOMMUNICATIONVDA5050";

    /// <inheritdoc />
    public DeviceCommunicationVda5050DbContext(DbContextOptions options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
       modelBuilder.ApplyConfiguration(new AgvDataEntityConfiguration());
       modelBuilder.ApplyConfiguration(new AgvActionEntityConfiguration());
       modelBuilder.ApplyConfiguration(new AgvEntityConfiguration());
       modelBuilder.ApplyConfiguration(new LifFileEntityConfiguration());
       modelBuilder.ApplyConfiguration(new LayoutEntityConfiguration());
       modelBuilder.ApplyConfiguration(new AgvTypeEntityConfiguration());
    }
}

 