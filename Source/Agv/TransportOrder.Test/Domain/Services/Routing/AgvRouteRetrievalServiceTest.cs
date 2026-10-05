using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.Test.Repositories;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.Routing;

[TestClass]
public class AgvRouteRetrievalServiceTest
{
    [TestMethod]
    public async Task GetAgvRouteAsync_WhenLastNodeIdIsNull_ReturnsAgvRouteBasedOnAgvsLastNodeId()
    {
        var route = AgvGraphGenerator.CreateRoute();
        var node = route.nodes[1];
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: node.Name);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(ActionState.Started, assignedAgv: agv.CorrelationId);
        var action = actionGroup.Actions.First();
        action.Nodes = route.nodes;
        action.Edges = route.edges;
        var fixture = await new AgvRouteServiceTestFixture()
            .WithAgv(agv)
            .WithActionGroup(actionGroup)
            .BuildAsync();

        var actualRoute = await fixture.AgvRouteRetrievalService.GetAgvRouteAsync(agv.CorrelationId);

        actualRoute.ShouldNotBeNull();
        actualRoute.RemainingNodes.ShouldBe(route.nodes.Skip(1).ToArray());
        actualRoute.RemainingEdges.ShouldBe(route.edges.Skip(1).ToArray());
    }

    [TestMethod]
    public async Task GetAgvRouteAsync_WhenLastNodeIdIsInMiddleOfRoute_ReturnsRemainingNodes()
    {
        var route = AgvGraphGenerator.CreateRoute();
        var node = route.nodes[1];
        var agv = AgvGenerator.GenerateIdleAgv();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: ActionState.Started);
        var action = actionGroup.Actions.First();
        action.Nodes = route.nodes;
        action.Edges = route.edges;
        var fixture = await new AgvRouteServiceTestFixture()
            .WithAgv(agv)
            .WithActionGroup(actionGroup)
            .BuildAsync();

        var actualRoute = await fixture.AgvRouteRetrievalService.GetAgvRouteAsync(agv.CorrelationId, node.Id);

        actualRoute.ShouldNotBeNull();
        actualRoute.RemainingNodes.ShouldBe(route.nodes.Skip(1).ToArray());
        actualRoute.RemainingEdges.ShouldBe(route.edges.Skip(1).ToArray());
    }

    [TestMethod]
    public async Task GetAgvRouteAsync_WhenAgvCorrelationIdIsInvalid_ReturnsNull()
    {
        var invalidAgvCorrelationId = Guid.NewGuid();
        var fixture = new AgvRouteServiceTestFixture();

        var actualRoute = await fixture.AgvRouteRetrievalService.GetAgvRouteAsync(invalidAgvCorrelationId);

        actualRoute.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetAgvRouteAsync_MultipleAgvs_ReturnsDictionaryWithAgvRoutes()
    {
        var route1 = AgvGraphGenerator.CreateRoute();
        var route2 = AgvGraphGenerator.CreateRoute(AgvGraphGenerator.Dst1Node);
        var agv1 = AgvGenerator.GenerateIdleAgv(lastNodeName: route1.nodes[1].Name, index: 1);
        var agv2 = AgvGenerator.GenerateIdleAgv(lastNodeName: route2.nodes[1].Name, index: 2);

        var actionGroup1 = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv1.CorrelationId, pickActionState: ActionState.Started);
        var action1 = actionGroup1.Actions.First();
        action1.Nodes = route1.nodes;
        action1.Edges = route1.edges;

        var actionGroup2 = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv2.CorrelationId, pickActionState: ActionState.Started);
        var action2 = actionGroup2.Actions.First();
        action2.Nodes = route2.nodes;
        action2.Edges = route2.edges;
        var fixture = await new AgvRouteServiceTestFixture()
            .WithAgv(agv1)
            .WithAgv(agv2)
            .WithActionGroup(actionGroup1)
            .WithActionGroup(actionGroup2)
            .BuildAsync();

        var actualRoutes = (await fixture.AgvRouteRetrievalService.GetAgvRouteAsync(new List<Guid> { agv1.CorrelationId, agv2.CorrelationId })).ToList();

        actualRoutes.ShouldNotBeNull();
        actualRoutes.Count.ShouldBe(2);
        actualRoutes.Find(r => r.CorrelationId == agv1.CorrelationId).ShouldNotBeNull();
        actualRoutes.Find(r => r.CorrelationId == agv1.CorrelationId)!.RemainingNodes.ShouldBe(route1.nodes.Skip(1).ToArray());
        actualRoutes.Find(r => r.CorrelationId == agv1.CorrelationId)!.RemainingEdges.ShouldBe(route1.edges.Skip(1).ToArray());
        actualRoutes.Find(r => r.CorrelationId == agv2.CorrelationId).ShouldNotBeNull();
        actualRoutes.Find(r => r.CorrelationId == agv2.CorrelationId)!.RemainingNodes.ShouldBe(route2.nodes.Skip(1).ToArray());
        actualRoutes.Find(r => r.CorrelationId == agv2.CorrelationId)!.RemainingEdges.ShouldBe(route2.edges.Skip(1).ToArray());
    }

    [TestMethod]
    public async Task GetAgvRouteAsync_WhenNoAgvCorrelationIdIsSpecific_ReturnsAllAgvRoutes()
    {
        const int agvCount = 5;
        var fixture = await new AgvRouteServiceTestFixture()
            .WithAgv(AgvGenerator.GenerateIdleAgvs(agvCount, "lastNode").ToArray())
            .BuildAsync();

        var actualRoutes = await fixture.AgvRouteRetrievalService.GetAgvRouteAsync(new List<Guid>());

        actualRoutes.ShouldNotBeNull();
        actualRoutes.Count.ShouldBe(agvCount);
    }

    [TestMethod]
    public async Task GetAgvRouteAsync_NonExistingAgvCorrelationId_ReturnsNullForThatId()
    {
        var route = AgvGraphGenerator.CreateRoute();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: route.nodes[1].Name);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: ActionState.Started);
        var action = actionGroup.Actions.First();
        action.Nodes = route.nodes;
        action.Edges = route.edges;
        var fixture = await new AgvRouteServiceTestFixture()
            .WithAgv(agv)
            .WithActionGroup(actionGroup)
            .BuildAsync();

        var nonExistingAgvCorrelationId = Guid.NewGuid();

        var actualRoutes = (await fixture.AgvRouteRetrievalService.GetAgvRouteAsync(new List<Guid> { agv.CorrelationId, nonExistingAgvCorrelationId })).ToList();

        actualRoutes.ShouldNotBeNull();
        actualRoutes.Count.ShouldBe(1);
        actualRoutes.Find(r => r.CorrelationId == agv.CorrelationId).ShouldNotBeNull();
        actualRoutes.Find(r => r.CorrelationId == agv.CorrelationId)!.RemainingNodes.ShouldBe(route.nodes.Skip(1).ToArray());
        actualRoutes.Find(r => r.CorrelationId == agv.CorrelationId)!.RemainingEdges.ShouldBe(route.edges.Skip(1).ToArray());
        actualRoutes.Find(r => r.CorrelationId == nonExistingAgvCorrelationId).ShouldBeNull();
    }

    [TestMethod]
    public async Task GetAgvRouteAsync_WhenRoutePlanCached_PopulatesBaseAndHorizon()
    {
        var route = AgvGraphGenerator.CreateRoute();
        var node = route.nodes[1];
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: node.Name);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(ActionState.Started, assignedAgv: agv.CorrelationId);
        var action = actionGroup.Actions.First();
        action.Nodes = route.nodes;
        action.Edges = route.edges;
        var fixture = await new AgvRouteServiceTestFixture()
            .WithAgv(agv)
            .WithActionGroup(actionGroup)
            .BuildAsync();
        fixture.RoutePlanCache.ConcurrentDict[agv.CorrelationId] = new RoutePlan([node], [], [], []);

        var actualRoute = await fixture.AgvRouteRetrievalService.GetAgvRouteAsync(agv.CorrelationId);

        actualRoute.ShouldNotBeNull();
        actualRoute.BaseNodeCount.ShouldBe(1);
    }
}