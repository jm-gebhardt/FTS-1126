using Gebhardt.Acc.Common.Persistence.Ef;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess;

/// <inheritdoc />
[UsedImplicitly]
public class DeviceCommunicationVda5050DesignTimeDbContextFactory : DesignTimeDbContextFactory<DeviceCommunicationVda5050DbContext>
{
    /// <inheritdoc />
    public DeviceCommunicationVda5050DesignTimeDbContextFactory() : base(DeviceCommunicationVda5050DbContext.ConfigSectionName, IDbContext<DeviceCommunicationVda5050DbContext>.MigrationTableName)
    {
    }

    /// <inheritdoc />
    public DeviceCommunicationVda5050DesignTimeDbContextFactory(string connectionStringKey, string migrationTableName) : base(connectionStringKey, migrationTableName)
    {
    }
}