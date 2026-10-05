using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

[TestClass]
public class ShortestRouteChooserTest
{
    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_WhenLastNodeIdIsNull_ReturnsNull()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var agv = AgvGenerator.GenerateIdleAgv(index: 0, lastNodeName: null); // No last node id

        var fixture = new ChooseAgvActionGroupTestFixture<ShortestRouteChooser>(true)
            .WithNotStartedActionGroup(actionGroup)
            .WithAgvAtQueueHead([])
            .Build();

        var notStartedActionGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        notStartedActionGroup.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_WhenThereIsNoRoute_ReturnNull()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(destination: AgvGraphGenerator.DestinationUnreachable.Name); // Action with unreachable destination
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Name);

        var fixture = new ChooseAgvActionGroupTestFixture<ShortestRouteChooser>()
            .WithNotStartedActionGroup(actionGroup)
            .WithAgvAtQueueHead([])
            .Build();

        var agvAction = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        agvAction.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_WhenThereIsANotStartedActionGroupsThatHasRoute_ReturnClosestActionGroup()
    {
        var actionGroup1 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Name);
        var actionGroup2 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Name); // Closest pick action
        var actionGroup3 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceUnreachable.Name);
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Name);

        var fixture = new ChooseAgvActionGroupTestFixture<ShortestRouteChooser>()
            .WithNotStartedActionGroup(actionGroup1)
            .WithNotStartedActionGroup(actionGroup2)
            .WithNotStartedActionGroup(actionGroup3)
            .WithAgvHasRouteToDestination(agv, actionGroup1.Actions.First().Target.Node!.Name, actionGroup1.Actions.Last().Target.Node!.Name, actionGroup1, 10f)
            .WithAgvHasRouteToDestination(agv, actionGroup2.Actions.First().Target.Node!.Name, actionGroup2.Actions.Last().Target.Node!.Name, actionGroup2, 1f)
            .WithAgvHasRouteToDestination(agv, actionGroup3.Actions.First().Target.Node!.Name, actionGroup3.Actions.Last().Target.Node!.Name, actionGroup3, 20f)
            .WithAgvAtQueueHead([])
            .Build();

        var chosenActionGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldNotBeNull();
        chosenActionGroup.Actions.First().ShouldBe(actionGroup2.Actions.First());
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_WhenThereIsAPriorityActionGroup_ReturnPriorityActionGroup()
    {
        var actionGroup1 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Name);
        var priorityActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Name); // Priority action
        priorityActionGroup.Priority = Priority.Resume;
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Name);

        var fixture = new ChooseAgvActionGroupTestFixture<ShortestRouteChooser>()
            .WithNotStartedActionGroup(actionGroup1)
            .WithNotStartedActionGroup(priorityActionGroup)
            .WithAgvHasRouteToDestination(agv, actionGroup1.Actions.First().Target.Node!.Name, actionGroup1.Actions.Last().Target.Node!.Name, actionGroup1, 10f)
            .WithAgvHasRouteToDestination(agv, priorityActionGroup.Actions.First().Target.Node!.Name, priorityActionGroup.Actions.Last().Target.Node!.Name, priorityActionGroup, 10f)
            .WithAgvAtQueueHead([])
            .Build();

        var chosenActionGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldNotBeNull();
        chosenActionGroup.Actions.First().ShouldBe(priorityActionGroup.Actions.First());
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_EqualPriorityActionGroups_ReturnActionGroupCreatedEarlier()
    {
        var earlierCreatedActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Name);
        var laterCreatedActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Name, createdDate: earlierCreatedActionGroup.CreatedDate.AddMinutes(1));
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Name);

        var fixture = new ChooseAgvActionGroupTestFixture<ShortestRouteChooser>()
            .WithNotStartedActionGroup(earlierCreatedActionGroup)
            .WithNotStartedActionGroup(laterCreatedActionGroup)
            .WithAgvHasRouteToDestination(agv, earlierCreatedActionGroup.Actions.First().Target.Node!.Name, earlierCreatedActionGroup.Actions.Last().Target.Node!.Name, earlierCreatedActionGroup, 1f)
            .WithAgvHasRouteToDestination(agv, laterCreatedActionGroup.Actions.First().Target.Node!.Name, laterCreatedActionGroup.Actions.Last().Target.Node!.Name, laterCreatedActionGroup, 1f)
            .WithAgvAtQueueHead([])
            .Build();

        var chosenActionGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldNotBeNull();
        chosenActionGroup.Actions.First().ShouldBe(earlierCreatedActionGroup.Actions.First());
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_WhenThereIsAnActionGroupAssignedToTheAgv_ReturnAssignedActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Name);
        var actionGroup1 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Name);
        var assignedActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Name, assignedAgv: agv.CorrelationId); // Assigned action

        var fixture = new ChooseAgvActionGroupTestFixture<ShortestRouteChooser>()
            .WithNotStartedActionGroup(actionGroup1)
            .WithNotStartedActionGroup(assignedActionGroup)
            .WithAgvHasRouteToDestination(agv, actionGroup1.Actions.First().Target.Node!.Name, actionGroup1.Actions.Last().Target.Node!.Name, actionGroup1, 1f)
            .WithAgvHasRouteToDestination(agv, assignedActionGroup.Actions.First().Target.Node!.Name, assignedActionGroup.Actions.Last().Target.Node!.Name, assignedActionGroup, 10f)
            .WithAgvAtQueueHead([])
            .Build();

        var chosenActionGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldNotBeNull();
        chosenActionGroup.Actions.First().ShouldBe(assignedActionGroup.Actions.First());
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_WhenThereAreNotStartedActionGroupsWithManualActionGroup_ReturnManualActionGroup()
    {
        string agvLastNodeId = AgvGraphGenerator.ParkClose.Name;
        var agv = AgvGenerator.GenerateIdleAgv(0, agvLastNodeId);
        var actionGroup1 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Name);
        var actionGroup2 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Name); // Closest pick action
        var actionGroup3 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceUnreachable.Name);
        var moveActionGroup = AgvActionGenerator.GenerateMoveActionGroup("Move Destination Id", true, assignedAgv: agv.CorrelationId);

        var fixture = new ChooseAgvActionGroupTestFixture<ShortestRouteChooser>()
            .WithNotStartedActionGroup(actionGroup1)
            .WithNotStartedActionGroup(actionGroup2)
            .WithNotStartedActionGroup(moveActionGroup)
            .WithNotStartedActionGroup(actionGroup3)
            .WithAgvHasRouteToDestination(agv, actionGroup1.Actions.First().Target.Node!.Name, actionGroup1.Actions.Last().Target.Node!.Name, actionGroup1, 10f)
            .WithAgvHasRouteToDestination(agv, actionGroup2.Actions.First().Target.Node!.Name, actionGroup2.Actions.Last().Target.Node!.Name, actionGroup2, 2f)
            .WithAgvHasRouteToDestination(agv, agvLastNodeId, moveActionGroup.Actions.First().Target.Node!.Name, moveActionGroup, 10f)
            .WithAgvHasRouteToDestination(agv, actionGroup3.Actions.First().Target.Node!.Name, actionGroup3.Actions.Last().Target.Node!.Name, actionGroup3, routeCosts: null)
            .Build();

        var chosenActionGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldNotBeNull();
        chosenActionGroup.Actions.First().ShouldBe(moveActionGroup.Actions.First());
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_QueueEvacuationMoveAndQueueHeadMove_ReturnQueueHeadMove()
    {
        string agvLastNodeId = AgvGraphGenerator.ParkClose.Name;
        var agv = AgvGenerator.GenerateIdleAgv(0, agvLastNodeId);
        var actionGroup1 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Name);
        var actionGroup2 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Name); // Closest pick action
        var actionGroup3 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceUnreachable.Name);
        var evacuationMove = AgvActionGenerator.GenerateMoveActionGroup("evacuation node", true, priority: Priority.QueueEvacuation, orderId: Queue.EvacuationOrderId, assignedAgv: agv.CorrelationId);
        var queueHeadMove = AgvActionGenerator.GenerateMoveActionGroup("queue head node", true, priority: Priority.Queueing, orderId: "Queue move", assignedAgv: agv.CorrelationId);

        var fixture = new ChooseAgvActionGroupTestFixture<ShortestRouteChooser>()
            .WithNotStartedActionGroup(actionGroup1)
            .WithNotStartedActionGroup(actionGroup2)
            .WithNotStartedActionGroup(actionGroup3)
            .WithNotStartedActionGroup(queueHeadMove)
            .WithNotStartedActionGroup(evacuationMove)
            .WithAgvHasRouteToDestination(agv, actionGroup1.Actions.First().Target.Node!.Name, actionGroup1.Actions.Last().Target.Node!.Name, actionGroup1, 10f)
            .WithAgvHasRouteToDestination(agv, actionGroup2.Actions.First().Target.Node!.Name, actionGroup2.Actions.Last().Target.Node!.Name, actionGroup2, 2f)
            .WithAgvHasRouteToDestination(agv, agvLastNodeId, evacuationMove.Actions.First().Target.Node!.Name, evacuationMove, 10f)
            .WithAgvHasRouteToDestination(agv, agvLastNodeId, queueHeadMove.Actions.First().Target.Node!.Name, queueHeadMove, 10f)
            .WithAgvHasRouteToDestination(agv, actionGroup3.Actions.First().Target.Node!.Name, actionGroup3.Actions.Last().Target.Node!.Name, actionGroup3, routeCosts: null)
            .Build();

        var chosenActionGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldNotBeNull();
        chosenActionGroup.Actions.First().ShouldBe(queueHeadMove.Actions.First());
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_WhenDropActionIsCloser_ReturnPickAction()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            source: AgvGraphGenerator.SourceFar.Name,
            destination: AgvGraphGenerator.DestinationClose.Name
        );
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Name);

        var fixture = new ChooseAgvActionGroupTestFixture<ShortestRouteChooser>()
            .WithNotStartedActionGroup(actionGroup)
            .WithAgvHasRouteToDestination(agv, actionGroup, 1f)
            .WithAgvAtQueueHead([])
            .Build();

        var chosenActionGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldNotBeNull();
        chosenActionGroup.Actions.First().ShouldBe(actionGroup.Actions.First());
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_FirstActionOfActionGroupIsIncomplete_ReturnNull()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            source: string.Empty,
            destination: AgvGraphGenerator.DestinationClose.Name
        );
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Name);

        var fixture = new ChooseAgvActionGroupTestFixture<ShortestRouteChooser>()
            .WithNotStartedActionGroup(actionGroup)
            .WithAgvHasRouteToDestination(agv, actionGroup, 1f)
            .WithAgvAtQueueHead([])
            .Build();

        var chosenActionGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_SecondActionOfTheOneActionGroupHasLockedTarget_ReturnNull()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(
            source: AgvGraphGenerator.SourceClose.Name,
            destination: AgvGraphGenerator.DestinationClose.Name
        );
        List<string> lockedNodes = [AgvGraphGenerator.DestinationClose.Name];
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Name);

        var fixture = new ChooseAgvActionGroupTestFixture<ShortestRouteChooser>()
            .WithNotStartedActionGroup(actionGroup)
            .WithLockedNodes(lockedNodes)
            .WithAgvHasRouteToDestination(agv, actionGroup, 1f)
            .WithAgvAtQueueHead([])
            .Build();

        var chosenActionGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_LowestCostActionGroupHasLockedDestination_ReturnHigherCostActionGroup()
    {
        var actionGroupClose = AgvActionGenerator.GeneratePickDropActionGroup(
            source: AgvGraphGenerator.SourceClose.Name,
            destination: AgvGraphGenerator.DestinationClose.Name
        );
        var actionGroupFar = AgvActionGenerator.GeneratePickDropActionGroup(
            source: AgvGraphGenerator.SourceFar.Name,
            destination: AgvGraphGenerator.DestinationFar.Name
        );
        List<string> lockedNodes = [AgvGraphGenerator.DestinationClose.Name];
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Name);

        var fixture = new ChooseAgvActionGroupTestFixture<ShortestRouteChooser>()
            .WithNotStartedActionGroup(actionGroupClose)
            .WithNotStartedActionGroup(actionGroupFar)
            .WithLockedNodes(lockedNodes)
            .WithAgvHasRouteToDestination(agv, actionGroupClose, 1f)
            .WithAgvHasRouteToDestination(agv, actionGroupFar, 5f)
            .WithAgvAtQueueHead([])
            .Build();

        var chosenActionGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup!.Actions.FirstOrDefault()?.Target.Node!.Name.ShouldBe(AgvGraphGenerator.SourceFar.Name);
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_ChargingActionGroupWithNotLockedCharger_ReturnChargingActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Name);
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(
            targetNodeName: AgvGraphGenerator.ChargingClose.Name,
            assignedAgv: agv.CorrelationId
        );
        List<string> lockedNodes = [AgvGraphGenerator.ChargingFar.Name];

        var fixture = new ChooseAgvActionGroupTestFixture<ShortestRouteChooser>()
            .WithNotStartedActionGroup(actionGroup)
            .WithLockedNodes(lockedNodes)
            .WithAgvHasRouteToDestination(agv, actionGroup, 1f)
            .Build();

        var chosenActionGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldNotBeNull();
        chosenActionGroup.IsChargingOrder.ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_ChargingActionGroupWithLockedCharger_ReturnChargingActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Name);
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(
            targetNodeName: AgvGraphGenerator.ChargingClose.Name,
            assignedAgv: agv.CorrelationId
        );
        List<string> lockedNodes = [AgvGraphGenerator.ChargingClose.Name];

        var fixture = new ChooseAgvActionGroupTestFixture<ShortestRouteChooser>()
            .WithNotStartedActionGroup(actionGroup)
            .WithLockedNodes(lockedNodes)
            .WithAgvHasRouteToDestination(agv, actionGroup, 1f)
            .WithAgvAtQueueHead([])
            .Build();

        var chosenActionGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldNotBeNull();
        chosenActionGroup.IsChargingOrder.ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_AgvIsQueueingNoManualOrAssignedActionGroups_ReturnNull()
    {
        var actionGroup1 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceFar.Name);
        var actionGroup2 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceClose.Name); // Closest pick action
        var actionGroup3 = AgvActionGenerator.GeneratePickDropActionGroup(source: AgvGraphGenerator.SourceUnreachable.Name);
        string agvLastNodeId = AgvGraphGenerator.ParkClose.Name;
        var agv = AgvGenerator.GenerateIdleAgv(0, agvLastNodeId);
        agv.IsQueueing = true;

        var fixture = new ChooseAgvActionGroupTestFixture<ShortestRouteChooser>()
            .WithNotStartedActionGroup(actionGroup1)
            .WithNotStartedActionGroup(actionGroup2)
            .WithNotStartedActionGroup(actionGroup3)
            .WithAgvHasRouteToDestination(agv, actionGroup1.Actions.First().Target.Node!.Name, actionGroup1.Actions.Last().Target.Node!.Name, actionGroup1, 10f)
            .WithAgvHasRouteToDestination(agv, actionGroup2.Actions.First().Target.Node!.Name, actionGroup2.Actions.Last().Target.Node!.Name, actionGroup2, 2f)
            .WithAgvHasRouteToDestination(agv, actionGroup3.Actions.First().Target.Node!.Name, actionGroup3.Actions.Last().Target.Node!.Name, actionGroup3, routeCosts: null)
            .WithAgvAtQueueHead([])
            .Build();

        var chosenActionGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_AgvIsQueueingChargingActionGroupWith_ReturnChargingActionGroup()
    {
        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Name);
        agv.IsQueueing = true;
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup(
            targetNodeName: AgvGraphGenerator.ChargingClose.Name,
            assignedAgv: agv.CorrelationId
        );
        List<string> lockedNodes = [AgvGraphGenerator.ChargingFar.Name];

        var fixture = new ChooseAgvActionGroupTestFixture<ShortestRouteChooser>()
            .WithNotStartedActionGroup(actionGroup)
            .WithLockedNodes(lockedNodes)
            .WithAgvHasRouteToDestination(agv, actionGroup, 1f)
            .WithAgvAtQueueHead([])
            .Build();

        var chosenActionGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup.ShouldNotBeNull();
        chosenActionGroup.IsChargingOrder.ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetNotStartedActionGroupAsync_AgvIsAtQueueHead_ReturnActionGroup()
    {
        var queue = QueueGenerator.GenerateEmptyQueue();

        var actionGroupClose = AgvActionGenerator.GeneratePickDropActionGroup(
            source: AgvGraphGenerator.SourceClose.Name,
            destination: AgvGraphGenerator.DestinationClose.Name
        );

        var agv = AgvGenerator.GenerateIdleAgv(0, AgvGraphGenerator.ParkClose.Name);

        queue.TryEnqueue(agv.CorrelationId, out var _);

        var fixture = new ChooseAgvActionGroupTestFixture<ShortestRouteChooser>()
            .WithNotStartedActionGroup(actionGroupClose)
            .WithLockedNodes([])
            .WithAgvHasRouteToDestination(agv, actionGroupClose, 1f)
            .WithAgvAtQueueHead([agv.CorrelationId])
            .WithQueue(queue)
            .Build();

        var chosenActionGroup = await fixture.Service.GetNotStartedActionGroupAsync(agv, null);
        chosenActionGroup!.Actions.FirstOrDefault()?.Target.Node!.Name.ShouldBe(AgvGraphGenerator.SourceClose.Name);
    }
}