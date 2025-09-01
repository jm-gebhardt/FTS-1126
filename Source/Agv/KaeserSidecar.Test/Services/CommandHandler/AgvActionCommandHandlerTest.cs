using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.KaeserSidecar.Messages;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
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
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());
        actionGroup.Actions.First().Nodes = [station];
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
        freeStation.Station.ShouldBe(station);
        await fixture.ShouldNotHavePublishedAsync<OccupyStation>();
        fixture.VerifyActionDequeued(actionGroup.Actions[0].CorrelationId, Times.Never());
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_ShortLoopPickAgvAlreadyAtStation_PublishesAgvStartedPick()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Source];
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.Actions.First().Nodes = [station];
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
        actionGroup.Actions.Last().Nodes = [station, "other nodes"];
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
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.OrderBy(c => c.Value).Last().Key;
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.Actions.First().Nodes = [station];
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
        actionGroup.Actions.First().Nodes = [station];
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
        actionGroup.Actions.First().Nodes = [station];
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
        actionGroup.Actions.First().Nodes = [station];

        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new DropActionCompleted(actionGroup.CorrelationId, actionGroup.Actions.Single(a => a.Target.NodeId == station).CorrelationId, DateTime.UtcNow));
        await fixture.ShouldNotHavePublishedAsync<AgvFinishedDrop>();
        await fixture.ShouldNotHavePublishedAsync<OccupyStation>();
        fixture.VerifyReportingToConveyors(times: Times.Never());
    }

    [TestMethod]
    public async Task Consume_PickActionCompleted_PickOnShortLoop_PublishesAgvFinishedPick()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        string station = OrderGenerator.KaeserConfiguration.StationMapping[orderDetails.Source];
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.Actions.First().Nodes = [station];

        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new PickActionCompleted(actionGroup.CorrelationId, actionGroup.Actions.Single(a => a.Target.NodeId == station).CorrelationId, DateTime.UtcNow));
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
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.Actions.First().Nodes = [station];
        actionGroup.IsManual = true;

        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithHostOrder(order)
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new PickActionCompleted(actionGroup.CorrelationId, actionGroup.Actions.Single(a => a.Target.NodeId == station).CorrelationId, DateTime.UtcNow));

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
        actionGroup.StartAction(moveAction.CorrelationId, [], []);
        var pickAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        actionGroup.SetTargetForActionWithUpdate(pickAction.CorrelationId, station);
        actionGroup.SetTargetForAction(pickAction.CorrelationId, station);
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
        actionGroup.Actions.Last().Nodes = [station];

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
        actionGroup.Actions.Last().Nodes = [station];
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
    public async Task Consume_ActionGroupCompleted_ManualMove_TargetIsOccupancyLimited_PublishesOccupyStation()
    {
        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source: station);
        actionGroup.CompleteAction(actionGroup.Actions.First().CorrelationId);
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
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid(), source:station);
        var action = actionGroup.Actions.First();
        var actionGroupMessage = AgvActionGenerator.ConvertToMessage(actionGroup);
        var abortedAction = actionGroupMessage.Actions.First();
        var fixture = new AgvActionCommandHandlerTestFixture()
            .WithActionGroup(actionGroup);

        await fixture.PublishMessageAsync(new ActionAborted(action.CorrelationId, abortedAction));
        await fixture.ShouldHavePublishedAsync<FreeStationOccupancy>();
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