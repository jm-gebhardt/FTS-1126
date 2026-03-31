using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TestPlanRunner.Domain.Providers;
using Gebhardt.Acc.TestPlanRunner.Domain.Validators;

namespace TestPlanRunner.Test.Domain.Validators;

public class TestPlanStepValidatorTestFixture : FixtureBase
{
    private readonly StationProvider _stationProvider = new();

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
}