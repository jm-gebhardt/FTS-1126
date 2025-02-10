using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

[TestClass]
public class ActionGroupDurationEstimationServiceTest
{
    [TestMethod]
    public async Task GetAverageDurationEstimateDeviationAsync_FirstDeviationEstimateForAgvAndTargetNodes_AddToInMemoryCache()
    {
        (double value, DateTime lastUpdated) deviation = (12, DateTime.UtcNow);
        var agvId = Guid.NewGuid();
        string[] targets = ["a target", "another target"];

        var fixture = new ActionGroupDurationEstimationServiceTestFixture()
            .WithDeviation(deviation, agvId, targets, existsInMemory: false);

        fixture.ValueExistsInMemory(agvId, targets, out var inMemoryDeviation).ShouldBe(false);
        (await fixture.Service.GetAverageDurationEstimateDeviationAsync(agvId, targets)).ShouldBe(deviation.value);
        fixture.ValueExistsInMemory(agvId, targets, out inMemoryDeviation).ShouldBe(true);
        inMemoryDeviation.value.ShouldBe(deviation.value);
        inMemoryDeviation.lastUpdated.ShouldBeGreaterThan(deviation.lastUpdated);
    }

    [TestMethod]
    public async Task GetAverageDurationEstimateDeviationAsync_DeviationEstimateForAgvAndTargetNodesExists_GetFromInMemoryCache()
    {
        (double value, DateTime lastUpdated) deviation = (12, DateTime.UtcNow);
        var agvId = Guid.NewGuid();
        string[] targets = ["a target", "another target"];

        var fixture = new ActionGroupDurationEstimationServiceTestFixture()
            .WithDeviation(deviation, agvId, targets, existsInMemory: true);

        fixture.ValueExistsInMemory(agvId, targets, out var inMemoryDeviation).ShouldBe(true);
        (await fixture.Service.GetAverageDurationEstimateDeviationAsync(agvId, targets)).ShouldBe(deviation.value);
        fixture.ValueExistsInMemory(agvId, targets, out inMemoryDeviation).ShouldBe(true);
        inMemoryDeviation.ShouldBe(deviation);
    }

    [TestMethod]
    public async Task GetAverageDurationEstimateDeviationAsync_OutdatedDeviationEstimateForAgvAndTargetNodesExists_UpdateInMemoryCache()
    {
        (double value, DateTime lastUpdated) deviation = (12, DateTime.UtcNow.AddHours(-1));
        var agvId = Guid.NewGuid();
        string[] targets = ["a target", "another target"];

        var fixture = new ActionGroupDurationEstimationServiceTestFixture()
            .WithDeviation(deviation, agvId, targets, existsInMemory: true);

        fixture.ValueExistsInMemory(agvId, targets, out var inMemoryDeviation).ShouldBe(true);
        (await fixture.Service.GetAverageDurationEstimateDeviationAsync(agvId, targets)).ShouldBe(deviation.value);
        fixture.ValueExistsInMemory(agvId, targets, out inMemoryDeviation).ShouldBe(true);
        inMemoryDeviation.value.ShouldBe(deviation.value);
        inMemoryDeviation.lastUpdated.ShouldBeGreaterThan(deviation.lastUpdated);
    }

    [DataRow(0, 0)]
    [DataRow(1, 1)]
    [DataTestMethod]
    public async Task GetDurationEstimate_MoveToCoordinate_ReturnsDuration(double velocity, double expectedDuration)
    {
        var agvPositionStatus = new AgvPositionStatus(Guid.NewGuid(), "agv", new AgvPosition(true, 0, 1, 0, 0));
        var fixture = new ActionGroupDurationEstimationServiceTestFixture()
            .WithPositionStatus(agvPositionStatus);

        var action = new AgvAction(Guid.NewGuid(), ActionType.Move, new ActionTarget(1, 1, 0), Guid.NewGuid());

        (await fixture.Service.GetDurationEstimate(velocity, action, agvPositionStatus.CorrelationId)).ShouldBe(expectedDuration);
    }

    [DataRow(0, 0)]
    [DataRow(1, 1)]
    [DataTestMethod]
    public async Task GetDurationEstimate_MoveFromCoordinateToGraph_ReturnsDuration(double velocity, double expectedDuration)
    {
        var node = new GraphNode("a node", 1, 1, 0, AgvGraph.NodeType.WayPoint);
        var agvPositionStatus = new AgvPositionStatus(Guid.NewGuid(), "agv", new AgvPosition(true, 0, 1, 0, 0));
        var fixture = new ActionGroupDurationEstimationServiceTestFixture()
            .WithPositionStatus(agvPositionStatus)
            .WithNode(node);

        var action = new AgvAction(Guid.NewGuid(), ActionType.Move, node.Id, Guid.NewGuid());

        (await fixture.Service.GetDurationEstimate(velocity, action, agvPositionStatus.CorrelationId)).ShouldBe(expectedDuration);
    }

    [TestMethod]
    public async Task AddNonRouteCosts_LastActionWasNotMoveToCoordinate_AddsDeviation()
    {
        (double value, DateTime lastUpdated) deviation = (12, DateTime.UtcNow.AddHours(-1));
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 70);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);

        var fixture = new ActionGroupDurationEstimationServiceTestFixture()
            .WithDeviation(deviation, agv.CorrelationId, actionGroup.GetTargets(), existsInMemory: true);

        (await fixture.Service.AddNonRouteCosts(false, actionGroup, actionGroup.Actions.Single(a => a.IsFirstAction), agv)).ShouldBe(deviation.value + actionGroup.Actions.Count * ActionCosts.SwitchingActions);
    }

    [TestMethod]
    public async Task AddNonRouteCosts_LastActionWasMoveToCoordinate_DoesNotAddDeviation()
    {
        (double value, DateTime lastUpdated) deviation = (12, DateTime.UtcNow.AddHours(-1));
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: 70);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId);

        var fixture = new ActionGroupDurationEstimationServiceTestFixture()
            .WithDeviation(deviation, agv.CorrelationId, actionGroup.GetTargets(), existsInMemory: true);

        (await fixture.Service.AddNonRouteCosts(true, actionGroup, actionGroup.Actions.Single(a => a.IsFirstAction), agv)).ShouldBe(actionGroup.Actions.Count * ActionCosts.SwitchingActions);
    }

    [TestMethod]
    public async Task AddNonRouteCosts_ChargeAction_ReturnsCosts()
    {
        const int batteryCharge = 40;
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: batteryCharge);
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv.CorrelationId);

        var fixture = new ActionGroupDurationEstimationServiceTestFixture();

        (await fixture.Service.AddNonRouteCosts(false, actionGroup, actionGroup.Actions.Single(a => a.IsFirstAction), agv)).ShouldBe(actionGroup.Actions.Count * ActionCosts.SwitchingActions + (100 - batteryCharge));
    }
}