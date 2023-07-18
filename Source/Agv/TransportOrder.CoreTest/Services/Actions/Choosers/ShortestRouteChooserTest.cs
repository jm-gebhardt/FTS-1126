using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Actions.Choosers;

[TestClass]
public class ShortestRouteChooserTest
{
    [TestMethod]
    public async Task GetAgvAsync_WhenThereAreNoAgvs_ReturnNull()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agvs = new List<IAgv>();

        var fixture = new ChooseAgvActionGroupServiceFixture();

        var chooseNextAgv = await fixture.ShortestRouteChooser.GetAgvAsync(actionGroup, agvs);
        chooseNextAgv.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetAgvAsync_WhenThereAreNoAgvThatCanHandleRoute_ReturnNull()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(destination: AgvGraphGenerator.DestinationUnreachable.Id); // Action with unreachable destination
        var agv0 = AgvGenerator.GenerateIdleAgv(index: 0, lastNodeId: AgvGraphGenerator.ParkFar.Id);
        var agv1 = AgvGenerator.GenerateIdleAgv(index: 1, lastNodeId: AgvGraphGenerator.ParkClose.Id);
        var agv2 = AgvGenerator.GenerateIdleAgv(index: 2, lastNodeId: AgvGraphGenerator.ChargingFar.Id);
        var agvs = new List<IAgv> { agv0, agv1, agv2 };

        var fixture = new ChooseAgvActionGroupServiceFixture();

        var chooseNextAgv = await fixture.ShortestRouteChooser.GetAgvAsync(actionGroup, agvs);
        chooseNextAgv.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetAgvAsync_WhenThereAreMultipleAgvThatCanHandleRoute_ReturnAgvWithLowestCost()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agv0 = AgvGenerator.GenerateIdleAgv(index: 0, lastNodeId: AgvGraphGenerator.ParkFar.Id);
        var agv1 = AgvGenerator.GenerateIdleAgv(index: 1, lastNodeId: AgvGraphGenerator.ParkClose.Id); // Closest AGV
        var agv2 = AgvGenerator.GenerateIdleAgv(index: 2, lastNodeId: AgvGraphGenerator.ChargingFar.Id);
        var agvs = new List<IAgv> { agv0, agv1, agv2 };
        var fixture = new ChooseAgvActionGroupServiceFixture()
            .WithAgvHasRouteToDestination(agv0, routeCosts: 5f)
            .WithAgvHasRouteToDestination(agv1, routeCosts: 2f)
            .WithAgvHasRouteToDestination(agv2, routeCosts: 3f);

        var chooseNextAgv = await fixture.ShortestRouteChooser.GetAgvAsync(actionGroup, agvs);

        chooseNextAgv.ShouldBe(agv1);
    }

    [TestMethod]
    public async Task GetActionGroupAsync_WhenLastNodeIdIsNull_ThrowAgvLocationUnknownException()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agv = AgvGenerator.GenerateIdleAgv(index: 0, lastNodeId: null); // No last node id

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .AddNotStartedActionGroup(actionGroup);

        await Should.ThrowAsync<AgvLocationUnknownException>(async () => await fixture.ShortestRouteChooser.GetActionGroupAsync(agv));
    }

    [TestMethod]
    public async Task GetActionGroupAsync_WhenThereIsNoRoute_ReturnNull()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(destination: AgvGraphGenerator.DestinationUnreachable.Id); // Action with unreachable destination
        var agv = AgvGenerator.GenerateIdleAgv(index: 0, lastNodeId: AgvGraphGenerator.ParkClose.Id);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .AddNotStartedActionGroup(actionGroup);

        var agvAction = await fixture.ShortestRouteChooser.GetActionGroupAsync(agv);
        agvAction.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetActionGroupAsync_WhenThereIsANotStartedActionGroupsThatHasRoute_ReturnClosestActionGroup()
    {
        var actionGroup1 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Id);
        var actionGroup2 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id); // Closest pick action
        var actionGroup3 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceUnreachable.Id);
        var agv = AgvGenerator.GenerateIdleAgv(index: 0, lastNodeId: AgvGraphGenerator.ParkClose.Id);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .AddNotStartedActionGroup(actionGroup1)
            .AddNotStartedActionGroup(actionGroup2)
            .AddNotStartedActionGroup(actionGroup3)
            .WithAgvHasRouteToDestination(agv, actionGroup1.Actions.First().TargetNodeId, actionGroup1.Actions.Last().TargetNodeId, routeCosts: 10f)
            .WithAgvHasRouteToDestination(agv, actionGroup2.Actions.First().TargetNodeId, actionGroup2.Actions.Last().TargetNodeId, routeCosts: 1f)
            .WithAgvHasRouteToDestination(agv, actionGroup3.Actions.First().TargetNodeId, actionGroup3.Actions.Last().TargetNodeId, routeCosts: null);

        var agvAction = await fixture.ShortestRouteChooser.GetActionGroupAsync(agv);
        agvAction.ShouldBe(actionGroup2.Actions.First());
    }

    [TestMethod]
    public async Task GetActionGroupAsync_WhenDropActionIsCloser_ReturnPickAction()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            source: AgvGraphGenerator.SourceFar.Id,
            destination: AgvGraphGenerator.DestinationClose.Id
        );
        var agv = AgvGenerator.GenerateIdleAgv(index: 0, lastNodeId: AgvGraphGenerator.ParkClose.Id);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .AddNotStartedActionGroup(actionGroup)
            .WithAgvHasRouteToDestination(agv, routeCosts: 1f);

        var agvAction = await fixture.ShortestRouteChooser.GetActionGroupAsync(agv);
        agvAction.ShouldBe(actionGroup.Actions.First());
    }
}