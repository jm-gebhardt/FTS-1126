using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Models.Stations;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TestPlanRunner.Domain.Providers;

namespace TestPlanRunner.Test.Domain.Providers;

public class StationProviderTestFixture : FixtureBase
{
    public const string StationName = "station";
    public const string UnknownStation = "unknown";
    public const AgvGraph.NodeType PrimaryType = AgvGraph.NodeType.Pick;
    public const AgvGraph.NodeType AdditionalType = AgvGraph.NodeType.Drop;
    public const AgvGraph.NodeType MissingType = AgvGraph.NodeType.Charge;

    public StationProvider StationProvider { get; } = new();

    public StationProviderTestFixture WithStations(params string[] labels)
    {
        StationProvider.Update(labels.ToHashSet());
        return this;
    }

    public StationProviderTestFixture WithTypedStation(string name = StationName, AgvGraph.NodeType primaryType = PrimaryType, params AgvGraph.NodeType[] additionalTypes)
    {
        StationProvider.UpdateStationTypes([new StationTypeInfo(name, primaryType, additionalTypes)]);
        return this;
    }
}