using Gebhardt.Acc.Common.Persistence.Ef;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.DataAccess;

/// <inheritdoc />
[UsedImplicitly]
public class DeviceCommunicationDesignTimeDbContextFactory : DesignTimeDbContextFactory<DeviceCommunicationDbContext>
{
    /// <inheritdoc />
    public DeviceCommunicationDesignTimeDbContextFactory() : base(DeviceCommunicationDbContext.ConfigSectionName, IDbContext<DeviceCommunicationDbContext>.MigrationTableName)
    {
    }
}