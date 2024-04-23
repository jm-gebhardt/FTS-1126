using Gebhardt.Acc.Common.Persistence.Ef;
using JetBrains.Annotations;

namespace Gebhardt.Acc.WCSCommunication.Persistence.DataAccess;

/// <inheritdoc />
[UsedImplicitly]
public class WcsCommunicationDbDesignTimeDbContextFactory : DesignTimeDbContextFactory<WcsCommunicationDbContext>
{
    /// <inheritdoc />
    public WcsCommunicationDbDesignTimeDbContextFactory() : base(WcsCommunicationDbContext.ConfigSectionName, WcsCommunicationDbContext.MigrationTableName)
    {
    }
}