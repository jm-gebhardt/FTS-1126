using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;
using Gebhardt.StoreWare.Agv.WCSCommunication.States;
using Gebhardt.StoreWare.Agv.WCSCommunication.Messages;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Services;

[TestClass]
public class HostOrderStateMachineTest
{
    /// <summary>
    /// When HostOrder received agv Is requested
    /// </summary>
    [TestMethod]
    public async Task WhenCreateHostOrderReceived_HostOrderSagaCreated()
    {
        var fixture = new StateMachineTestFixture<HostOrderStateMachine, HostOrderSaga>();

        const string orderId = "Order#01";
        var correlationId = orderId.ToGuid();
        var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

        await fixture.TestHarness.Bus.Publish(new CreateHostOrder(orderDetails));
        (await fixture.SagaHarness.Consumed.Any<CreateHostOrder>()).ShouldBeTrue();

        var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;

        saga.CurrentState.ShouldBe("Initial");
        saga.OrderDetails.CorrelationId.ShouldBe(correlationId);
        saga.OrderDetails.OrderId.ShouldBe(orderId);
    }

    [TestMethod]
    public async Task WhenCreateHostOrderReceived_ActionGroup_Sent()
    {
        // Arrange
        var fixture = new StateMachineTestFixture<HostOrderStateMachine, HostOrderSaga>();

        const string orderId = "Order#01";
        var correlationId = orderId.ToGuid();
        var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

        // Act
        await fixture.TestHarness.Bus.Publish(new CreateHostOrder(orderDetails));

        // Assert
        (await fixture.SagaHarness.Consumed.Any<CreateHostOrder>()).ShouldBeTrue();

        (await fixture.TestHarness.Published.Any<QueueActionGroup>()).ShouldBeTrue();

        var msg = fixture.TestHarness.Published.Select<QueueActionGroup>().First().Context.Message;
        msg.CorrelationId.ShouldBe(correlationId);

        msg.ActionGroup.Creator.ShouldBe(ActionGroupCreator.Simulation);
        var pickAction = msg.ActionGroup.Actions.First();
        var dropAction = msg.ActionGroup.Actions.Last();

        pickAction.ShouldNotBeNull();
        dropAction.ShouldNotBeNull();

        pickAction.ActionType.ShouldBe(ActionType.Pick);
        pickAction.ParentId.ShouldBe(correlationId);
        pickAction.CorrelationId.ShouldNotBe(Guid.Empty);

        dropAction.ActionType.ShouldBe(ActionType.Drop);
        dropAction.ParentId.ShouldBe(correlationId);
        dropAction.CorrelationId.ShouldNotBe(Guid.Empty);

        var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
        saga.ShouldNotBeNull();
        saga.PickActionGuid.ShouldBe(pickAction.CorrelationId);
        saga.DropActionGuid.ShouldBe(dropAction.CorrelationId);
    }
}