using Gebhardt.Acc.Common.Persistence.Ef;
using JetBrains.Annotations;

namespace Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;

/// <inheritdoc />
[UsedImplicitly]
public class PrismatCommunicationDesignTimeDbContextFactory : DesignTimeDbContextFactory<PrismatCommunicationDbContext>
{
    /// <inheritdoc />
    public PrismatCommunicationDesignTimeDbContextFactory() : base(PrismatCommunicationDbContext.ConfigSectionName, PrismatCommunicationDbContext.MigrationTableName)
    {
    }
}