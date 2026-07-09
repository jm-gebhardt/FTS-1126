using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TestPlanRunner.Domain.Models;
using Shouldly;

namespace TestPlanRunner.Test.Domain.Validators;

[TestClass]
public class TestPlanValidatorTest
{
    [TestMethod]
    public void Validate_NameEmpty_Invalid()
    {
        var fixture = new TestPlanValidatorTestFixture();

        var testPlan = new TestPlan { Steps = [new TestPlanStep { StepType = StepType.Pick }] };

        fixture.Validator.Validate(testPlan).IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public void Validate_StepsEmpty_Invalid()
    {
        var fixture = new TestPlanValidatorTestFixture();

        var testPlan = new TestPlan { Name = "Test", Steps = [] };

        fixture.Validator.Validate(testPlan).IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public void Validate_IterationsNegative_Invalid()
    {
        var fixture = new TestPlanValidatorTestFixture().WithTypedStation("station", AgvGraph.NodeType.Pick);

        var testPlan = new TestPlan
        {
            Name = "Test",
            Iterations = -1,
            Steps = [new TestPlanStep { StepType = StepType.Pick, TargetNode = "station" }]
        };

        fixture.Validator.Validate(testPlan).IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public void Validate_IterationsZero_Valid()
    {
        var fixture = new TestPlanValidatorTestFixture().WithTypedStation("station", AgvGraph.NodeType.Pick);

        var testPlan = new TestPlan
        {
            Name = "Test",
            Iterations = 0,
            Steps = [new TestPlanStep { StepType = StepType.Pick, TargetNode = "station" }]
        };

        fixture.Validator.Validate(testPlan).IsValid.ShouldBeTrue();
    }

    [TestMethod]
    public void Validate_ValidTestPlan_Valid()
    {
        var fixture = new TestPlanValidatorTestFixture().WithTypedStation("station", AgvGraph.NodeType.Pick);

        var testPlan = new TestPlan
        {
            Name = "Test",
            Steps = [new TestPlanStep { StepType = StepType.Pick, TargetNode = "station" }]
        };

        fixture.Validator.Validate(testPlan).IsValid.ShouldBeTrue();
    }
}