using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;

/// <summary>
/// Db Context for Statistics
/// </summary>
public class StatisticsDbContext : BaseDbContext<StatisticsDbContext>, IDbContext<StatisticsDbContext>
{
    /// <inheritdoc />
    public static string ConfigSectionName => "STAT";

    /// <inheritdoc />
    public StatisticsDbContext(DbContextOptions<StatisticsDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new MapEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AgvGridPositionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AgvErrorEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AgvFieldViolationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new GridEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MapAgvPositionsEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AgvWifiMetricsEntityConfiguration());
    }
}