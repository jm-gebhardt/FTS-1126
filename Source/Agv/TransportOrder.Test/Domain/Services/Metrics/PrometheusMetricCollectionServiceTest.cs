using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;
using Gebhardt.Acc.TransportOrder.Test.Extensions;
using Gebhardt.Acc.TransportOrder.Test.Helper;
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
            actions: new List<TransportOrder.Domain.Models.ActionGroup.AgvAction> { new(Guid.NewGuid(), ActionType.Move, AgvGraphGenerator.Prk1Node, actionGroupCorrelationId) },
            assignedAgv: agvId,
            creator: ActionGroupCreator.TransportOrder,
            priority: Priority.Default
        );
        GraphNode[] nodes = [AgvGraphGenerator.Src1Node, AgvGraphGenerator.Way1Node, AgvGraphGenerator.Prk1Node];
        actionGroup.Actions.First().Nodes = nodes;
        actionGroup.Actions.First().Edges = graph.GetEdgesForNodes(nodes);

        var fixture = new PrometheusMetricCollectionServiceTestFixture()
            .AddGraph(graph);

        await fixture.Service.CollectTravelDistance(actionGroup, agvId, null);
        double expectedRouteLength = graph.GetRouteLength([AgvGraphGenerator.Src1Node, AgvGraphGenerator.Way1Node, AgvGraphGenerator.Prk1Node]);
        PrometheusMetricCollector.AgvTravelDistanceGauge.WithLabels(agvId.ToString()).Value.ShouldBe(expectedRouteLength);
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
            actions: new List<TransportOrder.Domain.Models.ActionGroup.AgvAction> { new(Guid.NewGuid(), ActionType.Move, AgvGraphGenerator.Prk1Node, actionGroupCorrelationId) },
            assignedAgv: agvId,
            creator: ActionGroupCreator.TransportOrder
        );
        GraphNode[] nodes = [AgvGraphGenerator.Src1Node, AgvGraphGenerator.Way1Node, AgvGraphGenerator.Prk1Node];
        actionGroup.Actions.First().Nodes = nodes;
        actionGroup.Actions.First().Edges = graph.GetEdgesForNodes(nodes);
        var fixture = new PrometheusMetricCollectionServiceTestFixture()
            .AddGraph(graph);

        await fixture.Service.CollectTravelDistance(actionGroup, agvId, AgvGraphGenerator.Way1Node.Id);
        PrometheusMetricCollector.AgvTravelDistanceGauge.WithLabels(agvId.ToString()).Value.ShouldBe(graph.GetRouteLength([AgvGraphGenerator.Src1Node, AgvGraphGenerator.Way1Node]));
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
        GraphNode[] nodes = [AgvGraphGenerator.Prk1Node, AgvGraphGenerator.Way1Node, AgvGraphGenerator.Src1Node];
        actionGroup.Actions.First().Nodes = nodes;
        actionGroup.Actions.First().Edges = graph.GetEdgesForNodes(nodes);
        nodes = [AgvGraphGenerator.Src1Node, AgvGraphGenerator.Way1Node, AgvGraphGenerator.Way3Node, AgvGraphGenerator.Dst1Node];
        actionGroup.Actions.Last().Nodes = nodes;
        actionGroup.Actions.Last().Edges = graph.GetEdgesForNodes(nodes);
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
    [TestMethod]
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
        GraphNode[] nodes = [AgvGraphGenerator.Prk1Node, AgvGraphGenerator.Way1Node, AgvGraphGenerator.Way3Node, AgvGraphGenerator.Crg1Node];
        actionGroup.Actions.First().Nodes = nodes;
        actionGroup.Actions.First().Edges = graph.GetEdgesForNodes(nodes);
        actionGroup.Actions.Last().Nodes = [AgvGraphGenerator.Crg1Node];
        actionGroup.Actions.Last().Edges = [];
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
    public async Task CollectOrderCompletionMetrics_AbortedOrder_MetricsUpdated()
    {
        var agvId = Guid.NewGuid();
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        var startedDate = DateTime.UtcNow;

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            pickActionState: ActionState.Aborted,
            dropActionState: ActionState.Created,
            assignedAgv: agvId);
        actionGroup.SetFixedDatesFromDurations(null, startedDate, null);

        var fixture = new PrometheusMetricCollectionServiceTestFixture()
            .AddGraph(graph)
            .AddActionGroups([actionGroup]);

        await fixture.Service.CollectOrderCompletionMetrics(actionGroup);

        PrometheusMetricCollector.AgvCompletedOrdersCounter.WithLabels(agvId.ToString()).Value.ShouldBe(0);
        PrometheusMetricCollector.AgvStateBusyCargoGauge.WithLabels(agvId.ToString()).Value.ShouldBe(0);
        PrometheusMetricCollector.AgvStateIdleGauge.WithLabels(agvId.ToString()).Value.ShouldBe(0);
        PrometheusMetricCollector.AgvStateChargingGauge.WithLabels(agvId.ToString()).Value.ShouldBe(0);
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
            actions: new List<TransportOrder.Domain.Models.ActionGroup.AgvAction> { new(Guid.NewGuid(), ActionType.Move, AgvGraphGenerator.Prk1Node, actionGroupCorrelationId) },
            assignedAgv: agvId,
            creator: ActionGroupCreator.TransportOrder
        );
        GraphNode[] nodes = [AgvGraphGenerator.Src1Node, AgvGraphGenerator.Way1Node, AgvGraphGenerator.Prk1Node];
        actionGroup.Actions.First().Nodes = nodes;
        actionGroup.Actions.First().Edges = graph.GetEdgesForNodes(nodes);
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
            actions: new List<TransportOrder.Domain.Models.ActionGroup.AgvAction> { new(Guid.NewGuid(), ActionType.Move, AgvGraphGenerator.Src1Node, actionGroupCorrelationId) },
            assignedAgv: agvId,
            creator: ActionGroupCreator.TransportOrder,
            isManual: true
        );
        GraphNode[] nodes = [AgvGraphGenerator.Prk1Node, AgvGraphGenerator.Way1Node, AgvGraphGenerator.Src1Node];
        actionGroup.Actions.First().Nodes = nodes;
        actionGroup.Actions.First().Edges = graph.GetEdgesForNodes(nodes);
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

    [TestMethod]
    public void CollectJamStateMetrics_PalletJam_CountUpdated()
    {
        var fixture = new PrometheusMetricCollectionServiceTestFixture();
        var now = DateTime.UtcNow;
        var station = Guid.NewGuid();
        fixture.Service.CollectJamStateMetrics(station, now.AddMinutes(-1), false, now, true);

        PrometheusMetricCollector.StationJamStateCounter.WithLabels(station.ToString()).Value.ShouldBe(1);
        PrometheusMetricCollector.StationJamStateDurationGauge.WithLabels(station.ToString()).Value.ShouldBe(0);
    }

    [TestMethod]
    public void CollectJamStateMetrics_PalletJamResolved_DurationUpdated()
    {
        var fixture = new PrometheusMetricCollectionServiceTestFixture();
        var now = DateTime.UtcNow;
        const int duration = 60;
        var station = Guid.NewGuid();
        fixture.Service.CollectJamStateMetrics(station, now.AddSeconds(-duration), true, now, false);

        PrometheusMetricCollector.StationJamStateCounter.WithLabels(station.ToString()).Value.ShouldBe(0);
        PrometheusMetricCollector.StationJamStateDurationGauge.WithLabels(station.ToString()).Value.ShouldBe(duration);
    }

    [DataRow(ActionType.Charge, 0, 0, 1)]
    [DataRow(ActionType.Pick, 0, 1, 0)]
    [DataRow(ActionType.Drop, 1, 0, 0)]
    [TestMethod]
    public async Task CollectStationOrderCompletionMetrics_WithStation_CountersUpdated(ActionType actionType, int dropCount, int pickCount, int chargeCount)
    {
        var station = new Station { Id = Guid.NewGuid() };
        var fixture = new PrometheusMetricCollectionServiceTestFixture()
            .WithStation(station);

        await fixture.Service.CollectStationOrderCompletionMetrics(actionType, station.Id);

        PrometheusMetricCollector.StationChargeCounter.WithLabels(station.Id.ToString()).Value.ShouldBe(chargeCount);
        PrometheusMetricCollector.StationPickCounter.WithLabels(station.Id.ToString()).Value.ShouldBe(pickCount);
        PrometheusMetricCollector.StationDropCounter.WithLabels(station.Id.ToString()).Value.ShouldBe(dropCount);
    }
}