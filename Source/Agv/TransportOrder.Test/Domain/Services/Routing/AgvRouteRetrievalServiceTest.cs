using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Test.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Test.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvActionGenerator = Gebhardt.StoreWare.Agv.TransportOrder.Test.Helper.AgvActionGenerator;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Domain.Services.Routing;

[TestClass]
public class AgvRouteRetrievalServiceTest
{
    [TestMethod]
    public async Task GetAgvRouteAsync_WhenLastNodeIdIsNull_ReturnsAgvRouteBasedOnAgvsLastNodeId()
    {
        string[] nodes = NodeGenerator.GenerateRouteNodes();
        string node = nodes[1];
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: node);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(ActionState.Started, assignedAgv: agv.CorrelationId);
        var action = actionGroup.Actions.First();
        action.Nodes = nodes;
        var fixture = await new AgvRouteServiceTestFixture()
            .WithAgv(agv)
            .WithActionGroup(actionGroup)
            .BuildAsync();

        var actualRoute = await fixture.AgvRouteRetrievalService.GetAgvRouteAsync(agv.CorrelationId);

        actualRoute.ShouldNotBeNull();
        actualRoute.RemainingRoute.ShouldBe(nodes.Skip(1).ToArray());
    }

    [TestMethod]
    public async Task GetAgvRouteAsync_WhenLastNodeIdIsGiven_ReturnsRemainingRoute()
    {
        string[] nodes = NodeGenerator.GenerateRouteNodes();
        string node = nodes[2];
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: nodes[0]);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: ActionState.Started);
        var action = actionGroup.Actions.First();
        action.Nodes = nodes;
        var fixture = await new AgvRouteServiceTestFixture()
            .WithAgv(agv)
            .WithActionGroup(actionGroup)
            .BuildAsync();

        var actualRoute = await fixture.AgvRouteRetrievalService.GetAgvRouteAsync(agv.CorrelationId, node);

        actualRoute.ShouldNotBeNull();
        actualRoute.RemainingRoute.ShouldBe(nodes.Skip(2).ToArray());
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
        string[] nodes1 = NodeGenerator.GenerateRouteNodes();
        string[] nodes2 = NodeGenerator.GenerateRouteNodes("D1");
        var agv1 = AgvGenerator.GenerateIdleAgv(lastNodeId: nodes1[1], index: 1);
        var agv2 = AgvGenerator.GenerateIdleAgv(lastNodeId: nodes2[2], index: 2);

        var actionGroup1 = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv1.CorrelationId, pickActionState: ActionState.Started);
        var action1 = actionGroup1.Actions.First();
        action1.Nodes = nodes1;

        var actionGroup2 = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv2.CorrelationId, pickActionState: ActionState.Started);
        var action2 = actionGroup2.Actions.First();
        action2.Nodes = nodes2;
        var fixture = await new AgvRouteServiceTestFixture()
            .WithAgv(agv1)
            .WithAgv(agv2)
            .WithActionGroup(actionGroup1)
            .WithActionGroup(actionGroup2)
            .BuildAsync();

        IReadOnlyDictionary<Guid, CurrentAgvRouteReadModel?> actualRoutes = await fixture.AgvRouteRetrievalService.GetAgvRouteAsync(new List<Guid> { agv1.CorrelationId, agv2.CorrelationId });

        actualRoutes.ShouldNotBeNull();
        actualRoutes.Count.ShouldBe(2);
        actualRoutes[agv1.CorrelationId].ShouldNotBeNull();
        actualRoutes[agv1.CorrelationId]!.RemainingRoute.ShouldBe(nodes1.Skip(1).ToArray());
        actualRoutes[agv2.CorrelationId].ShouldNotBeNull();
        actualRoutes[agv2.CorrelationId]!.RemainingRoute.ShouldBe(nodes2.Skip(2).ToArray());
    }

    [TestMethod]
    public async Task GetAgvRouteAsync_WhenNoAgvCorrelationIdIsSpecific_ReturnsAllAgvRoutes()
    {
        const int agvCount = 5;
        var fixture = await new AgvRouteServiceTestFixture()
            .WithAgv(AgvGenerator.GenerateIdleAgvs(agvCount, "lastNode").ToArray())
            .BuildAsync();

        IReadOnlyDictionary<Guid, CurrentAgvRouteReadModel?> actualRoutes = await fixture.AgvRouteRetrievalService.GetAgvRouteAsync(new List<Guid>());

        actualRoutes.ShouldNotBeNull();
        actualRoutes.Count.ShouldBe(agvCount);
    }

    [TestMethod]
    public async Task GetAgvRouteAsync_NonExistingAgvCorrelationId_ReturnsNullForThatId()
    {
        string[] nodes = NodeGenerator.GenerateRouteNodes();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeId: nodes[1]);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, pickActionState: ActionState.Started);
        var action = actionGroup.Actions.First();
        action.Nodes = nodes;
        var fixture = await new AgvRouteServiceTestFixture()
            .WithAgv(agv)
            .WithActionGroup(actionGroup)
            .BuildAsync();

        var nonExistingAgvCorrelationId = Guid.NewGuid();

        IReadOnlyDictionary<Guid, CurrentAgvRouteReadModel?> actualRoutes = await fixture.AgvRouteRetrievalService.GetAgvRouteAsync(new List<Guid> { agv.CorrelationId, nonExistingAgvCorrelationId });

        actualRoutes.ShouldNotBeNull();
        actualRoutes.Count.ShouldBe(2);
        actualRoutes[agv.CorrelationId].ShouldNotBeNull();
        actualRoutes[agv.CorrelationId]!.RemainingRoute.ShouldBe(nodes.Skip(1).ToArray());
        actualRoutes[nonExistingAgvCorrelationId].ShouldBeNull();
    }
}