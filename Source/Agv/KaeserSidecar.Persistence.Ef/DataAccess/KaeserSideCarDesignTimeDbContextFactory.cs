using Gebhardt.Acc.Common.Persistence.Ef;
using JetBrains.Annotations;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess;

/// <inheritdoc />
[UsedImplicitly]
public class KaeserSideCarDesignTimeDbContextFactory: DesignTimeDbContextFactory<KaeserSidecarDbContext>
{
    /// <inheritdoc />
    public KaeserSideCarDesignTimeDbContextFactory() : base(KaeserSidecarDbContext.ConfigSectionName, KaeserSidecarDbContext.MigrationTableName){}
}