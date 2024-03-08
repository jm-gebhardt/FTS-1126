using Gebhardt.StoreWare.Agv.Common.Persistence.Ef;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Statistics.Persistence.Ef.DataAccess;

/// <inheritdoc />
[UsedImplicitly]
public class StatisticsDbDesignTimeDbContextFactory : DesignTimeDbContextFactory<StatisticsDbContext>
{
    /// <inheritdoc />
    public StatisticsDbDesignTimeDbContextFactory() : base(StatisticsDbContext.ConfigSectionName, StatisticsDbContext.MigrationTableName)
    {
    }
}