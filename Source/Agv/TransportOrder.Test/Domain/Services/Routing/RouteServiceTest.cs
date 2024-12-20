using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Routing.Exceptions;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    public async Task GetAgvClosestToTarget_ValidData_ReturnsAgvCorrelationId()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        var closeAgv = AgvGenerator.GenerateIdleAgv(lastNodeId: AgvGraphGenerator.Waypoint1.Id);
        var closeAgvData = AgvDataGenerator.GenerateAgvData(closeAgv.CorrelationId);
        var farAgv = AgvGenerator.GenerateIdleAgv(lastNodeId: AgvGraphGenerator.Waypoint2.Id);
        var farAgvData = AgvDataGenerator.GenerateAgvData(farAgv.CorrelationId);
        var fixture = new RouteServiceTestFixture()
            .WithAgvs([closeAgv, farAgv], [agvGraph, agvGraph]);

        (await fixture.RouteService.GetAgvClosestToTarget([(closeAgv, closeAgvData), (farAgv, farAgvData)], AgvGraphGenerator.Src1)).ShouldBe(closeAgv.CorrelationId);
    }

    [TestMethod]
    public async Task GetAgvClosestToTarget_TargetNotReachable_ReturnsNull()
    {
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        var closeAgv = AgvGenerator.GenerateIdleAgv(lastNodeId: AgvGraphGenerator.Waypoint1.Id);
        var closeAgvData = AgvDataGenerator.GenerateAgvData(closeAgv.CorrelationId);
        var farAgv = AgvGenerator.GenerateIdleAgv(lastNodeId: AgvGraphGenerator.Waypoint2.Id);
        var farAgvData = AgvDataGenerator.GenerateAgvData(farAgv.CorrelationId);
        var fixture = new RouteServiceTestFixture()
            .WithAgvs([closeAgv, farAgv], [agvGraph, agvGraph]);

        (await fixture.RouteService.GetAgvClosestToTarget([(closeAgv, closeAgvData), (farAgv, farAgvData)], AgvGraphGenerator.DestinationUnreachable.Id)).ShouldBeNull();
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