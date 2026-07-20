using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TestPlanRunner.Domain.Providers;
using Gebhardt.Acc.TestPlanRunner.Domain.Validators;
using Gebhardt.Acc.Common.Domain.Models.Stations;

namespace TestPlanRunner.Test.Domain.Validators;

public class TestPlanValidatorTestFixture : FixtureBase
{
    private readonly StationProvider _stationProvider = new();

    public TestPlanValidator Validator { get; private set; }

    public TestPlanValidatorTestFixture()
    {
        Validator = new TestPlanValidator(_stationProvider);
        base.Build();
    }

    public TestPlanValidatorTestFixture WithStations(HashSet<string> labels)
    {
        _stationProvider.Update(labels);
        return this;
    }

    public TestPlanValidatorTestFixture WithTypedStation(string name, AgvGraph.NodeType primaryType, params AgvGraph.NodeType[] additionalTypes)
    {
        _stationProvider.UpdateStationTypes([new StationTypeInfo(name, primaryType, additionalTypes)]);
        return this;
    }
}