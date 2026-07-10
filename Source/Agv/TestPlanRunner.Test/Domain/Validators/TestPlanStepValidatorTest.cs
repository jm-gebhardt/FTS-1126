using Gebhardt.Acc.Common.Domain.Models.Graphs;
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

        var step = new TestPlanStep { StepType = StepType.Move, TargetNode = "waypoint_node" };

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
        var fixture = new TestPlanStepValidatorTestFixture().WithTypedStation("station", AgvGraph.NodeType.Pick);

        var step = new TestPlanStep { StepType = StepType.Pick, TargetNode = "station", Source = "source" };

        fixture.Validator.Validate(step).IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public void Validate_NonHostOrder_WithDestination_Invalid()
    {
        var fixture = new TestPlanStepValidatorTestFixture().WithTypedStation("station", AgvGraph.NodeType.Pick);

        var step = new TestPlanStep { StepType = StepType.Pick, TargetNode = "station", Destination = "dest" };

        fixture.Validator.Validate(step).IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public void Validate_NonHostOrder_WithLeRotation_Invalid()
    {
        var fixture = new TestPlanStepValidatorTestFixture().WithTypedStation("station", AgvGraph.NodeType.Pick);

        var step = new TestPlanStep { StepType = StepType.Pick, TargetNode = "station", LeRotation = 1.5 };

        fixture.Validator.Validate(step).IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public void Validate_NonHostOrder_Valid()
    {
        var fixture = new TestPlanStepValidatorTestFixture().WithTypedStation("station", AgvGraph.NodeType.Pick);

        var step = new TestPlanStep { StepType = StepType.Pick, TargetNode = "station" };

        fixture.Validator.Validate(step).IsValid.ShouldBeTrue();
    }

    [TestMethod]
    [DataRow(StepType.Charge, AgvGraph.NodeType.Charge)]
    [DataRow(StepType.Pick, AgvGraph.NodeType.Pick)]
    [DataRow(StepType.Pick, AgvGraph.NodeType.PickDrop)]
    [DataRow(StepType.Drop, AgvGraph.NodeType.Drop)]
    [DataRow(StepType.Drop, AgvGraph.NodeType.PickDrop)]
    [DataRow(StepType.Move, AgvGraph.NodeType.WayPoint)]
    public void Validate_TargetStationTypeMatchesStepType_Valid(StepType stepType, AgvGraph.NodeType stationType)
    {
        var fixture = new TestPlanStepValidatorTestFixture().WithTypedStation("station", stationType);

        var step = new TestPlanStep { StepType = stepType, TargetNode = "station" };

        fixture.Validator.Validate(step).IsValid.ShouldBeTrue();
    }

    [TestMethod]
    [DataRow(StepType.Charge, AgvGraph.NodeType.Pick)]
    [DataRow(StepType.Charge, AgvGraph.NodeType.Park)]
    [DataRow(StepType.Pick, AgvGraph.NodeType.Drop)]
    [DataRow(StepType.Pick, AgvGraph.NodeType.Charge)]
    [DataRow(StepType.Drop, AgvGraph.NodeType.Pick)]
    [DataRow(StepType.Drop, AgvGraph.NodeType.Charge)]
    public void Validate_TargetStationTypeMismatchesStepType_Invalid(StepType stepType, AgvGraph.NodeType stationType)
    {
        var fixture = new TestPlanStepValidatorTestFixture().WithTypedStation("station", stationType);

        var step = new TestPlanStep { StepType = stepType, TargetNode = "station" };

        fixture.Validator.Validate(step).IsValid.ShouldBeFalse();
    }

    [TestMethod]
    public void Validate_Charge_ChargeParkStation_Valid()
    {
        var fixture = new TestPlanStepValidatorTestFixture()
            .WithTypedStation("station", AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park);

        var step = new TestPlanStep { StepType = StepType.Charge, TargetNode = "station" };

        fixture.Validator.Validate(step).IsValid.ShouldBeTrue();
    }

    [TestMethod]
    public void Validate_Charge_ParkStationWithAdditionalCharge_Valid()
    {
        var fixture = new TestPlanStepValidatorTestFixture()
            .WithTypedStation("station", AgvGraph.NodeType.Park, AgvGraph.NodeType.Charge);

        var step = new TestPlanStep { StepType = StepType.Charge, TargetNode = "station" };

        fixture.Validator.Validate(step).IsValid.ShouldBeTrue();
    }
}