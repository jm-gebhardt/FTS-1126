using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Dispatch;
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
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 5, assignedAgv: agv);
        var fixture = new ActionCommandHandlerTestFixture()
            .AddOrder(order)
            .AddAgv(agv);

        await fixture.PublishMessageAsync(new DispatchRouteCommand(agv.CorrelationId, agv.SerialNumber, new RoutePlan([], [], [], []), AgvActionGenerator.GenerateAction("map/NODE-10", targetNodeId: "PS01", actionType: ActionType.Pick)));

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

        await fixture.PublishMessageAsync(new UpdateDispatchedRouteCommand(agv.CorrelationId, agv.SerialNumber, new RoutePlan([], [], [], []), AgvActionGenerator.GenerateAction("map/NODE-10", targetNodeId: "PS01", actionType: ActionType.Pick)));

        await fixture.ShouldHavePublishedAsync<StartOrder>();
        fixture.VerifyAgvUpdated();
    }

    [TestMethod]
    public async Task Consume_UpdateActionCommand_ValidData_PublishesOrderUpdateRequested()
    {
        var action = AgvActionGenerator.GenerateAction("map/NODE-10", targetNodeId: "PS01", actionType: ActionType.Pick);
        var agv = AgvGenerator.Generate(Guid.NewGuid(), currentOrder: action.CorrelationId, currentFleetManagerActionId: action.CorrelationId);
        var order = OrderGenerator.GeneratePickOrder("map/NODE-10", "PS01", 5, assignedAgv: agv);
        var fixture = new ActionCommandHandlerTestFixture()
            .AddOrder(order)
            .AddAgv(agv);

        await fixture.PublishMessageAsync(new UpdateDispatchedRouteCommand(agv.CorrelationId, agv.SerialNumber, new RoutePlan([], [], [], []), action));

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

        await fixture.PublishMessageAsync(new ResendDispatchedRouteCommand(agv.CorrelationId, agv.SerialNumber, new RoutePlan([], [], [], []), AgvActionGenerator.GenerateAction("map/NODE-10", targetNodeId: "PS01", actionType: ActionType.Pick)));

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

        await fixture.PublishMessageAsync(new AbortAndDispatchRouteCommand(agv.CorrelationId, agv.SerialNumber, AgvActionGenerator.GenerateAction("map/NODE-10", targetNodeId: "PS01", actionType: ActionType.Pick), new RoutePlan([], [], [], []), null));

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

        var command = new AbortAndDispatchRouteCommand(agv.CorrelationId, agv.SerialNumber, AgvActionGenerator.GenerateAction("map/NODE-10", targetNodeId: "PS01", actionType: ActionType.Pick), new RoutePlan([], [], [], []), Guid.NewGuid());
        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<OrderUpdateRequested>();
        fixture.VerifyAgvUpdated();
        agv.CurrentFleetManagerActionId = command.AbortedActionCorrelationId;
        agv.CurrentOrder = action.CorrelationId;
    }
}