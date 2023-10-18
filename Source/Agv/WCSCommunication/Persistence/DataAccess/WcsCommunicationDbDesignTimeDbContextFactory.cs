using Gebhardt.StoreWare.Agv.Common.Persistence;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Persistence.DataAccess;

/// <inheritdoc />
[UsedImplicitly]
public class WcsCommunicationDbDesignTimeDbContextFactory : DesignTimeDbContextFactory<WcsCommunicationDbContext>
{
    /// <inheritdoc />
    public WcsCommunicationDbDesignTimeDbContextFactory() : base(WcsCommunicationDbContext.ConfigSectionName, WcsCommunicationDbContext.MigrationTableName)
    {
    }
}