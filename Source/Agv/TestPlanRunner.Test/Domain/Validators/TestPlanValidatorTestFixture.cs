using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TestPlanRunner.Domain.Providers;
using Gebhardt.Acc.TestPlanRunner.Domain.Validators;

namespace TestPlanRunner.Test.Domain.Validators;

public class TestPlanValidatorTestFixture: FixtureBase
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
}