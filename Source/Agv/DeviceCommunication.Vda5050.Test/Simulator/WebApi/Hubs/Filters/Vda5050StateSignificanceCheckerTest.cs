using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator;
using Shouldly;
using Fixture = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.WebApi.Hubs.Filters.Vda5050StateSignificanceCheckerTestFixture;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.WebApi.Hubs.Filters;

[TestClass]
public class Vda5050StateSignificanceCheckerTest
{
    [TestMethod]
    public void HasSignificantChanges_OnlyVolatileFieldsChanged_ReturnsFalse()
    {
        var previous = Vda5050StateDtoGenerator.Generate();
        var current = previous with
        {
            HeaderId = previous.HeaderId + 1,
            Timestamp = previous.Timestamp.AddSeconds(5),
            MessageCreatedDate = previous.MessageCreatedDate.AddSeconds(5)
        };

        new Fixture().Checker.HasSignificantChanges(current, previous).ShouldBeFalse();
    }

    [TestMethod]
    public void HasSignificantChanges_BatteryChargeAtThreshold_ReturnsTrue()
    {
        var previous = Vda5050StateDtoGenerator.Generate(batteryCharge: Fixture.BaselineBatteryCharge);
        var current = Vda5050StateDtoGenerator.Generate(batteryCharge: Fixture.BaselineBatteryCharge + Fixture.BatteryChargeThreshold);

        new Fixture().Checker.HasSignificantChanges(current, previous).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_BatteryChargeBelowThreshold_ReturnsFalse()
    {
        var previous = Vda5050StateDtoGenerator.Generate(batteryCharge: Fixture.BaselineBatteryCharge);
        var current = Vda5050StateDtoGenerator.Generate(batteryCharge: Fixture.BaselineBatteryCharge + Fixture.BatteryChargeThreshold - Fixture.InsignificantDelta);

        new Fixture().Checker.HasSignificantChanges(current, previous).ShouldBeFalse();
    }

    [TestMethod]
    public void HasSignificantChanges_PositionAtThreshold_ReturnsTrue()
    {
        var previous = Vda5050StateDtoGenerator.Generate(agvPosition: Vda5050StateDtoGenerator.GenerateAgvPosition());
        var current = Vda5050StateDtoGenerator.Generate(agvPosition: Vda5050StateDtoGenerator.GenerateAgvPosition(Fixture.PositionThreshold));

        new Fixture().Checker.HasSignificantChanges(current, previous).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_PositionBelowThreshold_ReturnsFalse()
    {
        var previous = Vda5050StateDtoGenerator.Generate(agvPosition: Vda5050StateDtoGenerator.GenerateAgvPosition());
        var current = Vda5050StateDtoGenerator.Generate(agvPosition: Vda5050StateDtoGenerator.GenerateAgvPosition(Fixture.PositionThreshold - Fixture.InsignificantDelta));

        new Fixture().Checker.HasSignificantChanges(current, previous).ShouldBeFalse();
    }

    [TestMethod]
    public void HasSignificantChanges_ThetaAtThreshold_ReturnsTrue()
    {
        var previous = Vda5050StateDtoGenerator.Generate(agvPosition: Vda5050StateDtoGenerator.GenerateAgvPosition());
        var current = Vda5050StateDtoGenerator.Generate(agvPosition: Vda5050StateDtoGenerator.GenerateAgvPosition(theta: Fixture.ThetaThreshold));

        new Fixture().Checker.HasSignificantChanges(current, previous).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_NodeStateAdded_ReturnsTrue()
    {
        var previous = Vda5050StateDtoGenerator.Generate();
        var current = Vda5050StateDtoGenerator.Generate(nodeStates: [Vda5050StateDtoGenerator.GenerateNodeState()]);

        new Fixture().Checker.HasSignificantChanges(current, previous).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_NodeStateReleasedChanged_ReturnsTrue()
    {
        var previous = Vda5050StateDtoGenerator.Generate(nodeStates: [Vda5050StateDtoGenerator.GenerateNodeState(released: true)]);
        var current = Vda5050StateDtoGenerator.Generate(nodeStates: [Vda5050StateDtoGenerator.GenerateNodeState(released: false)]);

        new Fixture().Checker.HasSignificantChanges(current, previous).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_ActionStatusChanged_ReturnsTrue()
    {
        var previous = Vda5050StateDtoGenerator.Generate(actionStates: [Vda5050StateDtoGenerator.GenerateActionState(actionStatus: ActionStatusDto.Waiting)]);
        var current = Vda5050StateDtoGenerator.Generate(actionStates: [Vda5050StateDtoGenerator.GenerateActionState(actionStatus: ActionStatusDto.Running)]);

        new Fixture().Checker.HasSignificantChanges(current, previous).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_ErrorAdded_ReturnsTrue()
    {
        var previous = Vda5050StateDtoGenerator.Generate();
        var current = Vda5050StateDtoGenerator.Generate(errors: [Vda5050StateDtoGenerator.GenerateError()]);

        new Fixture().Checker.HasSignificantChanges(current, previous).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_LoadChanged_ReturnsTrue()
    {
        var previous = Vda5050StateDtoGenerator.Generate(loads: [Vda5050StateDtoGenerator.GenerateLoad("a")]);
        var current = Vda5050StateDtoGenerator.Generate(loads: [Vda5050StateDtoGenerator.GenerateLoad("b")]);

        new Fixture().Checker.HasSignificantChanges(current, previous).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_OperatingModeChanged_ReturnsTrue()
    {
        var previous = Vda5050StateDtoGenerator.Generate(operatingMode: OperatingModeDto.Automatic);
        var current = Vda5050StateDtoGenerator.Generate(operatingMode: OperatingModeDto.Manual);

        new Fixture().Checker.HasSignificantChanges(current, previous).ShouldBeTrue();
    }

    [TestMethod]
    public void HasSignificantChanges_IdenticalStates_ReturnsFalse()
    {
        var previous = Vda5050StateDtoGenerator.Generate();
        var current = Vda5050StateDtoGenerator.Generate();

        new Fixture().Checker.HasSignificantChanges(current, previous).ShouldBeFalse();
    }
}