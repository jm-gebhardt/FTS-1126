using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.CommonTest.Helpers;
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
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agv0 = AgvGenerator.GenerateIdleAgv(index: 0);
        var agv1 = AgvGenerator.GenerateIdleAgv(index: 1);
        var agv2 = AgvGenerator.GenerateIdleAgv(index: 2);
        var agvs = new List<IAgv> { agv0, agv1, agv2 };

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .WithAgvHasRouteToDestination(agv0, false)
            .WithAgvHasRouteToDestination(agv1, false)
            .WithAgvHasRouteToDestination(agv2, false);

        var chooseNextAgv = await fixture.ShortestRouteChooser.GetAgvAsync(actionGroup, agvs);
        chooseNextAgv.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetAgvAsync_WhenThereAreMultipleAgvThatCanHandleRoute_ReturnAgvWithLowestCost()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agv0 = AgvGenerator.GenerateIdleAgv(index: 0);
        var agv1 = AgvGenerator.GenerateIdleAgv(index: 1);
        var agv2 = AgvGenerator.GenerateIdleAgv(index: 2);
        var agvs = new List<IAgv> { agv0, agv1, agv2 };

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .WithAgvHasRouteToDestination(agv0, false)
            .WithAgvHasRouteToDestination(agv1, true, 56f)
            .WithAgvHasRouteToDestination(agv2, true, 10f);

        var chooseNextAgv = await fixture.ShortestRouteChooser.GetAgvAsync(actionGroup, agvs);
        chooseNextAgv.ShouldBe(agv2);
    }

    [TestMethod]
    public async Task GetActionGroupAsync_WhenLastNodeIdIsNull_ThrowAgvLocationUnknownException()
    {
        var agv = AgvGenerator.GenerateIdleAgv(index: 0, lastNodeId: null);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .AddNotStartedActionGroup(actionGroup);

        await Should.ThrowAsync<AgvLocationUnknownException>(async () => await fixture.ShortestRouteChooser.GetActionGroupAsync(agv));
    }

    [TestMethod]
    public async Task GetActionGroupAsync_WhenThereIsNoRoute_ReturnNull()
    {
        var agv = AgvGenerator.GenerateIdleAgv(index: 0);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .AddNotStartedActionGroup(actionGroup)
            .WithAgvHasRouteToDestination(agv, false);

        var agvAction = await fixture.ShortestRouteChooser.GetActionGroupAsync(agv);
        agvAction.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetActionGroupAsync_WhenThereIsANotStartedActionGroupsThatHasRoute_ReturnNextActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv(index: 0);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .AddNotStartedActionGroup(actionGroup)
            .WithAgvHasRouteToDestination(agv);

        var agvAction = await fixture.ShortestRouteChooser.GetActionGroupAsync(agv);
        agvAction.ShouldBe(actionGroup.Actions.First());
    }
}