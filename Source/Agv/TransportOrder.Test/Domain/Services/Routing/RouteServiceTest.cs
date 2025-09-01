using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Routing.Exceptions;
using Gebhardt.Acc.TransportOrder.Test.Helper;
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
        string node = AgvGraphGenerator.DestinationClose.Id;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: node);
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        var actualRoute = await fixture.RouteService.GetRouteAsync(agv, node);

        actualRoute.Costs.ShouldBe(0);
        actualRoute.Nodes.ShouldBe(new[] { node });
    }

    [TestMethod]
    public async Task GetRoute_AsCalledInEstimateActionGroupDuration_IncompleteActionGroupWithoutDropTarget_ReturnsRouteUntilMoveActionTarget()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string node = AgvGraphGenerator.DestinationClose.Id;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: node);
        var agvData = AgvDataGenerator.GenerateAgvData(correlationId: agv.CorrelationId);
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutDropTarget();
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        var actualRoute = await fixture.RouteService.GetRouteAsync(agv, actionGroup.GetTargets(), null, agvData);

        actualRoute.Costs.ShouldNotBe(0);
        actualRoute.Nodes.Last().ShouldBe(actionGroup.Actions.Single(a => a.ActionType == ActionType.Move).Target.NodeId);
        actualRoute.Nodes.First().ShouldBe(node);
    }

    [TestMethod]
    public async Task GetRoute_AsCalledInEstimateActionGroupDuration_IncompleteActionGroupWithoutPickTarget_ReturnsRouteUntilMoveActionTarget()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string node = AgvGraphGenerator.DestinationClose.Id;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: node);
        var agvData = AgvDataGenerator.GenerateAgvData(correlationId: agv.CorrelationId);
        var actionGroup = AgvActionGenerator.GenerateIncompletePickDropActionGroupWithoutPickTarget();
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        var actualRoute = await fixture.RouteService.GetRouteAsync(agv, actionGroup.GetTargets(), null, agvData);

        actualRoute.Costs.ShouldNotBe(0);
        actualRoute.Nodes.Last().ShouldBe(actionGroup.Actions.Single(a => a.ActionType == ActionType.Move).Target.NodeId);
        actualRoute.Nodes.First().ShouldBe(node);
    }

    [TestMethod]
    public async Task GetRoute_WhenLastNodeIsUnknown_ThrowsException()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string destinationNode = agvGraph.Nodes.Skip(5).First().Id;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: "unknown");
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        await Should.ThrowAsync<NodeNotFoundException>(async () => await fixture.RouteService.GetRouteAsync(agv, destinationNode));
    }

    [TestMethod]
    public async Task GetRoute_WhenStartNodeIsUnknown_ThrowsException()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string destinationNode = agvGraph.Nodes.Skip(5).First().Id;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: agvGraph.Nodes.First().Id);
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        await Should.ThrowAsync<NodeNotFoundException>(async () => await fixture.RouteService.GetRouteAsync(agv, destinationNode, "unknown"));
    }

    [TestMethod]
    public async Task GetRoute_WhenDestinationNodeIsUnknown_ThrowsException()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: agvGraph.Nodes.First().Id);
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        await Should.ThrowAsync<NodeNotFoundException>(async () => await fixture.RouteService.GetRouteAsync(agv, "unknown"));
    }

    [TestMethod]
    public async Task GetNodeWithLowestCostsAsync_MultipleNodes_ReturnsNodeWithLowestCosts()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        string node = AgvGraphGenerator.Waypoint1.Id;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: node);
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        var actualRoute = await fixture.RouteService.GetNodeWithLowestCostsAsync(agv, AgvGraphGenerator.Nodes, It.IsAny<AgvData>());

        actualRoute.Id.ShouldBe(AgvGraphGenerator.Waypoint1.Id);
    }

    [TestMethod]
    public async Task GetNodeWithLowestCostsAsync_NoNodeReachable_Throws()
    {
        string node = AgvGraphGenerator.DestinationUnreachable.Id;
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(AgvGraphGenerator.Nodes.Where(n => n.Id != node).ToArray());
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: node);
        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        await Should.ThrowAsync<RouteNotFoundException>(async () => await fixture.RouteService.GetNodeWithLowestCostsAsync(agv, AgvGraphGenerator.Nodes, It.IsAny<AgvData>()));
    }

    [TestMethod]
    public async Task GetAgvRouteClosestToTarget_ValidData_ReturnsAgvCorrelationId()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        var closeAgv = AgvGenerator.GenerateIdleAgv(1, lastNodeId: AgvGraphGenerator.Waypoint1.Id);
        var closeAgvData = AgvDataGenerator.GenerateAgvData(closeAgv.CorrelationId);
        var farAgv = AgvGenerator.GenerateIdleAgv(2, lastNodeId: AgvGraphGenerator.Waypoint2.Id);
        var farAgvData = AgvDataGenerator.GenerateAgvData(farAgv.CorrelationId);
        var fixture = new RouteServiceTestFixture()
            .WithAgvs([closeAgv, farAgv], [agvGraph, agvGraph])
            .WithBlockedQueueNodes([]);

        (await fixture.RouteService.GetAgvRouteClosestToTarget([(closeAgv, closeAgvData), (farAgv, farAgvData)], AgvGraphGenerator.Src1)).agv.ShouldBe(closeAgv.CorrelationId);
    }

    [TestMethod]
    public async Task GetAgvRouteClosestToTarget_SkipAgvsStartingAtTarget_ReturnsAgvCorrelationId()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        var closeAgv = AgvGenerator.GenerateIdleAgv(1, lastNodeId: AgvGraphGenerator.Waypoint1.Id);
        var closeAgvData = AgvDataGenerator.GenerateAgvData(closeAgv.CorrelationId);

        var agvAtTarget = AgvGenerator.GenerateIdleAgv(2, lastNodeId: AgvGraphGenerator.Src1);
        var agvAtTargetData = AgvDataGenerator.GenerateAgvData(agvAtTarget.CorrelationId);

        var farAgv = AgvGenerator.GenerateIdleAgv(3, lastNodeId: AgvGraphGenerator.Waypoint2.Id);
        var farAgvData = AgvDataGenerator.GenerateAgvData(farAgv.CorrelationId);
        var fixture = new RouteServiceTestFixture()
            .WithAgvs([closeAgv, farAgv, agvAtTarget], [agvGraph, agvGraph, agvGraph])
            .WithBlockedQueueNodes([]);

        (await fixture.RouteService.GetAgvRouteClosestToTarget(
            [(closeAgv, closeAgvData), (farAgv, farAgvData), (agvAtTarget, agvAtTargetData)],
            AgvGraphGenerator.Src1,
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
        var closeAgv = AgvGenerator.GenerateIdleAgv(1, lastNodeId: AgvGraphGenerator.Waypoint1.Id);
        var closeAgvData = AgvDataGenerator.GenerateAgvData(closeAgv.CorrelationId);

        var agvPassingQueue = AgvGenerator.GenerateIdleAgv(2, lastNodeId: AgvGraphGenerator.ChargingFar.Id);
        var agvPassingQueueData = AgvDataGenerator.GenerateAgvData(agvPassingQueue.CorrelationId);

        var agvAtQueueHead = AgvGenerator.GenerateIdleAgv(3, lastNodeId: AgvGraphGenerator.Waypoint3.Id);
        var agvAtQueueHeadData = AgvDataGenerator.GenerateAgvData(agvPassingQueue.CorrelationId);

        var farAgv = AgvGenerator.GenerateIdleAgv(4, lastNodeId: AgvGraphGenerator.Waypoint2.Id);
        var farAgvData = AgvDataGenerator.GenerateAgvData(farAgv.CorrelationId);
        var fixture = new RouteServiceTestFixture()
            .WithAgvs([closeAgv, farAgv, agvPassingQueue, agvAtQueueHead], [agvGraph, agvGraph, agvGraph, agvGraph])
            .WithBlockedQueueNodes([AgvGraphGenerator.Waypoint3.Id, AgvGraphGenerator.Waypoint4.Id]);

        (await fixture.RouteService.GetAgvRouteClosestToTarget(
            [(closeAgv, closeAgvData), (farAgv, farAgvData), (agvPassingQueue, agvPassingQueueData), (agvAtQueueHead, agvAtQueueHeadData)],
            AgvGraphGenerator.Src1,
            true)).agv.ShouldBe(closeAgv.CorrelationId);
    }

    [TestMethod]
    public async Task GetAgvRouteClosestToTarget_AgvAtQueueHeadIsClosest_ReturnsAgvCorrelationId()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();

        var agvPassingQueue = AgvGenerator.GenerateIdleAgv(2, lastNodeId: AgvGraphGenerator.ChargingFar.Id);
        var agvPassingQueueData = AgvDataGenerator.GenerateAgvData(agvPassingQueue.CorrelationId);

        var agvAtQueueHead = AgvGenerator.GenerateIdleAgv(3, lastNodeId: AgvGraphGenerator.Waypoint3.Id);
        var agvAtQueueHeadData = AgvDataGenerator.GenerateAgvData(agvPassingQueue.CorrelationId);

        var farAgv = AgvGenerator.GenerateIdleAgv(4, lastNodeId: AgvGraphGenerator.Waypoint2.Id);
        var farAgvData = AgvDataGenerator.GenerateAgvData(farAgv.CorrelationId);
        var fixture = new RouteServiceTestFixture()
            .WithAgvs([farAgv, agvPassingQueue, agvAtQueueHead], [agvGraph, agvGraph, agvGraph])
            .WithBlockedQueueNodes([AgvGraphGenerator.Waypoint3.Id, AgvGraphGenerator.Waypoint4.Id]);

        (await fixture.RouteService.GetAgvRouteClosestToTarget(
            [(farAgv, farAgvData), (agvPassingQueue, agvPassingQueueData), (agvAtQueueHead, agvAtQueueHeadData)],
            AgvGraphGenerator.Src1,
            true)).agv.ShouldBe(agvAtQueueHead.CorrelationId);
    }

    [TestMethod]
    public async Task GetAgvRouteClosestToTarget_TargetNotReachable_ReturnsNull()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        var closeAgv = AgvGenerator.GenerateIdleAgv(1, lastNodeId: AgvGraphGenerator.Waypoint1.Id);
        var closeAgvData = AgvDataGenerator.GenerateAgvData(closeAgv.CorrelationId);
        var farAgv = AgvGenerator.GenerateIdleAgv(2, lastNodeId: AgvGraphGenerator.Waypoint2.Id);
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
        string node = AgvGraphGenerator.Waypoint1.Id;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: node);
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
        string node = AgvGraphGenerator.Waypoint1.Id;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: node);
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
        string node = AgvGraphGenerator.Waypoint1.Id;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: node);
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
        string node = AgvGraphGenerator.Waypoint1.Id;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: node);
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
        string node = AgvGraphGenerator.Waypoint1.Id;
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: node);
        var data = AgvDataGenerator.GenerateAgvData(correlationId: agv.CorrelationId);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        var fixture = new RouteServiceTestFixture()
            .WithAgv(agv, agvGraph);

        double? costs = await fixture.RouteService.GetCostsForFirstActionNotPassingNodesAsync(agv, actionGroup, data, [actionGroup.Actions.First().Target.NodeId!], CancellationToken.None);
        costs.ShouldBeNull();
    }
}