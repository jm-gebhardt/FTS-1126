using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TestPlanRunner.Domain.Providers;
using Gebhardt.Acc.TestPlanRunner.Domain.Validators;
using Gebhardt.Acc.Common.Domain.Models.Stations;

namespace TestPlanRunner.Test.Domain.Validators;

public class TestPlanStepValidatorTestFixture : FixtureBase
{
    private readonly StationProvider _stationProvider = new();
    private readonly List<StationTypeInfo> _typedStations = [];

    public TestPlanStepValidator Validator { get; private set; }

    public TestPlanStepValidatorTestFixture()
    {
        Validator = new TestPlanStepValidator(_stationProvider);
        base.Build();
    }

    public TestPlanStepValidatorTestFixture WithStations(HashSet<string> labels)
    {
        _stationProvider.Update(labels);
        return this;
    }

    public TestPlanStepValidatorTestFixture WithTypedStation(string name, AgvGraph.NodeType primaryType, params AgvGraph.NodeType[] additionalTypes)
    {
        _typedStations.Add(new StationTypeInfo(name, primaryType, additionalTypes));
        _stationProvider.UpdateStationTypes(_typedStations);
        return this;
    }
}