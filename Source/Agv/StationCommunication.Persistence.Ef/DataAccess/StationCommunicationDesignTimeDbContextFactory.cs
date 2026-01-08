using Gebhardt.Acc.Common.Persistence.Ef;
using JetBrains.Annotations;

namespace Gebhardt.Acc.StationCommunication.Persistence.Ef.DataAccess;

/// <inheritdoc />
[UsedImplicitly]
public class StationCommunicationDesignTimeDbContextFactory : DesignTimeDbContextFactory<StationCommunicationDbContext>
{
    /// <inheritdoc />
    public StationCommunicationDesignTimeDbContextFactory() : base(StationCommunicationDbContext.ConfigSectionName, IDbContext<StationCommunicationDbContext>.MigrationTableName)
    {
    }
}