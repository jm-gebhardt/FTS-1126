using Gebhardt.Acc.Common.Persistence.Ef;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TriggerService.Persistence.Ef.DataAccess;

/// <inheritdoc />
[UsedImplicitly]
public class TriggerServiceDbDesignTimeDbContextFactory : DesignTimeDbContextFactory<TriggerServiceDbContext>
{
    /// <inheritdoc />
    public TriggerServiceDbDesignTimeDbContextFactory() : base(TriggerServiceDbContext.ConfigSectionName, TriggerServiceDbContext.MigrationTableName)
    {
    }
}