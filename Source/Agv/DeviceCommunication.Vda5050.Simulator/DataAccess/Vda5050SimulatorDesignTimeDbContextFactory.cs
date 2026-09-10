using Gebhardt.Acc.Common.Persistence.Ef;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.DataAccess;

/// <inheritdoc />
[UsedImplicitly]
public class Vda5050SimulatorDesignTimeDbContextFactory : DesignTimeDbContextFactory<Vda5050SimulatorDbContext>
{
    /// <inheritdoc />
    public Vda5050SimulatorDesignTimeDbContextFactory() : base(Vda5050SimulatorDbContext.ConfigSectionName, IDbContext<Vda5050SimulatorDbContext>.MigrationTableName)
    {
    }
}