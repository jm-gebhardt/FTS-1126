using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Errors;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Order;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.StateMachines;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Order;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.StateMachines;

[TestClass]
public class OrderExecutionStateMachineTest
{
    [TestMethod]
    public async Task Consume_StartOrder_ValidData_PublishesOrderTransitionsToCreated()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), currentOrder: Guid.NewGuid(), currentFleetManagerActionId: Guid.NewGuid());
        var order = OrderGenerator.GeneratePickOrder("PS03", "AP01", 5, agv);

        var fixture = new OrderExecutionStationMachineTestFixture(TimeSpan.FromSeconds(2))
            .Build();

        var startOrderCommand = new StartOrder(agv.CurrentOrder!.Value, order);

        await fixture.PublishMessageAsync(startOrderCommand);

        var saga = fixture.GetSaga(startOrderCommand.CorrelationId);

        saga.CurrentState.Name.ShouldBe(nameof(OrderExecutionStateMachine.Created));
        saga.Order.OrderId.ShouldBe(order.OrderId);
        saga.Order.Route.IsEmpty.ShouldBeFalse();
        saga.Order.Base.IsEmpty.ShouldBeFalse();

        await fixture.ShouldHavePublishedAsync<MqttOrder>();
        var mqttOrder = await fixture.GetFirstPublishedMessageAsync<MqttOrder>();
        mqttOrder.HeaderId.ShouldBe((uint)1);
        mqttOrder.Manufacturer.ShouldBe(agv.Manufacturer);
        mqttOrder.SerialNumber.ShouldBe(agv.SerialNumber);
        mqttOrder.Nodes.Select(n => n.NodeId).ToList().ShouldBeEquivalentTo(saga.Order.Route.Nodes.Select(n => n.NodeId).ToList());
    }

    [TestMethod]
    public async Task Consume_StartOrder_InitializationFails_PublishesActionFailedByAgvTransitionsToFinal()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), currentOrder: Guid.NewGuid(), currentFleetManagerActionId: Guid.NewGuid());
        var order = new Order{ PlannedRoute = new Route([], []), AssignedAgv = agv};

        var fixture = new OrderExecutionStationMachineTestFixture(TimeSpan.FromSeconds(2))
            .Build();

        var startOrderCommand = new StartOrder(agv.CurrentOrder!.Value, order);

        await fixture.PublishMessageAsync(startOrderCommand);

        var saga = fixture.GetSaga(startOrderCommand.CorrelationId);

        saga.CurrentState.Name.ShouldBe(nameof(OrderExecutionStateMachine.Final));
        await fixture.ShouldNotHavePublishedAsync<MqttOrder>();
        await fixture.ShouldHavePublishedAsync<ActionFailedByAgv>();
        await fixture.ShouldHavePublishedAsync<LogOccurred>();
        var logOccurred = await fixture.GetFirstPublishedMessageAsync<LogOccurred>();
        logOccurred.Entry.Variables[LogLabels.Reason].ShouldBe(OrderErrors.InitializationFailed.Description);
    }

    [TestMethod]
    public async Task Consume_OrderStateReceived_CreatedToRunning_PublishesActionStartedByAgv()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), currentOrder: Guid.NewGuid(), currentFleetManagerActionId: Guid.NewGuid());
        var order = OrderGenerator.GeneratePickOrder("PS03", "AP01", 5, agv);

        var fixture = new OrderExecutionStationMachineTestFixture(TimeSpan.FromSeconds(2))
            .Build();

        // 1) Start order
        var startOrderCommand = new StartOrder(agv.CurrentOrder!.Value, order);
        await fixture.PublishMessageAsync(startOrderCommand);

        // 2) Receive first status from agv
        var saga = fixture.GetSaga(startOrderCommand.CorrelationId);
        var status = OrderGenerator.GenerateOrderState(order.OrderId, 0, 0, false);

        await fixture.PublishMessageAsync(new OrderStateReceived(startOrderCommand.CorrelationId, status));
        saga.Order.Status.ShouldBeEquivalentTo(status);
        saga.CurrentState.Name.ShouldBe(nameof(OrderExecutionStateMachine.Running));
        await fixture.ShouldHavePublishedAsync<ActionStartedByAgv>();
        await fixture.ShouldHavePublishedAsync<LogOccurred>();
    }

    [TestMethod]
    public async Task Consume_OrderStateReceived_ValidationByAgvFails_PublishesActionFailedByAgv()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), currentOrder: Guid.NewGuid(), currentFleetManagerActionId: Guid.NewGuid());
        var order = OrderGenerator.GeneratePickOrder("PS03", "AP01", 5, agv);

        var fixture = new OrderExecutionStationMachineTestFixture(TimeSpan.FromSeconds(2))
            .Build();

        // 1) Start order
        var startOrderCommand = new StartOrder(agv.CurrentOrder!.Value, order);
        await fixture.PublishMessageAsync(startOrderCommand);

        // 2) Receive first status from agv
        var saga = fixture.GetSaga(startOrderCommand.CorrelationId);
        var error = OrderGenerator.GenerateAgvError(Vda5050Specifications.OrderErrors.ValidationError, [new ErrorReference{ReferenceKey = "Validation", ReferenceValue = "Failed for reason xy"}]);
        var status = OrderGenerator.GenerateOrderState(order.OrderId, 0, 0, false, errors: [error]);

        await fixture.PublishMessageAsync(new OrderStateReceived(startOrderCommand.CorrelationId, status));
        saga.CurrentState.Name.ShouldBe(nameof(OrderExecutionStateMachine.Final));
        await fixture.ShouldHavePublishedAsync<ActionFailedByAgv>();
        await fixture.ShouldHavePublishedAsync<LogOccurred>();
        var logOccurred = await fixture.GetFirstPublishedMessageAsync<LogOccurred>();
        logOccurred.Entry.Variables[LogLabels.Reason].ShouldBe(string.Join(",", error.ErrorReferences!.Select(e => e.ToString()).ToList()));
    }

    [TestMethod]
    public async Task Consume_OrderStateReceived_OrderErrorByAgv_PublishesActionFailedByAgvTransitionsToFinal()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), currentOrder: Guid.NewGuid(), currentFleetManagerActionId: Guid.NewGuid());
        var order = OrderGenerator.GeneratePickOrder("PS03", "AP01", 5, agv);

        var fixture = new OrderExecutionStationMachineTestFixture(TimeSpan.FromSeconds(2))
            .Build();

        var startOrderCommand = new StartOrder(agv.CurrentOrder!.Value, order);
        await fixture.PublishMessageAsync(startOrderCommand);

        // Transition to Running
        var saga = fixture.GetSaga(startOrderCommand.CorrelationId);
        await fixture.PublishMessageAsync(new OrderStateReceived(startOrderCommand.CorrelationId,
            OrderGenerator.GenerateOrderState(order.OrderId, 0, 0, false)));
        saga.CurrentState.Name.ShouldBe(nameof(OrderExecutionStateMachine.Running));

        // AGV reports an orderError (e.g., AGV got stuck)
        var error = OrderGenerator.GenerateAgvError(Vda5050Specifications.OrderErrors.OrderError,
            [new ErrorReference { ReferenceKey = "reason", ReferenceValue = "AGV stuck" }]);
        await fixture.PublishMessageAsync(new OrderStateReceived(startOrderCommand.CorrelationId,
            OrderGenerator.GenerateOrderState(order.OrderId, 0, 2, false, errors: [error])));

        await fixture.ShouldHavePublishedAsync<ActionFailedByAgv>();
        // LogOccurred(1) = ActionStarted log, LogOccurred(2) = ActionFailed log
        var failedLog = await fixture.GetFirstPublishedMessageAsync<LogOccurred>(2);
        failedLog.Entry.Variables[LogLabels.Reason].ShouldBe(string.Join(",", error.ErrorReferences!.Select(e => e.ToString())));
    }

    [TestMethod]
    public async Task Consume_OrderCancellationRequested_InCreated_PublishesMqttInstantActionsWithCancelOrder()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), currentOrder: Guid.NewGuid(), currentFleetManagerActionId: Guid.NewGuid());
        var order = OrderGenerator.GeneratePickOrder("PS03", "AP01", 5, agv);

        var fixture = new OrderExecutionStationMachineTestFixture(TimeSpan.FromSeconds(2))
            .Build();

        var startOrderCommand = new StartOrder(agv.CurrentOrder!.Value, order);
        await fixture.PublishMessageAsync(startOrderCommand);
        var saga = fixture.GetSaga(startOrderCommand.CorrelationId);
        saga.CurrentState.Name.ShouldBe(nameof(OrderExecutionStateMachine.Created));

        await fixture.PublishMessageAsync(new OrderCancellationRequested(startOrderCommand.CorrelationId));

        await fixture.ShouldHavePublishedAsync<MqttInstantActions>();
        var instantActions = await fixture.GetFirstPublishedMessageAsync<MqttInstantActions>();
        instantActions.Actions.Count.ShouldBe(1);
        instantActions.Actions.Single().ActionType.ShouldBe(Vda5050Specifications.ActionTypes.CancelOrder);
        instantActions.Manufacturer.ShouldBe(agv.Manufacturer);
        instantActions.SerialNumber.ShouldBe(agv.SerialNumber);
        saga.CurrentState.Name.ShouldBe(nameof(OrderExecutionStateMachine.Created));
    }

    [TestMethod]
    public async Task Consume_OrderCancellationRequested_InRunning_PublishesMqttInstantActionsWithCancelOrder()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), currentOrder: Guid.NewGuid(), currentFleetManagerActionId: Guid.NewGuid());
        var order = OrderGenerator.GeneratePickOrder("PS03", "AP01", 5, agv);

        var fixture = new OrderExecutionStationMachineTestFixture(TimeSpan.FromSeconds(2))
            .Build();

        var startOrderCommand = new StartOrder(agv.CurrentOrder!.Value, order);
        await fixture.PublishMessageAsync(startOrderCommand);

        // Transition to Running
        var saga = fixture.GetSaga(startOrderCommand.CorrelationId);
        await fixture.PublishMessageAsync(new OrderStateReceived(startOrderCommand.CorrelationId,
            OrderGenerator.GenerateOrderState(order.OrderId, 0, 0, false)));
        saga.CurrentState.Name.ShouldBe(nameof(OrderExecutionStateMachine.Running));

        await fixture.PublishMessageAsync(new OrderCancellationRequested(startOrderCommand.CorrelationId));

        await fixture.ShouldHavePublishedAsync<MqttInstantActions>();
        var instantActions = await fixture.GetFirstPublishedMessageAsync<MqttInstantActions>();
        instantActions.Actions.Single().ActionType.ShouldBe(Vda5050Specifications.ActionTypes.CancelOrder);
        saga.CurrentState.Name.ShouldBe(nameof(OrderExecutionStateMachine.Running));
    }

    [TestMethod]
    public async Task Consume_StopChargingRequested_InCreated_PublishesMqttInstantActionsWithStopCharging()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), currentOrder: Guid.NewGuid(), currentFleetManagerActionId: Guid.NewGuid());
        var order = OrderGenerator.GeneratePickOrder("PS03", "AP01", 5, agv);

        var fixture = new OrderExecutionStationMachineTestFixture(TimeSpan.FromSeconds(2))
            .Build();

        var startOrderCommand = new StartOrder(agv.CurrentOrder!.Value, order);
        await fixture.PublishMessageAsync(startOrderCommand);
        var saga = fixture.GetSaga(startOrderCommand.CorrelationId);

        await fixture.PublishMessageAsync(new StopChargingRequested(startOrderCommand.CorrelationId));

        await fixture.ShouldHavePublishedAsync<MqttInstantActions>();
        var instantActions = await fixture.GetFirstPublishedMessageAsync<MqttInstantActions>();
        instantActions.Actions.Count.ShouldBe(1);
        instantActions.Actions.Single().ActionType.ShouldBe(Vda5050Specifications.ActionTypes.StopCharging);
        instantActions.Manufacturer.ShouldBe(agv.Manufacturer);
        instantActions.SerialNumber.ShouldBe(agv.SerialNumber);
        saga.CurrentState.Name.ShouldBe(nameof(OrderExecutionStateMachine.Created));
    }

    [TestMethod]
    public async Task Consume_OrderResendRequested_InRunning_PublishesAdditionalMqttOrder()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), currentOrder: Guid.NewGuid(), currentFleetManagerActionId: Guid.NewGuid());
        var order = OrderGenerator.GeneratePickOrder("PS03", "AP01", 5, agv);

        var fixture = new OrderExecutionStationMachineTestFixture(TimeSpan.FromSeconds(2))
            .Build();

        var startOrderCommand = new StartOrder(agv.CurrentOrder!.Value, order);
        await fixture.PublishMessageAsync(startOrderCommand);

        // Transition to Running
        var saga = fixture.GetSaga(startOrderCommand.CorrelationId);
        await fixture.PublishMessageAsync(new OrderStateReceived(startOrderCommand.CorrelationId,
            OrderGenerator.GenerateOrderState(order.OrderId, 0, 0, false)));
        saga.CurrentState.Name.ShouldBe(nameof(OrderExecutionStateMachine.Running));

        await fixture.PublishMessageAsync(new OrderResendRequested(startOrderCommand.CorrelationId));

        ICollection<MqttOrder> publishedOrders = fixture.GetPublishedMessages<MqttOrder>();
        publishedOrders.Count.ShouldBe(2);
        publishedOrders.Last().HeaderId.ShouldBe(2u);
        saga.CurrentState.Name.ShouldBe(nameof(OrderExecutionStateMachine.Running));
    }

    [TestMethod]
    public async Task Consume_OrderUpdateRequested_StitchingNodeNotInNewRoute_PublishesActionFailedByAgvKeepsState()
    {
        // After initialization the full route is released; the stitching node (last released base node) is "AP01" (seqId=8).
        // A new route that does not include "AP01" cannot be stitched → update fails.
        var agv = AgvGenerator.Generate(Guid.NewGuid(), currentOrder: Guid.NewGuid(), currentFleetManagerActionId: Guid.NewGuid());
        var order = OrderGenerator.GeneratePickOrder("PS03", "AP01", 5, agv);

        var fixture = new OrderExecutionStationMachineTestFixture(TimeSpan.FromSeconds(2))
            .Build();

        var startOrderCommand = new StartOrder(agv.CurrentOrder!.Value, order);
        await fixture.PublishMessageAsync(startOrderCommand);
        var saga = fixture.GetSaga(startOrderCommand.CorrelationId);
        saga.CurrentState.Name.ShouldBe(nameof(OrderExecutionStateMachine.Created));

        var newOrder = new Order
        {
            OrderId = order.OrderId,
            OrderUpdateId = 1,
            AssignedAgv = agv,
            PlannedRoute = RouteGenerator.GenerateFromNodeIds(["PS03", "DIFFERENT_NODE", "DIFFERENT_END"]),
            Route = new Route([], [])
        };
        await fixture.PublishMessageAsync(new OrderUpdateRequested(startOrderCommand.CorrelationId, newOrder, null));

        saga.CurrentState.Name.ShouldBe(nameof(OrderExecutionStateMachine.Created));
        await fixture.ShouldHavePublishedAsync<ActionFailedByAgv>();
        var logOccurred = await fixture.GetFirstPublishedMessageAsync<LogOccurred>();
        logOccurred.Entry.Variables[LogLabels.Reason].ShouldBe(OrderErrors.OrderRouteUpdateStitchingNodeNotFound.Description);
    }
}