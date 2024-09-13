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
    public async Task GetNotStartedActionGroupAsync_WhenLastNodeIdIsNull_ThrowAgvLocationUnknownException()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agv = AgvGenerator.GenerateIdleAgv(index: 0, lastNodeId: null); // No last node id

        var fixture = new ChooseAgvActionGroupServiceFixture(true)
            .WithNotStartedActionGroup(actionGroup);

        await Should.ThrowAsync<AgvLocationUnknownException>(async () => await fixture.ShortestRouteChooser.GetNotStartedActionGroupAsync(agv, null));
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_WhenThereIsNoRoute_ReturnNull()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(destination: AgvGraphGenerator.DestinationUnreachable.Id); // Action with unreachable destination
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .WithNotStartedActionGroup(actionGroup);

        var agvAction = await fixture.ShortestRouteChooser.GetNotStartedActionGroupAsync(agv, null);
        agvAction.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_WhenThereIsANotStartedActionGroupsThatHasRoute_ReturnClosestActionGroup()
    {
        var actionGroup1 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Id);
        var actionGroup2 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id); // Closest pick action
        var actionGroup3 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceUnreachable.Id);
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .WithNotStartedActionGroup(actionGroup1)
            .WithNotStartedActionGroup(actionGroup2)
            .WithNotStartedActionGroup(actionGroup3)
            .WithAgvHasRouteToDestination(agv, actionGroup1.Actions.First().Target.NodeId!, actionGroup1.Actions.Last().Target.NodeId!, actionGroup1, 10f)
            .WithAgvHasRouteToDestination(agv, actionGroup2.Actions.First().Target.NodeId!, actionGroup2.Actions.Last().Target.NodeId!, actionGroup2, 1f)
            .WithAgvHasRouteToDestination(agv, actionGroup3.Actions.First().Target.NodeId!, actionGroup3.Actions.Last().Target.NodeId!, actionGroup3, 20f);

        var chosenActionGroup = await fixture.ShortestRouteChooser.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldNotBeNull();
        chosenActionGroup.Actions.First().ShouldBe(actionGroup2.Actions.First());
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_WhenThereIsAPriorityActionGroup_ReturnPriorityActionGroup()
    {
        var actionGroup1 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id);
        var priorityActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id); // Priority action
        priorityActionGroup.HasPriority = true;
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .WithNotStartedActionGroup(actionGroup1)
            .WithNotStartedActionGroup(priorityActionGroup)
            .WithAgvHasRouteToDestination(agv, actionGroup1.Actions.First().Target.NodeId!, actionGroup1.Actions.Last().Target.NodeId!, actionGroup1, 10f)
            .WithAgvHasRouteToDestination(agv, priorityActionGroup.Actions.First().Target.NodeId!, priorityActionGroup.Actions.Last().Target.NodeId!, priorityActionGroup, 10f);

        var chosenActionGroup = await fixture.ShortestRouteChooser.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldNotBeNull();
        chosenActionGroup.Actions.First().ShouldBe(priorityActionGroup.Actions.First());
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_EqualPriorityActionGroups_ReturnActionGroupCreatedEarlier()
    {
        var earlierCreatedActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id);
        var laterCreatedActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id, createdDate:earlierCreatedActionGroup.CreatedDate.AddMinutes(1)); 
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .WithNotStartedActionGroup(earlierCreatedActionGroup)
            .WithNotStartedActionGroup(laterCreatedActionGroup)
            .WithAgvHasRouteToDestination(agv, earlierCreatedActionGroup.Actions.First().Target.NodeId!, earlierCreatedActionGroup.Actions.Last().Target.NodeId!, earlierCreatedActionGroup, 1f)
            .WithAgvHasRouteToDestination(agv, laterCreatedActionGroup.Actions.First().Target.NodeId!, laterCreatedActionGroup.Actions.Last().Target.NodeId!, laterCreatedActionGroup, 1f);

        var chosenActionGroup = await fixture.ShortestRouteChooser.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldNotBeNull();
        chosenActionGroup.Actions.First().ShouldBe(earlierCreatedActionGroup.Actions.First());
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_WhenThereIsAnActionGroupAssignedToTheAgv_ReturnAssignedActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);
        var actionGroup1 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id);
        var assignedActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id, assignedAgv: agv.CorrelationId); // Assigned action

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .WithNotStartedActionGroup(actionGroup1)
            .WithNotStartedActionGroup(assignedActionGroup)
            .WithAgvHasRouteToDestination(agv, actionGroup1.Actions.First().Target.NodeId!, actionGroup1.Actions.Last().Target.NodeId!, actionGroup1, 1f)
            .WithAgvHasRouteToDestination(agv, assignedActionGroup.Actions.First().Target.NodeId!, assignedActionGroup.Actions.Last().Target.NodeId!, assignedActionGroup, 10f);

        var chosenActionGroup = await fixture.ShortestRouteChooser.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldNotBeNull();
        chosenActionGroup.Actions.First().ShouldBe(assignedActionGroup.Actions.First());
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_WhenThereAreNotStartedActionGroupsWithManualActionGroup_ReturnManualActionGroup()
    {
        var actionGroup1 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Id);
        var actionGroup2 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Id); // Closest pick action
        var actionGroup3 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceUnreachable.Id);
        var moveActionGroup = AgvActionGenerator.GenerateMoveActionGroup("Move Destination Id", true);
        string agvLastNodeId = AgvGraphGenerator.ParkClose.Id;
        var agv = AgvGenerator.GenerateIdleAgv(0, agvLastNodeId);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .WithNotStartedActionGroup(actionGroup1)
            .WithNotStartedActionGroup(actionGroup2)
            .WithNotStartedActionGroup(moveActionGroup)
            .WithNotStartedActionGroup(actionGroup3)
            .WithAgvHasRouteToDestination(agv, actionGroup1.Actions.First().Target.NodeId!, actionGroup1.Actions.Last().Target.NodeId!, actionGroup1, 10f)
            .WithAgvHasRouteToDestination(agv, actionGroup2.Actions.First().Target.NodeId!, actionGroup2.Actions.Last().Target.NodeId!, actionGroup2, 2f)
            .WithAgvHasRouteToDestination(agv, agvLastNodeId, moveActionGroup.Actions.First().Target.NodeId!, moveActionGroup, 10f)
            .WithAgvHasRouteToDestination(agv, actionGroup3.Actions.First().Target.NodeId!, actionGroup3.Actions.Last().Target.NodeId!, actionGroup3, routeCosts: null);

        var chosenActionGroup = await fixture.ShortestRouteChooser.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldNotBeNull();
        chosenActionGroup.Actions.First().ShouldBe(moveActionGroup.Actions.First());
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_WhenDropActionIsCloser_ReturnPickAction()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            source: AgvGraphGenerator.SourceFar.Id,
            destination: AgvGraphGenerator.DestinationClose.Id
        );
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .WithNotStartedActionGroup(actionGroup)
            .WithAgvHasRouteToDestination(agv, actionGroup, 1f);

        var chosenActionGroup = await fixture.ShortestRouteChooser.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldNotBeNull();
        chosenActionGroup.Actions.First().ShouldBe(actionGroup.Actions.First());
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_FirstActionOfActionGroupIsIncomplete_ReturnNull()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            source: string.Empty,
            destination: AgvGraphGenerator.DestinationClose.Id
        );
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .WithNotStartedActionGroup(actionGroup)
            .WithAgvHasRouteToDestination(agv, actionGroup, 1f);

        var chosenActionGroup = await fixture.ShortestRouteChooser.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_SecondActionOfTheOneActionGroupHasLockedTarget_ReturnNull()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            source: AgvGraphGenerator.SourceClose.Id,
            destination: AgvGraphGenerator.DestinationClose.Id
        );
        List<string> lockedNodes = [AgvGraphGenerator.DestinationClose.Id];
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .WithNotStartedActionGroup(actionGroup)
            .WithLockedNodes(lockedNodes)
            .WithAgvHasRouteToDestination(agv, actionGroup, 1f);

        var chosenActionGroup = await fixture.ShortestRouteChooser.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_LowestCostActionGroupHasLockedDestination_ReturnHigherCostActionGroup()
    {
        var actionGroupClose = AgvActionGenerator.GeneratePickDropActionGroup(
            source: AgvGraphGenerator.SourceClose.Id,
            destination: AgvGraphGenerator.DestinationClose.Id
        );
        var actionGroupFar = AgvActionGenerator.GeneratePickDropActionGroup(
            source: AgvGraphGenerator.SourceFar.Id,
            destination: AgvGraphGenerator.DestinationFar.Id
        );
        List<string> lockedNodes = [AgvGraphGenerator.DestinationClose.Id];
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .WithNotStartedActionGroup(actionGroupClose)
            .WithNotStartedActionGroup(actionGroupFar)
            .WithLockedNodes(lockedNodes)
            .WithAgvHasRouteToDestination(agv, actionGroupClose, 1f)
            .WithAgvHasRouteToDestination(agv, actionGroupFar, 5f);

        var chosenActionGroup = await fixture.ShortestRouteChooser.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup!.Actions.FirstOrDefault()?.Target.NodeId.ShouldBe(AgvGraphGenerator.SourceFar.Id);
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_ChargingActionGroupWithNotLockedCharger_ReturnChargingActionGroup()
    {
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(
            targetNodeId: AgvGraphGenerator.ChargingClose.Id
        );
        List<string> lockedNodes = [AgvGraphGenerator.ChargingFar.Id];
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .WithNotStartedActionGroup(actionGroup)
            .GetCreatedChargingActionGroupForAgvAsync(actionGroup)
            .WithLockedNodes(lockedNodes)
            .WithAgvHasRouteToDestination(agv, actionGroup, 1f);

        var chosenActionGroup = await fixture.ShortestRouteChooser.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldNotBeNull();
        chosenActionGroup.IsChargingOrder.ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_ChargingActionGroupWithLockedCharger_ReturnNull()
    {
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(
            targetNodeId: AgvGraphGenerator.ChargingClose.Id
        );
        List<string> lockedNodes = [AgvGraphGenerator.ChargingClose.Id];
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Id);

        var fixture = new ChooseAgvActionGroupServiceFixture()
            .WithNotStartedActionGroup(actionGroup)
            .GetCreatedChargingActionGroupForAgvAsync(actionGroup)
            .WithLockedNodes(lockedNodes)
            .WithAgvHasRouteToDestination(agv, actionGroup, 1f);

        var chosenActionGroup = await fixture.ShortestRouteChooser.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldBeNull();
    }
}