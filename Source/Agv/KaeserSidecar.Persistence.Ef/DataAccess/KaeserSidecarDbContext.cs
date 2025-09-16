using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess;

/// <summary>
/// Db Context for KaeserSidecar
/// </summary>
public class KaeserSidecarDbContext : BaseDbContext<KaeserSidecarDbContext>, IDbContext<KaeserSidecarDbContext>
{
    /// <inheritdoc />
    public static string ConfigSectionName => "KAESERSIDECAR";

    /// <inheritdoc />
    public KaeserSidecarDbContext(DbContextOptions<KaeserSidecarDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new StationOccupancyEntityConfiguration());
        modelBuilder.ApplyConfiguration(new JamStateEntityConfiguration());
        modelBuilder.ApplyConfiguration(new OrderReportEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DecisionPointActionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new WaitingPointActionEntityConfiguration());
    }
}