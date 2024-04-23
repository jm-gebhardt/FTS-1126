using Gebhardt.Acc.Common.Persistence.Ef;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;

/// <inheritdoc />
[UsedImplicitly]
public class StatisticsDbDesignTimeDbContextFactory : DesignTimeDbContextFactory<StatisticsDbContext>
{
    /// <inheritdoc />
    public StatisticsDbDesignTimeDbContextFactory() : base(StatisticsDbContext.ConfigSectionName, StatisticsDbContext.MigrationTableName)
    {
    }
}