using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.DataAccess;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Repositories;

public class SimulatedAgvTypeConfigurationRepositoryTestFixture : DbTestFixtureBase<Vda5050SimulatorDbContext>
{
    public const string Manufacturer = SimulatedAgvTypeConfigurationGenerator.Manufacturer;
    public const string SeriesName = SimulatedAgvTypeConfigurationGenerator.SeriesName;
    public const string AgvTypeIdentifier = SimulatedAgvTypeConfigurationGenerator.AgvTypeIdentifier;
    public const string OtherAgvTypeIdentifier = SimulatedAgvTypeConfigurationGenerator.OtherAgvTypeIdentifier;
    public const int NoOfAgvs = 3;
    public const int UpdatedNoOfAgvs = 5;

    public ISimulatedAgvTypeConfigurationRepository Sut { get; }

    public SimulatedAgvTypeConfigurationRepositoryTestFixture()
    {
        Sut = new EfSimulatedAgvTypeConfigurationRepository(ContextFactory);
    }
}