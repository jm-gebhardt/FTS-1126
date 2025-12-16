using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.KaeserSidecar.Messages;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Moq;
using Shouldly;
using AgvActionGenerator = Gebhardt.Acc.TransportOrder.Test.Helper.AgvActionGenerator;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.CommandHandler;

[TestClass]
public class AgvActionCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_StartActionCommand_ValidData_PublishesFreeStation()
    {
        string stationId = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.Actions.First().Nodes = [stationId.ToGraphNode()];
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var agv = actionGroup.AssignedAgv!.Value;
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new StartActionCommand(agv, agv.ToString(), actionGroupMessageContent.Actions.First()));
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
        var freeStation = await fixture.GetFirstPublishedMessageAsync<FreeStationOccupancy>();
        freeStation.NodeId.ShouldBe(stationId.ToGuid());
        await fixture.ShouldNotHavePublishedAsync<ReserveStation>();
        fixture.VerifyActionDequeued(actionGroup.Actions[0].CorrelationId, Times.Never());
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_DropAtAkl_PublishesReserveStation()
    {
        string stationId = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), destination: stationId);
        actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Nodes = [new GraphNode("station".ToGuid(), "station"), stationId.ToGraphNode()];
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var agv = actionGroup.AssignedAgv!.Value;
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var prismatStation = new Station(orderDetails.Source, orderDetails.Destination, stationId.ToGuid(), stationId);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithPrismatStation(prismatStation)
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new StartActionCommand(agv, agv.ToString(), actionGroupMessageContent.Actions.First(a => a.ActionType == ActionType.Drop)));
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
        await fixture.ShouldHavePublishedAsync<ReserveStation>();
        await fixture.ShouldNotHavePublishedAsync<FreeStationOccupancy>();
        fixture.VerifyActionDequeued(actionGroup.Actions[0].CorrelationId, Times.Never());
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_MoveToAkl_PublishesReserveStation()
    {
        string stationId = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup(assignedAgv: Guid.NewGuid(), targetNodeId: stationId);
        actionGroup.Actions.First(a => a.ActionType == ActionType.Move).Nodes = [new GraphNode("station".ToGuid(), "station"), stationId.ToGraphNode()];
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var agv = actionGroup.AssignedAgv!.Value;
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var prismatStation = new Station(orderDetails.Source, orderDetails.Destination, stationId.ToGuid(), stationId);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithPrismatStation(prismatStation)
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new StartActionCommand(agv, agv.ToString(), actionGroupMessageContent.Actions.First(a => a.ActionType == ActionType.Move)));
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
        await fixture.ShouldHavePublishedAsync<ReserveStation>();
        await fixture.ShouldNotHavePublishedAsync<FreeStationOccupancy>();
        fixture.VerifyActionDequeued(actionGroup.Actions[0].CorrelationId, Times.Never());
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_ManualPickAtAkl_PublishesReserveStation()
    {
        string stationId = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: stationId, isManual: true);
        actionGroup.Actions.First(a => a.ActionType == ActionType.Pick).Nodes = [new GraphNode("station".ToGuid(), "station"), stationId.ToGraphNode()];
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var agv = actionGroup.AssignedAgv!.Value;
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var prismatStation = new Station(orderDetails.Source, orderDetails.Destination, stationId.ToGuid(), stationId);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithPrismatStation(prismatStation)
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new StartActionCommand(agv, agv.ToString(), actionGroupMessageContent.Actions.First(a => a.ActionType == ActionType.Pick)));
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
        await fixture.ShouldHavePublishedAsync<ReserveStation>();
        await fixture.ShouldNotHavePublishedAsync<FreeStationOccupancy>();
        fixture.VerifyActionDequeued(actionGroup.Actions[0].CorrelationId, Times.Never());
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_OldLastCompletedAction_LastActionWasAbortedOnStation_PublishesFreeStation()
    {
        string stationId = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.Actions.First().Nodes = [stationId.ToGraphNode()];
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var agv = actionGroup.AssignedAgv!.Value;
        var abortedDate = DateTime.UtcNow.AddMinutes(2);
        var completedActionGroup = AgvActionGenerator.GenerateChargeActionGroup("charger2", assignedAgv: agv, completedDate: abortedDate.AddMinutes(-5), actionStateCharge: ActionState.Completed);
        var abortedActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv, source: stationId, pickActionState: ActionState.Aborted, completedDate: abortedDate);

        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup)
            .WithLastAbortedAction(abortedActionGroup.Actions.Single(a => a.State == ActionState.Aborted))
            .WithLastCompletedAction(completedActionGroup.Actions.Last());

        await fixture.PublishMessageAsync(new StartActionCommand(agv, agv.ToString(), actionGroupMessageContent.Actions.First()));
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
        var freeStation = await fixture.GetFirstPublishedMessageAsync<FreeStationOccupancy>();
        freeStation.NodeId.ShouldBe(stationId.ToGuid());
        await fixture.ShouldNotHavePublishedAsync<ReserveStation>();
        fixture.VerifyActionDequeued(actionGroup.Actions[0].CorrelationId, Times.Never());
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_ShortLoopPickAgvAlreadyAtStation_PublishesAgvStartedPick()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Source];
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.Actions.First().Nodes = [station.ToGraphNode()];
        var agv = actionGroup.AssignedAgv!.Value;
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new StartActionCommand(agv, agv.ToString(), actionGroupMessageContent.Actions.First()));
        await fixture.ShouldHavePublishedAsync<AgvStartedPick>();
        await fixture.ShouldNotHavePublishedAsync<FreeStationOccupancy>();
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_ShortLoopDropAgvAlreadyAtStation_DoesNotPublishPublishAgvStartedPick()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Source];
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.Actions.Last().Nodes = [station.ToGraphNode(), "other nodes".ToGraphNode()];
        var agv = actionGroup.AssignedAgv!.Value;
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);

        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new StartActionCommand(agv, agv.ToString(), actionGroupMessageContent.Actions.Last()));
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_AlreadyAtConveyorStation_NoShortLoopAction_PublishesAgvStartedPick()
    {
        var orderDetails = OrderGenerator.GenerateUnknownPickTargetOrder();
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string stationId = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.OrderBy(c => c.Value).Last().Key;
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: stationId);
        actionGroup.Actions.First().Nodes = [stationId.ToGraphNode()];
        var agv = actionGroup.AssignedAgv!.Value;
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new StartActionCommand(agv, agv.ToString(), actionGroupMessageContent.Actions.First()));
        await fixture.ShouldNotHavePublishedAsync<FreeStationOccupancy>();
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_StartNodeNotConveyorStation_DoesNotPublishFreeStation()
    {
        const string station = "not conveyor station";
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.Actions.First().Nodes = [station.ToGraphNode()];
        var agv = actionGroup.AssignedAgv!.Value;
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new StartActionCommand(agv, agv.ToString(), actionGroupMessageContent.Actions.First()));
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
        await fixture.ShouldNotHavePublishedAsync<FreeStationOccupancy>();
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_NoNodes_DoesNotPublishFreeStation()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.Actions.First().Nodes = [];
        var agv = actionGroup.AssignedAgv!.Value;
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new StartActionCommand(agv, agv.ToString(), actionGroupMessageContent.Actions.First()));
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
        await fixture.ShouldNotHavePublishedAsync<FreeStationOccupancy>();
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_UnknownActionCorrelationId_DoesNotPublishFreeStation()
    {
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.Actions.First().Nodes = [station.ToGraphNode()];
        var agv = actionGroup.AssignedAgv!.Value;
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(null);

        await fixture.PublishMessageAsync(new StartActionCommand(agv, agv.ToString(), actionGroupMessageContent.Actions.First()));
        await fixture.ShouldNotHavePublishedAsync<AgvStartedPick>();
        await fixture.ShouldNotHavePublishedAsync<FreeStationOccupancy>();
    }

    [TestMethod]
    public async Task Consume_DropActionCompleted_ValidData_AddAgvToStationOccupancy()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Source];
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.Actions.First().Nodes = [station.ToGraphNode()];

        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new DropActionCompleted(actionGroup.CorrelationId, actionGroup.Actions.Single(a => a.Target.Node?.Name == station).CorrelationId, DateTime.UtcNow));
        await fixture.ShouldNotHavePublishedAsync<AgvFinishedDrop>();
        await fixture.ShouldNotHavePublishedAsync<ReserveStation>();
        fixture.VerifyReportingToConveyors(times: Times.Never());
    }

    [TestMethod]
    public async Task Consume_PickActionCompleted_PickOnShortLoop_PublishesAgvFinishedPick()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Source];
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.Actions.First().Nodes = [station.ToGraphNode()];

        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new PickActionCompleted(actionGroup.CorrelationId, actionGroup.Actions.Single(a => a.Target.Node?.Name == station).CorrelationId, DateTime.UtcNow));
        await fixture.ShouldHavePublishedAsync<AgvFinishedPick>();
        await fixture.ShouldNotHavePublishedAsync<OccupyStation>();
        fixture.VerifyReportingToConveyors(times: Times.Never());
    }

    [TestMethod]
    public async Task Consume_PickActionCompleted_ManualPickOnShortLoop_PublishesOccupyStation()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Source];
        var prismatStation = new Station(orderDetails.Source, orderDetails.Destination, station.ToGuid(), station);

        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.Actions.First().Nodes = [station.ToGraphNode()];
        actionGroup.IsManual = true;

        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithPrismatStation(prismatStation)
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new PickActionCompleted(actionGroup.CorrelationId, actionGroup.Actions.Single(a => a.Target.Node?.Name == station).CorrelationId, DateTime.UtcNow));

        await fixture.ShouldHavePublishedAsync<OccupyStation>();
        fixture.VerifyReportingToConveyors(times: Times.Never());
    }

    [TestMethod]
    public async Task Consume_PickActionCompleted_UpdatedMove_PublishesAgvFinishedShortLoopPick()
    {
        var orderDetails = OrderGenerator.GenerateUnknownPickTargetOrder();
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var actionGroupMessageContent = order.CreateActionGroup(OrderGenerator.KaeserConfiguration, Guid.NewGuid());
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Source];
        var actionGroup = AgvActionGenerator.ConvertFromMessage(actionGroupMessageContent);
        var moveAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);
        actionGroup.StartAction(moveAction.CorrelationId, [], [], DateTimeOffset.Now);
        var pickAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var targetNode = station.ToGraphNode();
        actionGroup.SetTargetForActionWithUpdate(pickAction.CorrelationId, targetNode);
        actionGroup.SetTargetForAction(pickAction.CorrelationId, targetNode);
        // we need to manually set the Node property, because EF would normally do that during retrieval
        actionGroup.Actions.First(a => a.ActionType == ActionType.Pick).Target.Node = targetNode;
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new PickActionCompleted(actionGroup.CorrelationId, moveAction.CorrelationId, DateTime.UtcNow));
        await fixture.ShouldHavePublishedAsync<AgvFinishedPick>();
        await fixture.ShouldNotHavePublishedAsync<OccupyStation>();
        fixture.VerifyReportingToConveyors(times: Times.Never());
    }

    [TestMethod]
    public async Task Consume_DropActionCompleted_DropOnShortLoop_PublishesAgvFinishedDrop()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Destination];
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), destination: station);
        actionGroup.Actions.Last().Nodes = [station.ToGraphNode()];

        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new DropActionCompleted(actionGroup.CorrelationId, actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).CorrelationId, DateTime.UtcNow));
        await fixture.ShouldHavePublishedAsync<AgvFinishedDrop>();
        await fixture.ShouldNotHavePublishedAsync<OccupyStation>();
        fixture.VerifyReportingToConveyors(order.Hu, orderDetails.Destination, station, Times.Once());
    }

    [TestMethod]
    public async Task Consume_DropActionCompleted_DropOnShortLoop_ManualAction_PublishesOccupyStation()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Destination];
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), destination: station);
        actionGroup.Actions.Last().Nodes = [station.ToGraphNode()];
        actionGroup.IsManual = true;

        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new DropActionCompleted(actionGroup.CorrelationId, actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).CorrelationId, DateTime.UtcNow));

        await fixture.ShouldHavePublishedAsync<OccupyStation>();
        fixture.VerifyReportingToConveyors(order.Hu, orderDetails.Destination, station, Times.Never());
    }

    [TestMethod]
    public async Task Consume_DropActionCompleted_UnknownAction_StationOccupancyNotUpdated()
    {
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), destination: station);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(null);

        await fixture.PublishMessageAsync(new DropActionCompleted(actionGroup.CorrelationId, actionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).CorrelationId, DateTime.UtcNow));
        await fixture.ShouldNotHavePublishedAsync<AgvFinishedDrop>();
        await fixture.ShouldNotHavePublishedAsync<OccupyStation>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCompleted_IsManual_DropAction_StationOccupancyNotUpdated()
    {
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), destination: station, completedDate: DateTime.UtcNow);
        actionGroup.IsManual = true;
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(null);

        await fixture.PublishMessageAsync(new ActionGroupCompleted(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow));
        await fixture.ShouldNotHavePublishedAsync<OccupyStation>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCompleted_NotManual_StationOccupancyNotUpdated()
    {
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), destination: station, completedDate: DateTime.UtcNow);
        actionGroup.IsManual = false;
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(null);

        await fixture.PublishMessageAsync(new ActionGroupCompleted(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow));
        await fixture.ShouldNotHavePublishedAsync<OccupyStation>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCompleted_ManualPickDropAction_TargetIsOccupancyLimited_PublishesOccupyStation()
    {
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.CompleteAction(actionGroup.Actions.First().CorrelationId, DateTimeOffset.Now);
        actionGroup.IsManual = true;
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new ActionGroupCompleted(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow));
        await fixture.ShouldHaveConsumedAsync<ActionGroupCompleted>();
        await fixture.ShouldHavePublishedAsync<OccupyStation>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCompleted_ManualMove_TargetIsAklStation_PublishesOccupyStation()
    {
        const string station = "5";
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.CompleteAction(actionGroup.Actions.First().CorrelationId, DateTimeOffset.Now);
        actionGroup.IsManual = true;
        var actionGroupMessageContent = AgvActionGenerator.ConvertToMessage(actionGroup);
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new ActionGroupCompleted(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow));
        await fixture.ShouldHaveConsumedAsync<ActionGroupCompleted>();
        await fixture.ShouldHavePublishedAsync<OccupyStation>();
    }

    [TestMethod]
    public async Task Consume_ActionAborted_TargetIsOccupancyLimited_PublishesFreeStation()
    {
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: "somewhere else");
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, source: station);
        var action = actionGroup.Actions.First();
        var actionGroupMessage = AgvActionGenerator.ConvertToMessage(actionGroup);
        var abortedAction = actionGroupMessage.Actions.First();
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup)
            .WithAgv(agv);

        await fixture.PublishMessageAsync(new ActionAborted(action.CorrelationId, abortedAction));
        await fixture.ShouldHavePublishedAsync<FreeStationOccupancy>();
    }

    [TestMethod]
    public async Task Consume_ActionAborted_AgvIsOnTarget_DoesNotPublishesFreeStation()
    {
        const string targetNode = "on that node";
        var agv = AgvGenerator.GenerateIdleAgv(lastNodeName: targetNode);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv.CorrelationId, source: targetNode);
        var action = actionGroup.Actions.First();
        var actionGroupMessage = AgvActionGenerator.ConvertToMessage(actionGroup);
        var abortedAction = actionGroupMessage.Actions.First();
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup)
            .WithAgv(agv);

        await fixture.PublishMessageAsync(new ActionAborted(action.CorrelationId, abortedAction));
        await fixture.ShouldNotHavePublishedAsync<FreeStationOccupancy>();
    }

    [TestMethod]
    public async Task Consume_ActionAborted_TargetIsNotOccupancyLimited_DoesNotPublishFreeStation()
    {
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        var action = actionGroup.Actions.First();
        var actionGroupMessage = AgvActionGenerator.ConvertToMessage(actionGroup);
        var abortedAction = actionGroupMessage.Actions.First();
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new ActionAborted(action.CorrelationId, abortedAction));
        await fixture.ShouldNotHavePublishedAsync<FreeStationOccupancy>();
    }
}