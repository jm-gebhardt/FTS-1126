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
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        var agv0 = AgvGenerator.GenerateIdleAgv(index: 0, agvGraph: agvGraph, lastNodeId: AgvGraphGenerator.ParkFar.Id);
        var agv1 = AgvGenerator.GenerateIdleAgv(index: 1, agvGraph: agvGraph, lastNodeId: AgvGraphGenerator.ParkClose.Id);
        var agv2 = AgvGenerator.GenerateIdleAgv(index: 2, agvGraph: agvGraph, lastNodeId: AgvGraphGenerator.ChargingFar.Id);
        var agvs = new List<IAgv> { agv0, agv1, agv2 };

        var fixture = new ChooseAgvActionGroupServiceFixture();

        var chooseNextAgv = await fixture.ShortestRouteChooser.GetAgvAsync(actionGroup, agvs);
        chooseNextAgv.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetAgvAsync_WhenThereAreMultipleAgvThatCanHandleRoute_ReturnAgvWithLowestCost()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        var agv0 = AgvGenerator.GenerateIdleAgv(index: 0, agvGraph: agvGraph, lastNodeId: AgvGraphGenerator.ParkFar.Id);
        var agv1 = AgvGenerator.GenerateIdleAgv(index: 1, agvGraph: agvGraph, lastNodeId: AgvGraphGenerator.ParkClose.Id); // Closest AGV
        var agv2 = AgvGenerator.GenerateIdleAgv(index: 2, agvGraph: agvGraph, lastNodeId: AgvGraphGenerator.ChargingFar.Id);
        var agvs = new List<IAgv> { agv0, agv1, agv2 };

        var fixture = new ChooseAgvActionGroupServiceFixture();

        var chooseNextAgv = await fixture.ShortestRouteChooser.GetAgvAsync(actionGroup, agvs);
        chooseNextAgv.ShouldBe(agv1);
    }

    [TestMethod]
    public async Task GetActionGroupAsync_WhenLastNodeIdIsNull_ThrowAgvLocationUnknownException()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        var agv = AgvGenerator.GenerateIdleAgv(index: 0, agvGraph: agvGraph, lastNodeId: null); // No last node id

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .AddNotStartedActionGroup(actionGroup);

        await Should.ThrowAsync<AgvLocationUnknownException>(async () => await fixture.ShortestRouteChooser.GetActionGroupAsync(agv));
    }

    [TestMethod]
    public async Task GetActionGroupAsync_WhenThereIsNoRoute_ReturnNull()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(destination: AgvGraphGenerator.DestinationUnreachable.Id); // Action with unreachable destination
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        var agv = AgvGenerator.GenerateIdleAgv(index: 0, agvGraph: agvGraph, lastNodeId: AgvGraphGenerator.ParkClose.Id);

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
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        var agv = AgvGenerator.GenerateIdleAgv(index: 0, agvGraph: agvGraph, lastNodeId: AgvGraphGenerator.ParkClose.Id);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .AddNotStartedActionGroup(actionGroup1)
            .AddNotStartedActionGroup(actionGroup2)
            .AddNotStartedActionGroup(actionGroup3);

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
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        var agv = AgvGenerator.GenerateIdleAgv(index: 0, agvGraph: agvGraph, lastNodeId: AgvGraphGenerator.ParkClose.Id);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .AddNotStartedActionGroup(actionGroup);

        var agvAction = await fixture.ShortestRouteChooser.GetActionGroupAsync(agv);
        agvAction.ShouldBe(actionGroup.Actions.First());
    }
}