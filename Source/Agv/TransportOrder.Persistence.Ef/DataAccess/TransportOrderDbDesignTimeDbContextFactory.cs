using Gebhardt.Acc.Common.Persistence.Ef;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;

/// <inheritdoc />
[UsedImplicitly]
public class TransportOrderDbDesignTimeDbContextFactory : DesignTimeDbContextFactory<TransportOrderDbContext>
{
    /// <inheritdoc />
    public TransportOrderDbDesignTimeDbContextFactory() : base(TransportOrderDbContext.ConfigSectionName, TransportOrderDbContext.MigrationTableName)
    {
    }
}