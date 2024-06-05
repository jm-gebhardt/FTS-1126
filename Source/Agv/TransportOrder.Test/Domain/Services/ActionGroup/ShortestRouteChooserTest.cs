using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

[TestClass]
public class ShortestRouteChooserTest
{
    [TestMethod]
    public async Task GetAgvAsync_WhenThereAreNoAgvs_ReturnNull()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agvs = new List<TransportOrder.Domain.Models.Agv.Agv>();

        var fixture = new ChooseAgvActionGroupServiceFixture();

        var chooseNextAgv = await fixture.ShortestRouteChooser.GetAgvAsync(actionGroup, agvs, new List<AgvData>());
        chooseNextAgv.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetAgvAsync_WhenThereAreNoAgvThatCanHandleRoute_ReturnNull()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(destination: AgvGraphGenerator.DestinationUnreachable.Id); // Action with unreachable destination
        var agv0 = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkFar.Id);
        var agv1 = AgvGenerator.GenerateIdleAgv(1, AgvGraphGenerator.ParkClose.Id);
        var agv2 = AgvGenerator.GenerateIdleAgv(2, AgvGraphGenerator.ChargingFar.Id);
        var agvs = new List<TransportOrder.Domain.Models.Agv.Agv> { agv0, agv1, agv2 };

        var fixture = new ChooseAgvActionGroupServiceFixture();

        var chooseNextAgv = await fixture.ShortestRouteChooser.GetAgvAsync(actionGroup, agvs, new List<AgvData>());
        chooseNextAgv.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetAgvAsync_WhenThereAreMultipleAgvThatCanHandleRoute_ReturnAgvWithLowestCost()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agv0 = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkFar.Id);
        var agv1 = AgvGenerator.GenerateIdleAgv(1, AgvGraphGenerator.ParkClose.Id); // Closest AGV
        var agv2 = AgvGenerator.GenerateIdleAgv(2, AgvGraphGenerator.ChargingFar.Id);
        var agvs = new List<TransportOrder.Domain.Models.Agv.Agv> { agv0, agv1, agv2 };
        var fixture = new ChooseAgvActionGroupServiceFixture()
            .WithAgvHasRouteToDestination(agv0, actionGroup, 5f)
            .WithAgvHasRouteToDestination(agv1, actionGroup, 2f)
            .WithAgvHasRouteToDestination(agv2, actionGroup, 3f);

        var chooseNextAgv = await fixture.ShortestRouteChooser.GetAgvAsync(actionGroup, agvs, new List<AgvData>());

        chooseNextAgv.ShouldBe(agv1);
    }

    [TestMethod]
    public async Task GetActionGroupAsync_WhenLastNodeIdIsNull_ThrowAgvLocationUnknownException()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agv = AgvGenerator.GenerateIdleAgv(index: 0, lastNodeId: null); // No last node id

        var fixture = new ChooseAgvActionGroupServiceFixture(true)
            .AddNotStartedActionGroup(actionGroup);

        await Should.ThrowAsync<AgvLocationUnknownException>(async () => await fixture.ShortestRouteChooser.GetNotStartedActionGroupAsync(agv, null));
    }

    [TestMethod]
    public async Task GetActionGroupAsync_WhenThereIsNoRoute_ReturnNull()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(destination: AgvGraphGenerator.DestinationUnreachable.Id); // Action with unreachable destination
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .AddNotStartedActionGroup(actionGroup);

        var agvAction = await fixture.ShortestRouteChooser.GetNotStartedActionGroupAsync(agv, null);
        agvAction.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetActionGroupAsync_WhenThereIsANotStartedActionGroupsThatHasRoute_ReturnClosestActionGroup()
    {
        var actionGroup1 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Id);
        var actionGroup2 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id); // Closest pick action
        var actionGroup3 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceUnreachable.Id);
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .AddNotStartedActionGroup(actionGroup1)
            .AddNotStartedActionGroup(actionGroup2)
            .AddNotStartedActionGroup(actionGroup3)
            .WithAgvHasRouteToDestination(agv, actionGroup1.Actions.First().TargetNodeId, actionGroup1.Actions.Last().TargetNodeId, actionGroup1, 10f)
            .WithAgvHasRouteToDestination(agv, actionGroup2.Actions.First().TargetNodeId, actionGroup2.Actions.Last().TargetNodeId, actionGroup2, 1f)
            .WithAgvHasRouteToDestination(agv, actionGroup3.Actions.First().TargetNodeId, actionGroup3.Actions.Last().TargetNodeId, actionGroup3, 20f);

        var chosenActionGroup = await fixture.ShortestRouteChooser.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldNotBeNull();
        chosenActionGroup.Actions.First().ShouldBe(actionGroup2.Actions.First());
    }

    [TestMethod]
    public async Task GetActionGroupAsync_WhenThereAreNotStartedActionGroupsWithManualActionGroup_ReturnManualActionGroup()
    {
        var actionGroup1 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Id);
        var actionGroup2 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id); // Closest pick action
        var actionGroup3 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceUnreachable.Id);
        var moveActionGroup = AgvActionGenerator.GenerateMoveActionGroup("Move Destination Id", true);
        string agvLastNodeId = AgvGraphGenerator.ParkClose.Id;
        var agv = AgvGenerator.GenerateIdleAgv(0, agvLastNodeId);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .AddNotStartedActionGroup(actionGroup1)
            .AddNotStartedActionGroup(actionGroup2)
            .AddNotStartedActionGroup(moveActionGroup)
            .AddNotStartedActionGroup(actionGroup3)
            .WithAgvHasRouteToDestination(agv, actionGroup1.Actions.First().TargetNodeId, actionGroup1.Actions.Last().TargetNodeId, actionGroup1, 10f)
            .WithAgvHasRouteToDestination(agv, actionGroup2.Actions.First().TargetNodeId, actionGroup2.Actions.Last().TargetNodeId, actionGroup2, 2f)
            .WithAgvHasRouteToDestination(agv, agvLastNodeId, moveActionGroup.Actions.First().TargetNodeId, moveActionGroup, 10f)
            .WithAgvHasRouteToDestination(agv, actionGroup3.Actions.First().TargetNodeId, actionGroup3.Actions.Last().TargetNodeId, actionGroup3, routeCosts: null);

        var chosenActionGroup = await fixture.ShortestRouteChooser.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldNotBeNull();
        chosenActionGroup.Actions.First().ShouldBe(moveActionGroup.Actions.First());
    }

    [TestMethod]
    public async Task GetActionGroupAsync_WhenDropActionIsCloser_ReturnPickAction()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            source: AgvGraphGenerator.SourceFar.Id,
            destination: AgvGraphGenerator.DestinationClose.Id
        );
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .AddNotStartedActionGroup(actionGroup)
            .WithAgvHasRouteToDestination(agv, actionGroup, 1f);

        var chosenActionGroup = await fixture.ShortestRouteChooser.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldNotBeNull();
        chosenActionGroup.Actions.First().ShouldBe(actionGroup.Actions.First());
    }

    [TestMethod]
    public async Task GetActionGroupAsync_FirstActionOfActionGroupIsIncomplete_ReturnNull()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            source: string.Empty,
            destination: AgvGraphGenerator.DestinationClose.Id
        );
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .AddNotStartedActionGroup(actionGroup)
            .WithAgvHasRouteToDestination(agv, actionGroup, 1f);

        var chosenActionGroup = await fixture.ShortestRouteChooser.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldBeNull();
    }
}