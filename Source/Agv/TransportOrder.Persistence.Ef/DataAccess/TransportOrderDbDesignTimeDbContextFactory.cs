using Gebhardt.StoreWare.Agv.Common.Persistence;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;

/// <inheritdoc />
[UsedImplicitly]
public class TransportOrderDbDesignTimeDbContextFactory : DesignTimeDbContextFactory<TransportOrderDbContext>
{
    /// <inheritdoc />
    public TransportOrderDbDesignTimeDbContextFactory() : base(TransportOrderDbContext.ConfigSectionName, TransportOrderDbContext.MigrationTableName)
    {
    }
}