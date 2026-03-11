using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Order;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Order;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.CommandHandler;

[TestClass]
public class ActionCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_StartActionCommand_ValidData_PublishesStartOrder()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid());
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 5,  assignedAgv: agv);
        var fixture = new ActionCommandHandlerTestFixture()
            .AddOrder(order)
            .AddAgv(agv);

        await fixture.PublishMessageAsync(new StartActionCommand(agv.CorrelationId, agv.SerialNumber, AgvActionGenerator.GenerateAction("map/NODE-10", targetNodeId: "PS01", actionType: ActionType.Pick)));

        await fixture.ShouldHavePublishedAsync<StartOrder>();
        fixture.VerifyAgvUpdated();
    }

    [TestMethod]
    public async Task Consume_UpdateActionCommand_NoCurrentOrder_PublishesStartOrder()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid());
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 5, assignedAgv: agv);
        var fixture = new ActionCommandHandlerTestFixture()
            .AddOrder(order)
            .AddAgv(agv);

        await fixture.PublishMessageAsync(new UpdateActionCommand(agv.CorrelationId, agv.SerialNumber, AgvActionGenerator.GenerateAction("map/NODE-10", targetNodeId: "PS01", actionType: ActionType.Pick)));

        await fixture.ShouldHavePublishedAsync<StartOrder>();
        fixture.VerifyAgvUpdated();
    }

    [TestMethod]
    public async Task Consume_UpdateActionCommand_AgvRunsDifferentOrder_PublishesActionFailedByAgv()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), currentOrder: Guid.NewGuid());
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 5, assignedAgv: agv);
        var fixture = new ActionCommandHandlerTestFixture()
            .AddOrder(order)
            .AddAgv(agv);

        await fixture.PublishMessageAsync(new UpdateActionCommand(agv.CorrelationId, agv.SerialNumber, AgvActionGenerator.GenerateAction("map/NODE-10", targetNodeId: "PS01", actionType: ActionType.Pick)));

        await fixture.ShouldHavePublishedAsync<ActionFailedByAgv>();
        await fixture.ShouldHavePublishedAsync<LogOccurred>();
    }

    [TestMethod]
    public async Task Consume_UpdateActionCommand_ValidData_PublishesOrderUpdateRequested()
    {
        var action = AgvActionGenerator.GenerateAction("map/NODE-10", targetNodeId: "PS01", actionType: ActionType.Pick);
        var agv = AgvGenerator.Generate(Guid.NewGuid(), currentOrder: action.CorrelationId, currentFleetManagerActionId:action.CorrelationId);
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 5, assignedAgv: agv);
        var fixture = new ActionCommandHandlerTestFixture()
            .AddOrder(order)
            .AddAgv(agv);

        await fixture.PublishMessageAsync(new UpdateActionCommand(agv.CorrelationId, agv.SerialNumber, action));

        await fixture.ShouldHavePublishedAsync<OrderUpdateRequested>();
    }

    [TestMethod]
    public async Task Consume_ResendAction_ValidData_PublishesOrderResendRequested()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid());
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 5, assignedAgv: agv);
        var fixture = new ActionCommandHandlerTestFixture()
            .AddOrder(order)
            .AddAgv(agv);

        await fixture.PublishMessageAsync(new ResendAction(agv.CorrelationId, agv.SerialNumber, AgvActionGenerator.GenerateAction("map/NODE-10", targetNodeId: "PS01", actionType: ActionType.Pick)));

        await fixture.ShouldHavePublishedAsync<OrderResendRequested>();
    }

    [TestMethod]
    public async Task Consume_AbortAndStartActionCommand_NoCurrentAgvOrderNoActionToAbort_PublishesStartOrder()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid());
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 5, assignedAgv: agv);
        var fixture = new ActionCommandHandlerTestFixture()
            .AddOrder(order)
            .AddAgv(agv);

        await fixture.PublishMessageAsync(new AbortAndStartActionCommand(agv.CorrelationId, agv.SerialNumber, AgvActionGenerator.GenerateAction("map/NODE-10", targetNodeId: "PS01", actionType: ActionType.Pick), null));

        await fixture.ShouldHavePublishedAsync<StartOrder>();
        fixture.VerifyAgvUpdated();
    }

    [TestMethod]
    public async Task Consume_AbortAndStartActionCommand_ValidData_PublishesOrderUpdateRequested()
    {
        var action = AgvActionGenerator.GenerateAction("map/NODE-10", targetNodeId: "PS01", actionType: ActionType.Pick);
        var agv = AgvGenerator.Generate(Guid.NewGuid(), currentOrder: action.CorrelationId, currentFleetManagerActionId: action.CorrelationId);
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 5, assignedAgv: agv);
        var fixture = new ActionCommandHandlerTestFixture()
            .AddOrder(order)
            .AddAgv(agv);

        var command = new AbortAndStartActionCommand(agv.CorrelationId, agv.SerialNumber, AgvActionGenerator.GenerateAction("map/NODE-10", targetNodeId: "PS01", actionType: ActionType.Pick), Guid.NewGuid());
        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<OrderUpdateRequested>();
        fixture.VerifyAgvUpdated();
        agv.CurrentFleetManagerActionId = command.AbortedActionCorrelationId;
        agv.CurrentOrder = action.CorrelationId;
    }
}