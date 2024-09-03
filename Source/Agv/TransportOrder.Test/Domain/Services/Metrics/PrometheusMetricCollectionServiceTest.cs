using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.Metrics;

[TestClass]
public class PrometheusMetricCollectionServiceTest
{
    [TestMethod]
    public void CollectBatterChargeMetrics_ValidData_BatterChargeGaugeUpdated()
    {
        var fixture = new PrometheusMetricCollectionServiceTestFixture();
        const int batteryCharge = 40;
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: batteryCharge);

        fixture.Service.CollectBatteryChargeMetrics(agv.AgvStatus);
        PrometheusMetricCollector.BatteryChargeGauge.WithLabels(agv.CorrelationId.ToString()).Value.ShouldBe(batteryCharge);
    }

    [TestMethod]
    public async Task CollectTravelDistance_NoLastNodeIdGiven_FullTravelDistance()
    {
        var agvId = Guid.NewGuid();
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        var actionGroupCorrelationId = Guid.NewGuid();
        var actionGroup = new TransportOrder.Domain.Models.ActionGroup.ActionGroup(
            actionGroupCorrelationId,
            string.Empty,
            actions: new List<TransportOrder.Domain.Models.ActionGroup.AgvAction> { new(Guid.NewGuid(), ActionType.Move, AgvGraphGenerator.Prk1, actionGroupCorrelationId) },
            assignedAgv: agvId,
            creator: ActionGroupCreator.TransportOrder
        );
        actionGroup.Actions.First().Nodes = [AgvGraphGenerator.Src1, AgvGraphGenerator.Way1, AgvGraphGenerator.Prk1];
        var fixture = new PrometheusMetricCollectionServiceTestFixture()
            .AddGraph(graph);

        await fixture.Service.CollectTravelDistance(actionGroup, agvId, null);
        PrometheusMetricCollector.AgvTravelDistanceGauge.WithLabels(agvId.ToString()).Value.ShouldBe(graph.GetRouteLength([AgvGraphGenerator.Src1, AgvGraphGenerator.Way1, AgvGraphGenerator.Prk1]));
    }

    [TestMethod]
    public async Task CollectTravelDistance_LastNodeIdGiven_TravelDistanceUntilLastNodeId()
    {
        var agvId = Guid.NewGuid();
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        var actionGroupCorrelationId = Guid.NewGuid();
        var actionGroup = new TransportOrder.Domain.Models.ActionGroup.ActionGroup(
            actionGroupCorrelationId,
            string.Empty,
            actions: new List<TransportOrder.Domain.Models.ActionGroup.AgvAction> { new(Guid.NewGuid(), ActionType.Move, AgvGraphGenerator.Prk1, actionGroupCorrelationId) },
            assignedAgv: agvId,
            creator: ActionGroupCreator.TransportOrder
        );
        actionGroup.Actions.First().Nodes = [AgvGraphGenerator.Src1, AgvGraphGenerator.Way1, AgvGraphGenerator.Prk1];
        var fixture = new PrometheusMetricCollectionServiceTestFixture()
            .AddGraph(graph);

        await fixture.Service.CollectTravelDistance(actionGroup, agvId, AgvGraphGenerator.Way1);
        PrometheusMetricCollector.AgvTravelDistanceGauge.WithLabels(agvId.ToString()).Value.ShouldBe(graph.GetRouteLength([AgvGraphGenerator.Src1, AgvGraphGenerator.Way1]));
    }

    [TestMethod]
    public async Task CollectOrderCompletionMetrics_PickDropOrder_MetricsUpdated()
    {
        var agvId = Guid.NewGuid();
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        const double actionDuration = 36;
        const double idleTime = 42;
        const double idleTimeBetweenActions = 2;
        var startedDate = DateTime.UtcNow;

        var priorActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            pickActionState: ActionState.Completed,
            dropActionState: ActionState.Completed,
            assignedAgv: agvId);
        priorActionGroup.SetFixedDatesFromDurations(actionDuration, startedDate, idleTimeBetweenActions);
        startedDate = priorActionGroup.CompletedDate!.Value.AddSeconds(idleTime);

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            pickActionState: ActionState.Completed,
            dropActionState: ActionState.Completed,
            assignedAgv: agvId,
            source: AgvGraphGenerator.Src1,
            destination: AgvGraphGenerator.Dst1);
        actionGroup.Actions.First().Nodes = [AgvGraphGenerator.Prk1, AgvGraphGenerator.Way1, AgvGraphGenerator.Src1];
        actionGroup.Actions.Last().Nodes = [AgvGraphGenerator.Src1, AgvGraphGenerator.Way1, AgvGraphGenerator.Way3, AgvGraphGenerator.Dst1];
        actionGroup.SetFixedDatesFromDurations(actionDuration, startedDate, idleTimeBetweenActions);
        actionGroup.StartedDate = startedDate;

        var fixture = new PrometheusMetricCollectionServiceTestFixture()
            .AddGraph(graph)
            .AddActionGroups([priorActionGroup, actionGroup]);

        await fixture.Service.CollectOrderCompletionMetrics(actionGroup);

        PrometheusMetricCollector.AgvCompletedOrdersCounter.WithLabels(agvId.ToString()).Value.ShouldBe(1);
        PrometheusMetricCollector.AgvTravelDistanceGauge.WithLabels(agvId.ToString()).Value.ShouldBeGreaterThan(0);
        PrometheusMetricCollector.AgvStateBusyCargoGauge.WithLabels(agvId.ToString()).Value.ShouldBe(actionDuration);
        PrometheusMetricCollector.AgvStateIdleGauge.WithLabels(agvId.ToString()).Value.ShouldBe(idleTime + (actionGroup.Actions.Count - 1) * idleTimeBetweenActions);
        PrometheusMetricCollector.AgvStateChargingGauge.WithLabels(agvId.ToString()).Value.ShouldBe(0);
        PrometheusMetricCollector.AgvStateBusyGauge.WithLabels(agvId.ToString()).Value.ShouldBe(actionDuration);
    }

    [DataRow(ActionState.Completed)]
    [DataRow(ActionState.Aborted)]
    [DataTestMethod]
    public async Task CollectOrderCompletionMetrics_ChargingOrder_MetricsUpdated(ActionState chargeActionState)
    {
        var agvId = Guid.NewGuid();
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        const double actionDuration = 36;
        const double idleTime = 42;
        const double idleTimeBetweenActions = 2;
        var startedDate = DateTime.UtcNow;

        var priorActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            pickActionState: ActionState.Completed,
            dropActionState: ActionState.Completed,
            assignedAgv: agvId);
        priorActionGroup.SetFixedDatesFromDurations(actionDuration, startedDate, idleTimeBetweenActions);
        startedDate = priorActionGroup.CompletedDate!.Value.AddSeconds(idleTime);

        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(
            actionStateCharge: chargeActionState,
            actionStateMove: ActionState.Completed,
            assignedAgv: agvId);
        actionGroup.Actions.First().Nodes = [AgvGraphGenerator.Prk1, AgvGraphGenerator.Way1, AgvGraphGenerator.Way3, AgvGraphGenerator.Crg1];
        actionGroup.Actions.Last().Nodes = [AgvGraphGenerator.Crg1];
        actionGroup.SetFixedDatesFromDurations(actionDuration, startedDate, idleTimeBetweenActions);
        actionGroup.StartedDate = startedDate;

        var fixture = new PrometheusMetricCollectionServiceTestFixture()
            .AddGraph(graph)
            .AddActionGroups([priorActionGroup, actionGroup]);

        await fixture.Service.CollectOrderCompletionMetrics(actionGroup);

        PrometheusMetricCollector.AgvCompletedOrdersCounter.WithLabels(agvId.ToString()).Value.ShouldBe(0);
        PrometheusMetricCollector.AgvTravelDistanceGauge.WithLabels(agvId.ToString()).Value.ShouldBeGreaterThan(0);
        PrometheusMetricCollector.AgvStateBusyCargoGauge.WithLabels(agvId.ToString()).Value.ShouldBe(0);
        PrometheusMetricCollector.AgvStateIdleGauge.WithLabels(agvId.ToString()).Value.ShouldBe(idleTime + (actionGroup.Actions.Count - 1) * idleTimeBetweenActions);
        PrometheusMetricCollector.AgvStateChargingGauge.WithLabels(agvId.ToString()).Value.ShouldBe(actionGroup.Actions.Count(a => a.ActionType == ActionType.Charge) * actionDuration);
        PrometheusMetricCollector.AgvStateBusyGauge.WithLabels(agvId.ToString()).Value.ShouldBe(actionGroup.Actions.Count(a => a.ActionType == ActionType.Move) * actionDuration);
    }

    [TestMethod]
    public async Task CollectOrderCompletionMetrics_ParkOrder_MetricsUpdated()
    {
        var agvId = Guid.NewGuid();
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        const double actionDuration = 36;
        const double idleTime = 42;
        const double idleTimeBetweenActions = 2;
        var startedDate = DateTime.UtcNow;

        var priorActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            pickActionState: ActionState.Completed,
            dropActionState: ActionState.Completed,
            assignedAgv: agvId);
        priorActionGroup.SetFixedDatesFromDurations(actionDuration, startedDate, idleTimeBetweenActions);
        startedDate = priorActionGroup.CompletedDate!.Value.AddSeconds(idleTime);

        var actionGroupCorrelationId = Guid.NewGuid();
        var actionGroup = new TransportOrder.Domain.Models.ActionGroup.ActionGroup(
            actionGroupCorrelationId,
            string.Empty,
            actions: new List<TransportOrder.Domain.Models.ActionGroup.AgvAction> { new(Guid.NewGuid(), ActionType.Move, AgvGraphGenerator.Prk1, actionGroupCorrelationId) },
            assignedAgv: agvId,
            creator: ActionGroupCreator.TransportOrder
        );
        actionGroup.Actions.First().Nodes = [AgvGraphGenerator.Src1, AgvGraphGenerator.Way1, AgvGraphGenerator.Prk1];
        actionGroup.SetFixedDatesFromDurations(actionDuration, startedDate, idleTimeBetweenActions);
        actionGroup.StartedDate = startedDate;

        var fixture = new PrometheusMetricCollectionServiceTestFixture()
            .AddGraph(graph)
            .AddActionGroups([priorActionGroup, actionGroup]);

        await fixture.Service.CollectOrderCompletionMetrics(actionGroup);

        PrometheusMetricCollector.AgvCompletedOrdersCounter.WithLabels(agvId.ToString()).Value.ShouldBe(0);
        PrometheusMetricCollector.AgvTravelDistanceGauge.WithLabels(agvId.ToString()).Value.ShouldBeGreaterThan(0);
        PrometheusMetricCollector.AgvStateBusyCargoGauge.WithLabels(agvId.ToString()).Value.ShouldBe(0);
        PrometheusMetricCollector.AgvStateIdleGauge.WithLabels(agvId.ToString()).Value.ShouldBe(idleTime);
        PrometheusMetricCollector.AgvStateChargingGauge.WithLabels(agvId.ToString()).Value.ShouldBe(0);
        PrometheusMetricCollector.AgvStateBusyGauge.WithLabels(agvId.ToString()).Value.ShouldBe(actionDuration);
    }

    [TestMethod]
    public async Task CollectOrderCompletionMetrics_ManualMoveOrder_MetricsUpdated()
    {
        var agvId = Guid.NewGuid();
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        const double actionDuration = 36;
        const double idleTime = 42;
        const double idleTimeBetweenActions = 2;
        var startedDate = DateTime.UtcNow;

        var priorActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            pickActionState: ActionState.Completed,
            dropActionState: ActionState.Completed,
            assignedAgv: agvId);
        priorActionGroup.SetFixedDatesFromDurations(actionDuration, startedDate, idleTimeBetweenActions);
        startedDate = priorActionGroup.CompletedDate!.Value.AddSeconds(idleTime);

        var actionGroupCorrelationId = Guid.NewGuid();
        var actionGroup = new TransportOrder.Domain.Models.ActionGroup.ActionGroup(
            actionGroupCorrelationId,
            string.Empty,
            actions: new List<TransportOrder.Domain.Models.ActionGroup.AgvAction> { new(Guid.NewGuid(), ActionType.Move, AgvGraphGenerator.Src1, actionGroupCorrelationId) },
            assignedAgv: agvId,
            creator: ActionGroupCreator.TransportOrder,
            isManual: true
        );
        actionGroup.Actions.First().Nodes = [AgvGraphGenerator.Prk1, AgvGraphGenerator.Way1, AgvGraphGenerator.Src1];
        actionGroup.SetFixedDatesFromDurations(actionDuration, startedDate, idleTimeBetweenActions);
        actionGroup.StartedDate = startedDate;

        var fixture = new PrometheusMetricCollectionServiceTestFixture()
            .AddGraph(graph)
            .AddActionGroups([priorActionGroup, actionGroup]);

        await fixture.Service.CollectOrderCompletionMetrics(actionGroup);

        PrometheusMetricCollector.AgvCompletedOrdersCounter.WithLabels(agvId.ToString()).Value.ShouldBe(0);
        PrometheusMetricCollector.AgvTravelDistanceGauge.WithLabels(agvId.ToString()).Value.ShouldBeGreaterThan(0);
        PrometheusMetricCollector.AgvStateBusyCargoGauge.WithLabels(agvId.ToString()).Value.ShouldBe(0);
        PrometheusMetricCollector.AgvStateIdleGauge.WithLabels(agvId.ToString()).Value.ShouldBe(idleTime);
        PrometheusMetricCollector.AgvStateChargingGauge.WithLabels(agvId.ToString()).Value.ShouldBe(0);
        PrometheusMetricCollector.AgvStateBusyGauge.WithLabels(agvId.ToString()).Value.ShouldBe(actionDuration);
    }

    [TestMethod]
    public void CollectSafetyFieldViolationMetrics_NoSafetyFieldViolations_MetricsNotUpdated()
    {
        var agv = Guid.NewGuid();

        var fixture = new PrometheusMetricCollectionServiceTestFixture();

        fixture.Service.CollectSafetyFieldViolationMetrics(agv, [], []);

        PrometheusMetricCollector.AgvSafetyFieldViolationCounter.WithLabels(agv.ToString()).Value.ShouldBe(0);
        PrometheusMetricCollector.AgvSafetyFieldDurationGauge.WithLabels(agv.ToString()).Value.ShouldBe(0);
    }

    [TestMethod]
    public void CollectSafetyFieldViolationMetrics_ValidData_MetricsUpdated()
    {
        var agv = Guid.NewGuid();
        const double timePassed = 60;
        var fieldViolation = new FieldViolation(
            agv,
            DateTime.UtcNow.AddSeconds(-timePassed)
        );
        var fixture = new PrometheusMetricCollectionServiceTestFixture();

        fixture.Service.CollectSafetyFieldViolationMetrics(agv, [fieldViolation], [fieldViolation]);

        PrometheusMetricCollector.AgvSafetyFieldViolationCounter.WithLabels(agv.ToString()).Value.ShouldBe(1);
        PrometheusMetricCollector.AgvSafetyFieldDurationGauge.WithLabels(agv.ToString()).Value.ShouldBeGreaterThanOrEqualTo(timePassed);
    }
}