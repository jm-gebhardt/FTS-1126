using Gebhardt.Acc.TestPlanRunner.Domain.Models;
using Shouldly;

namespace TestPlanRunner.Test.Domain.Validators;

[TestClass]
public class TestPlanStepValidatorTest
{
    [TestMethod]
    public void Validate_WaitTimeNegative_Invalid()
    {
        var fixture = new TestPlanStepValidatorTestFixture().WithStations(["station"]);

        var step = new TestPlanStep { StepType = StepType.Pick, TargetNode = "station", WaitTimeSeconds = -1 };

        fixture.Validator.Validate(step).IsValid.ShouldBeFalse();
    }

    // HostOrder step tests

    [TestMethod]
    public void Validate_HostOrder_WithTargetNode_Invalid()
    {
        var fixture = new TestPlanStepValidatorTestFixture().WithStations(["source", "dest"]);

        var step = new TestPlanStep
        {
            StepType = StepType.HostOrder,
            TargetNode = "source",
            Source = "source",
            Destination = "dest"
        };

        fixture.Validator.Validate(step).IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public void Validate_HostOrder_SourceEmpty_Invalid()
    {
        var fixture = new TestPlanStepValidatorTestFixture().WithStations(["dest"]);

        var step = new TestPlanStep { StepType = StepType.HostOrder, Destination = "dest" };

        fixture.Validator.Validate(step).IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public void Validate_HostOrder_SourceNotExists_Invalid()
    {
        var fixture = new TestPlanStepValidatorTestFixture().WithStations(["dest"]);

        var step = new TestPlanStep { StepType = StepType.HostOrder, Source = "unknown-source", Destination = "dest" };

        fixture.Validator.Validate(step).IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public void Validate_HostOrder_DestinationEmpty_Invalid()
    {
        var fixture = new TestPlanStepValidatorTestFixture().WithStations(["source"]);

        var step = new TestPlanStep { StepType = StepType.HostOrder, Source = "source" };

        fixture.Validator.Validate(step).IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public void Validate_HostOrder_DestinationNotExists_Invalid()
    {
        var fixture = new TestPlanStepValidatorTestFixture().WithStations(["source"]);

        var step = new TestPlanStep { StepType = StepType.HostOrder, Source = "source", Destination = "unknown-dest" };

        fixture.Validator.Validate(step).IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public void Validate_HostOrder_Valid()
    {
        var fixture = new TestPlanStepValidatorTestFixture().WithStations(["source", "dest"]);

        var step = new TestPlanStep { StepType = StepType.HostOrder, Source = "source", Destination = "dest" };

        fixture.Validator.Validate(step).IsValid.ShouldBeTrue();
    }

    // Non-HostOrder step tests

    [TestMethod]
    public void Validate_NonHostOrderPick_TargetNodeEmpty_Invalid()
    {
        var fixture = new TestPlanStepValidatorTestFixture();

        var step = new TestPlanStep { StepType = StepType.Pick };

        fixture.Validator.Validate(step).IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public void Validate_NonHostOrderMove_TargetNodeNotStation_Valid()
    {
        var fixture = new TestPlanStepValidatorTestFixture();

        var step = new TestPlanStep { StepType = StepType.Move, TargetNode = "waypoint_node"  };

        fixture.Validator.Validate(step).IsValid.ShouldBeTrue();
    }

    [TestMethod]
    public void Validate_NonHostOrderPick_TargetNodeNotExists_Invalid()
    {
        var fixture = new TestPlanStepValidatorTestFixture();

        var step = new TestPlanStep { StepType = StepType.Pick, TargetNode = "unknown-node" };

        fixture.Validator.Validate(step).IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public void Validate_NonHostOrder_WithSource_Invalid()
    {
        var fixture = new TestPlanStepValidatorTestFixture().WithStations(["station", "source"]);

        var step = new TestPlanStep { StepType = StepType.Pick, TargetNode = "station", Source = "source" };

        fixture.Validator.Validate(step).IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public void Validate_NonHostOrder_WithDestination_Invalid()
    {
        var fixture = new TestPlanStepValidatorTestFixture().WithStations(["station", "dest"]);

        var step = new TestPlanStep { StepType = StepType.Pick, TargetNode = "station", Destination = "dest" };

        fixture.Validator.Validate(step).IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public void Validate_NonHostOrder_WithLeRotation_Invalid()
    {
        var fixture = new TestPlanStepValidatorTestFixture().WithStations(["station"]);

        var step = new TestPlanStep { StepType = StepType.Pick, TargetNode = "station", LeRotation = 1.5 };

        fixture.Validator.Validate(step).IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public void Validate_NonHostOrder_Valid()
    {
        var fixture = new TestPlanStepValidatorTestFixture().WithStations(["station"]);

        var step = new TestPlanStep { StepType = StepType.Pick, TargetNode = "station" };

        fixture.Validator.Validate(step).IsValid.ShouldBeTrue();
    }
}
