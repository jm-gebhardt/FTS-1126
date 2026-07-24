using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Routing.Exceptions;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.Routing;

[TestClass]
public class RouteServiceTest
{
    [TestMethod]
    public async Task GetRoute_WhenAgvCurrentPositionIsDestinationNode_ReturnsRoute()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string node = AgvGraphGenerator.DestinationClose.Name;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: node);
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        var actualRoute = await fixture.RouteService.GetRouteAsync(agv, node.ToGuid());

        actualRoute.Costs.ShouldBe(0);
        actualRoute.Nodes.Select(n => n.Name).ShouldBe([node]);
    }

    [TestMethod]
    public async Task GetRoute_AsCalledInEstimateActionGroupDuration_IncompleteActionGroupWithoutDropTarget_ReturnsRouteUntilMoveActionTarget()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string node = AgvGraphGenerator.DestinationClose.Name;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: node);
        var agvData = AgvDataGenerator.GenerateAgvData(correlationId: agv.CorrelationId);
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget();
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        var actualRoute = await fixture.RouteService.GetRouteAsync(agv, actionGroup.GetTargets(), null, agvData);

        actualRoute.Costs.ShouldNotBe(0);
        actualRoute.Nodes.Last().Id.ShouldBe(actionGroup.Actions.Single(a => a.ActionType == ActionType.Move).Target.NodeId!.Value);
        actualRoute.Nodes.First().Name.ShouldBe(node);
    }

    [TestMethod]
    public async Task GetRoute_AsCalledInEstimateActionGroupDuration_IncompleteActionGroupWithoutPickTarget_ReturnsRouteUntilMoveActionTarget()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string node = AgvGraphGenerator.DestinationClose.Name;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: node);
        var agvData = AgvDataGenerator.GenerateAgvData(correlationId: agv.CorrelationId);
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutPickTarget();
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        var actualRoute = await fixture.RouteService.GetRouteAsync(agv, actionGroup.GetTargets(), null, agvData);

        actualRoute.Costs.ShouldNotBe(0);
        actualRoute.Nodes.Last().Name.ShouldBe(actionGroup.Actions.Single(a => a.ActionType == ActionType.Move).Target.Node!.Name);
        actualRoute.Nodes.First().Name.ShouldBe(node);
    }

    [TestMethod]
    public async Task GetRoute_WhenLastNodeIsUnknown_ThrowsException()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string destinationNode = agvGraph.Nodes.Skip(5).First().Name;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "unknown");
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        await Should.ThrowAsync<NodeNotFoundException>(async () => await fixture.RouteService.GetRouteAsync(agv, destinationNode.ToGuid()));
    }

    [TestMethod]
    public async Task GetRoute_WhenStartNodeIsUnknown_ThrowsException()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string destinationNode = agvGraph.Nodes.Skip(5).First().Name;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: agvGraph.Nodes.First().Name);
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        await Should.ThrowAsync<NodeNotFoundException>(async () => await fixture.RouteService.GetRouteAsync(agv, destinationNode.ToGuid(), "unknown".ToGuid()));
    }

    [TestMethod]
    public async Task GetRoute_WhenDestinationNodeIsUnknown_ThrowsException()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: agvGraph.Nodes.First().Name);
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        await Should.ThrowAsync<NodeNotFoundException>(async () => await fixture.RouteService.GetRouteAsync(agv, "unknown".ToGuid()));
    }

    [TestMethod]
    public async Task GetNodeWithLowestCostsAsync_MultipleNodes_ReturnsNodeWithLowestCosts()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string node = AgvGraphGenerator.Waypoint1.Name;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: node);
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        var actualRoute = await fixture.RouteService.GetNodeWithLowestCostsAsync(agv, AgvGraphGenerator.Nodes, It.IsAny<AgvData>());

        actualRoute.Name.ShouldBe(AgvGraphGenerator.Waypoint1.Name);
    }

    [TestMethod]
    public async Task GetNodeWithLowestCostsAsync_NoNodeReachable_Throws()
    {
        string node = AgvGraphGenerator.DestinationUnreachable.Name;
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(AgvGraphGenerator.Nodes.Where(n => n.Name != node).ToArray());
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: node);
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        await Should.ThrowAsync<RouteNotFoundException>(async () => await fixture.RouteService.GetNodeWithLowestCostsAsync(agv, AgvGraphGenerator.Nodes, It.IsAny<AgvData>()));
    }

    [TestMethod]
    public async Task GetAgvRouteClosestToTarget_ValidData_ReturnsAgvCorrelationId()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        var closeAgv = AgvGenerator.GenerateIdleAgv(1, lastNodeName: AgvGraphGenerator.Waypoint1.Name);
        var closeAgvData = AgvDataGenerator.GenerateAgvData(closeAgv.CorrelationId);
        var farAgv = AgvGenerator.GenerateIdleAgv(2, lastNodeName: AgvGraphGenerator.Waypoint2.Name);
        var farAgvData = AgvDataGenerator.GenerateAgvData(farAgv.CorrelationId);
        var fixture = new RouteServiceTestFixture()
            .WithAgvs([closeAgv, farAgv], [agvGraph, agvGraph])
            .WithBlockedQueueNodes([]);

        (await fixture.RouteService.GetAgvRouteClosestToTarget([(closeAgv, closeAgvData), (farAgv, farAgvData)], AgvGraphGenerator.Src1Node.Id)).agv.ShouldBe(closeAgv.CorrelationId);
    }

    [TestMethod]
    public async Task GetAgvRouteClosestToTarget_SkipAgvsStartingAtTarget_ReturnsAgvCorrelationId()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        var closeAgv = AgvGenerator.GenerateIdleAgv(1, lastNodeName: AgvGraphGenerator.Waypoint1.Name);
        var closeAgvData = AgvDataGenerator.GenerateAgvData(closeAgv.CorrelationId);

        var agvAtTarget = AgvGenerator.GenerateIdleAgv(2, lastNodeName: AgvGraphGenerator.Src1);
        var agvAtTargetData = AgvDataGenerator.GenerateAgvData(agvAtTarget.CorrelationId);

        var farAgv = AgvGenerator.GenerateIdleAgv(3, lastNodeName: AgvGraphGenerator.Waypoint2.Name);
        var farAgvData = AgvDataGenerator.GenerateAgvData(farAgv.CorrelationId);
        var fixture = new RouteServiceTestFixture()
            .WithAgvs([closeAgv, farAgv, agvAtTarget], [agvGraph, agvGraph, agvGraph])
            .WithBlockedQueueNodes([]);

        (await fixture.RouteService.GetAgvRouteClosestToTarget(
            [(closeAgv, closeAgvData), (farAgv, farAgvData), (agvAtTarget, agvAtTargetData)],
            AgvGraphGenerator.Src1Node.Id,
            true)).agv.ShouldBe(closeAgv.CorrelationId);
    }

    // 0 ---- 1 ---- 2 ---- 3 ---- 4 ---- 5 ---- //
    // |                                         //
    // 1                   Src2          Src3    //
    // |                    |    Dbl1            //
    // 2            Src1    |   /                //
    // |             |      |  /                 //
    // 3     Prk1---Way1---Way2----------Prk2    //
    // |             |      |                    //
    // 4     Crg1---Way3---Way4----------Crg2    //
    // |             |      |                    //
    // 5            Dst1    |                    //
    // |                    |                    //
    // 6                   Dst2          Dst3    //
    // |                                         //
    ///////////////////////////////////////////////
    [TestMethod]
    public async Task GetAgvRouteClosestToTarget_SkipAgvsPassingQueue_ReturnsAgvCorrelationId()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        var closeAgv = AgvGenerator.GenerateIdleAgv(1, lastNodeName: AgvGraphGenerator.Waypoint1.Name);
        var closeAgvData = AgvDataGenerator.GenerateAgvData(closeAgv.CorrelationId);

        var agvPassingQueue = AgvGenerator.GenerateIdleAgv(2, lastNodeName: AgvGraphGenerator.ChargingFar.Name);
        var agvPassingQueueData = AgvDataGenerator.GenerateAgvData(agvPassingQueue.CorrelationId);

        var agvAtQueueHead = AgvGenerator.GenerateIdleAgv(3, lastNodeName: AgvGraphGenerator.Waypoint3.Name);
        var agvAtQueueHeadData = AgvDataGenerator.GenerateAgvData(agvPassingQueue.CorrelationId);

        var farAgv = AgvGenerator.GenerateIdleAgv(4, lastNodeName: AgvGraphGenerator.Waypoint2.Name);
        var farAgvData = AgvDataGenerator.GenerateAgvData(farAgv.CorrelationId);
        var fixture = new RouteServiceTestFixture()
            .WithAgvs([closeAgv, farAgv, agvPassingQueue, agvAtQueueHead], [agvGraph, agvGraph, agvGraph, agvGraph])
            .WithBlockedQueueNodes([AgvGraphGenerator.Waypoint3.Name, AgvGraphGenerator.Waypoint4.Name]);

        (await fixture.RouteService.GetAgvRouteClosestToTarget(
            [(closeAgv, closeAgvData), (farAgv, farAgvData), (agvPassingQueue, agvPassingQueueData), (agvAtQueueHead, agvAtQueueHeadData)],
            AgvGraphGenerator.Src1Node.Id,
            true)).agv.ShouldBe(closeAgv.CorrelationId);
    }

    [TestMethod]
    public async Task GetAgvRouteClosestToTarget_AgvAtQueueHeadIsClosest_ReturnsAgvCorrelationId()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();

        var agvPassingQueue = AgvGenerator.GenerateIdleAgv(2, lastNodeName: AgvGraphGenerator.ChargingFar.Name);
        var agvPassingQueueData = AgvDataGenerator.GenerateAgvData(agvPassingQueue.CorrelationId);

        var agvAtQueueHead = AgvGenerator.GenerateIdleAgv(3, lastNodeName: AgvGraphGenerator.Waypoint3.Name);
        var agvAtQueueHeadData = AgvDataGenerator.GenerateAgvData(agvPassingQueue.CorrelationId);

        var farAgv = AgvGenerator.GenerateIdleAgv(4, lastNodeName: AgvGraphGenerator.Waypoint2.Name);
        var farAgvData = AgvDataGenerator.GenerateAgvData(farAgv.CorrelationId);
        var fixture = new RouteServiceTestFixture()
            .WithAgvs([farAgv, agvPassingQueue, agvAtQueueHead], [agvGraph, agvGraph, agvGraph])
            .WithBlockedQueueNodes([AgvGraphGenerator.Waypoint3.Name, AgvGraphGenerator.Waypoint4.Name]);

        (await fixture.RouteService.GetAgvRouteClosestToTarget(
            [(farAgv, farAgvData), (agvPassingQueue, agvPassingQueueData), (agvAtQueueHead, agvAtQueueHeadData)],
            AgvGraphGenerator.Src1Node.Id,
            true)).agv.ShouldBe(agvAtQueueHead.CorrelationId);
    }

    [TestMethod]
    public async Task GetAgvRouteClosestToTarget_TargetNotReachable_ReturnsNull()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        var closeAgv = AgvGenerator.GenerateIdleAgv(1, lastNodeName: AgvGraphGenerator.Waypoint1.Name);
        var closeAgvData = AgvDataGenerator.GenerateAgvData(closeAgv.CorrelationId);
        var farAgv = AgvGenerator.GenerateIdleAgv(2, lastNodeName: AgvGraphGenerator.Waypoint2.Name);
        var farAgvData = AgvDataGenerator.GenerateAgvData(farAgv.CorrelationId);
        var fixture = new RouteServiceTestFixture()
            .WithAgvs([closeAgv, farAgv], [agvGraph, agvGraph])
            .WithBlockedQueueNodes([]);

        (await fixture.RouteService.GetAgvRouteClosestToTarget([(closeAgv, closeAgvData), (farAgv, farAgvData)], AgvGraphGenerator.DestinationUnreachable.Id)).ShouldBe((null, null));
    }

    [TestMethod]
    public async Task GetCostForFirstActionAsync_PickDropActionGroup_ReturnsActionCosts()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string node = AgvGraphGenerator.Waypoint1.Name;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: node);
        var data = AgvDataGenerator.GenerateAgvData(correlationId: agv.CorrelationId);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        double? costs = await fixture.RouteService.GetCostsForFirstActionAsync(agv, actionGroup, data, CancellationToken.None);
        costs.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task GetCostForFirstActionAsync_IncompletePickDropActionGroupWithoutDropTarget_ReturnsActionCosts()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string node = AgvGraphGenerator.Waypoint1.Name;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: node);
        var data = AgvDataGenerator.GenerateAgvData(correlationId: agv.CorrelationId);
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget();

        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        double? costs = await fixture.RouteService.GetCostsForFirstActionAsync(agv, actionGroup, data, CancellationToken.None);
        costs.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task GetCostForFirstActionAsync_IncompletePickDropActionGroupWithoutPickTarget_ReturnsActionCosts()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string node = AgvGraphGenerator.Waypoint1.Name;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: node);
        var data = AgvDataGenerator.GenerateAgvData(correlationId: agv.CorrelationId);
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutPickTarget();

        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        double? costs = await fixture.RouteService.GetCostsForFirstActionAsync(agv, actionGroup, data, CancellationToken.None);
        costs.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task GetCostsForFirstActionNotPassingNodesAsync_PickDropActionGroupNoAvoidedNodes_ReturnsActionCosts()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string node = AgvGraphGenerator.Waypoint1.Name;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: node);
        var data = AgvDataGenerator.GenerateAgvData(correlationId: agv.CorrelationId);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        double? costs = await fixture.RouteService.GetCostsForFirstActionNotPassingNodesAsync(agv, actionGroup, data, [], CancellationToken.None);
        costs.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task GetCostsForFirstActionNotPassingNodesAsync_NodesToAvoidOnRoute_ReturnsNull()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string node = AgvGraphGenerator.Waypoint1.Name;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: node);
        var data = AgvDataGenerator.GenerateAgvData(correlationId: agv.CorrelationId);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        double? costs = await fixture.RouteService.GetCostsForFirstActionNotPassingNodesAsync(agv, actionGroup, data, [actionGroup.Actions.First().Target.NodeId!.Value], CancellationToken.None);
        costs.ShouldBeNull();
    }

    [TestMethod]
    public async Task HasAnyAgvRouteAsync_WhenCancelled_PropagatesCancellationWithoutLoggingFailure()
    {
        // Cancellation (a caller abort, or the internal cancel once another AGV already has a route) must never be
        // logged as a route calculation failure. A caller cancellation is surfaced rather than reported as "no route".
        var firstAgv = AgvGenerator.GenerateIdleAgv(1);
        var secondAgv = AgvGenerator.GenerateIdleAgv(2);
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(AgvGraphGenerator.ChargingClose.Name);
        var fixture = new RouteServiceTestFixture();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await fixture.RouteService.HasAnyAgvRouteAsync(actionGroup, [firstAgv, secondAgv], new CancellationToken(true)));

        fixture.LoggingService.Verify(x => x.LogAgv(It.IsAny<ILogger>(), LogLevel.Error, LogCodes.RouteCalculationFailed,
            It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object?>?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task HasAnyAgvRouteAsync_WhenRouteCalculationThrows_LogsRouteCalculationFailed()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        var agv = AgvGenerator.GenerateIdleAgv(1, lastNodeName: AgvGraphGenerator.ChargingClose.Name);
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup("unknownNode");
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        bool hasRoute = await fixture.RouteService.HasAnyAgvRouteAsync(actionGroup, [agv], CancellationToken.None);

        hasRoute.ShouldBeFalse();
        fixture.LoggingService.Verify(x => x.LogAgv(It.IsAny<ILogger>(), LogLevel.Error, LogCodes.RouteCalculationFailed,
            agv.CorrelationId, It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object?>?>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}