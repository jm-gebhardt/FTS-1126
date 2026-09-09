using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Providers;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Routes.Generation;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.TrafficManagement.Routes.Generation;

[TestClass]
public class RouteGeneratorTest
{
    private static readonly AgvType _agvType = AgvTypeGenerator.GenerateAgvType();

    [TestMethod]
    public void CreateRoute_RequestWithoutTargetCandidates_ReturnsNull()
    {
        var startNode = RoutingResourceGenerator.CreateRoutingNode(_agvType);
        var request = CreateRequest(startNode, []);

        var route = CreateRouteGenerator().CreateRoute(request);

        route.ShouldBeNull();
    }

    [TestMethod]
    public void CreateRoute_StartNodeIsTargetNode_ReturnsRouteWithoutSteps()
    {
        var startNode = RoutingResourceGenerator.CreateRoutingNode(_agvType);
        var request = CreateRequest(startNode, [startNode]);

        var route = CreateRouteGenerator().CreateRoute(request);

        route.ShouldNotBeNull();
        route.Steps.ShouldBeEmpty();
        route.Agv.ShouldBeSameAs(request.Agv);
        route.ActionCorrelationId.ShouldBe(request.ActionCorrelationId);
    }

    [TestMethod]
    public void CreateRoute_ChainOfNodes_ReturnsStepsInTraversalOrder()
    {
        var startNode = RoutingResourceGenerator.CreateRoutingNode(_agvType, x: 0, y: 0);
        var middleNode = RoutingResourceGenerator.CreateRoutingNode(_agvType, x: 1, y: 0);
        var targetNode = RoutingResourceGenerator.CreateRoutingNode(_agvType, x: 2, y: 0);
        var firstEdge = RoutingResourceGenerator.CreateRoutingEdge(startNode, middleNode, 1);
        var secondEdge = RoutingResourceGenerator.CreateRoutingEdge(middleNode, targetNode, 1);
        var request = CreateRequest(startNode, [targetNode]);

        var route = CreateRouteGenerator().CreateRoute(request);

        route.ShouldNotBeNull();
        route.Steps.Select(step => step.RoutingEdge).ShouldBe([firstEdge, secondEdge]);
    }

    [TestMethod]
    public void CreateRoute_AlternativePaths_ReturnsPathWithShortestDrivingTime()
    {
        var startNode = RoutingResourceGenerator.CreateRoutingNode(_agvType);
        var detourNode = RoutingResourceGenerator.CreateRoutingNode(_agvType);
        var targetNode = RoutingResourceGenerator.CreateRoutingNode(_agvType);
        RoutingResourceGenerator.CreateRoutingEdge(startNode, targetNode, 10, maxSpeed: 1.0);
        var firstDetourEdge = RoutingResourceGenerator.CreateRoutingEdge(startNode, detourNode, 6, maxSpeed: 2.0);
        var secondDetourEdge = RoutingResourceGenerator.CreateRoutingEdge(detourNode, targetNode, 6, maxSpeed: 2.0);
        var request = CreateRequest(startNode, [targetNode]);

        var route = CreateRouteGenerator().CreateRoute(request);

        route.ShouldNotBeNull();
        route.Steps.Select(step => step.RoutingEdge).ShouldBe([firstDetourEdge, secondDetourEdge]);
        route.RemainingDrivingTime.ShouldBe(6.0, 1e-9);
    }

    [TestMethod]
    public void CreateRoute_EdgeWithoutMaxSpeed_UsesDefaultSpeedForDrivingTimeAndCost()
    {
        var startNode = RoutingResourceGenerator.CreateRoutingNode(_agvType);
        var targetNode = RoutingResourceGenerator.CreateRoutingNode(_agvType);
        RoutingResourceGenerator.CreateRoutingEdge(startNode, targetNode, 3, maxSpeed: null);
        var request = CreateRequest(startNode, [targetNode]);

        var route = CreateRouteGenerator().CreateRoute(request);

        route.ShouldNotBeNull();
        var step = route.Steps.ShouldHaveSingleItem();
        step.DrivingTime.ShouldBe(3.0 / 1.5, 1e-9);
        step.Cost.ShouldBe(step.DrivingTime, 1e-9);
    }

    [TestMethod]
    public void CreateRoute_TargetNotReachable_ReturnsNull()
    {
        var startNode = RoutingResourceGenerator.CreateRoutingNode(_agvType);
        var deadEndNode = RoutingResourceGenerator.CreateRoutingNode(_agvType);
        var targetNode = RoutingResourceGenerator.CreateRoutingNode(_agvType);
        RoutingResourceGenerator.CreateRoutingEdge(startNode, deadEndNode, 1);
        var request = CreateRequest(startNode, [targetNode]);

        var route = CreateRouteGenerator().CreateRoute(request);

        route.ShouldBeNull();
    }

    [TestMethod]
    public void CreateRoute_GraphWithCycle_TerminatesAndReturnsRouteWithoutRevisitedNodes()
    {
        var startNode = RoutingResourceGenerator.CreateRoutingNode(_agvType);
        var loopNode = RoutingResourceGenerator.CreateRoutingNode(_agvType);
        var targetNode = RoutingResourceGenerator.CreateRoutingNode(_agvType);
        var forwardEdge = RoutingResourceGenerator.CreateRoutingEdge(startNode, loopNode, 1);
        RoutingResourceGenerator.CreateRoutingEdge(loopNode, startNode, 1);
        var targetEdge = RoutingResourceGenerator.CreateRoutingEdge(loopNode, targetNode, 1);
        var request = CreateRequest(startNode, [targetNode]);

        var route = CreateRouteGenerator().CreateRoute(request);

        route.ShouldNotBeNull();
        route.Steps.Select(step => step.RoutingEdge).ShouldBe([forwardEdge, targetEdge]);
    }

    private static RouteCreationRequest CreateRequest(RoutingNode startNode, IReadOnlyCollection<RoutingNode> targetNodeCandidates)
    {
        return new RouteCreationRequest(new RoutingAgvStub(startNode), startNode, targetNodeCandidates, Guid.NewGuid(), []);
    }

    private static RouteGenerator CreateRouteGenerator()
    {
        return new RouteGenerator(NullLogger<RouteGenerator>.Instance);
    }

    private sealed class RoutingAgvStub(RoutingNode currentRoutingNode) : IRoutingAgv
    {
        public Guid CorrelationId { get; } = Guid.NewGuid();

        public RoutingNode? CurrentRoutingNode { get; } = currentRoutingNode;

        public bool IsInAutomaticMode => true;
    }
}
