using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;
using Gebhardt.StoreWare.Agv.WCSCommunication.States;
using Gebhardt.StoreWare.Agv.WCSCommunication.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using MassTransit;
using MassTransit.Testing;

using TestFixture = Gebhardt.StoreWare.Agv.Common.Testing.StateMachineTestFixture<Gebhardt.StoreWare.Agv.WCSCommunication.Services.HostOrderStateMachine, Gebhardt.StoreWare.Agv.WCSCommunication.States.HostOrderSaga>;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Services
{
    [TestClass]
    public class HostOrderStateMachineTest
    {
        #region Setup

        private HostOrderSaga CreateHostOrderSaga(string sagaState = nameof(HostOrderStateMachine.Initial))
        {
            const string orderId = "Order#01";
            var correlationId = orderId.ToGuid();
            var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);
            return new HostOrderSaga(orderDetails, sagaState);
        }

        public async Task<TMessage> PublishAndConsume<TMessage>(TestFixture fixture, TMessage message) where TMessage : class, CorrelatedBy<Guid>, IActionEventParent<Guid>
        {
            await fixture.TestHarness.Bus.Publish(message);
            (await fixture.SagaHarness.Consumed.Any<TMessage>()).ShouldBeTrue();

            var consumed = fixture.TestHarness.Published.Select<TMessage>().FirstOrDefault()?.Context.Message;
            consumed.ShouldNotBeNull();

            return consumed;
        }

        #endregion

        /// <summary>
        /// When HostOrder received agv Is requested
        /// </summary>
        [TestMethod]
        public async Task WhenCreateHostOrderReceived_HostOrderSagaCreated()
        {
            TestFixture fixture = new TestFixture();

            const string orderId = "Order#01";
            var correlationId = orderId.ToGuid();
            var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new CreateHostOrder(orderDetails));
            (await fixture.SagaHarness.Consumed.Any<CreateHostOrder>()).ShouldBeTrue();

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;

            saga.CurrentState.ShouldBe(nameof(HostOrderStateMachine.Initial));
            saga.OrderDetails.CorrelationId.ShouldBe(correlationId);
            saga.OrderDetails.OrderId.ShouldBe(orderId);
        }

        [TestMethod]
        public async Task WhenCreateHostOrderReceived_ActionGroup_Sent()
        {
            // Arrange
            var fixture = new TestFixture();

            const string orderId = "Order#01";
            var correlationId = orderId.ToGuid();
            var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

            // Act
            await fixture.TestHarness.Bus.Publish(new CreateHostOrder(orderDetails));

            // Assert
            (await fixture.SagaHarness.Consumed.Any<CreateHostOrder>()).ShouldBeTrue();

            (await fixture.TestHarness.Published.Any<ActionGroup>()).ShouldBeTrue();

            var msg = fixture.TestHarness.Published.Select<ActionGroup>().First().Context.Message;
            msg.CorrelationId.ShouldBe(correlationId);
            var pickAction = msg.ActionList[0];
            var dropAction = msg.ActionList[1];

            pickAction.ShouldNotBeNull();
            dropAction.ShouldNotBeNull();

            pickAction.ActionType.ShouldBe(ActionType.Pick);
            pickAction.AgvId.ShouldBe(string.Empty);
            pickAction.ParentId.ShouldBe(correlationId);
            pickAction.CorrelationId.ShouldNotBe(Guid.Empty);

            dropAction.ActionType.ShouldBe(ActionType.Drop);
            dropAction.AgvId.ShouldBe(string.Empty);
            dropAction.ParentId.ShouldBe(correlationId);
            dropAction.CorrelationId.ShouldNotBe(Guid.Empty);

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            saga.ShouldNotBeNull();
            saga.PickActionGuid.ShouldBe(pickAction.CorrelationId);
            saga.DropActionGuid.ShouldBe(dropAction.CorrelationId);
            saga.CurrentState.ShouldBe(nameof(HostOrderStateMachine.Initial));
        }

        [TestMethod]
        public async Task Handle_ActionStartedEvent()
        {
            var saga = CreateHostOrderSaga();
            var fixture = new TestFixture();
            fixture.Add(saga);

            var message = await PublishAndConsume(fixture, new ActionStartedEvent(saga.PickActionGuid, saga.CorrelationId));
            message.CorrelationId.ShouldBe(saga.PickActionGuid);

            // Get updated saga
            saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == saga.CorrelationId).First().Saga;
            saga.ShouldNotBeNull();
            //saga.CurrentState.ShouldBe(nameof(HostOrderStateMachine.Started));
        }

        [TestMethod]
        public async Task Handle_ActionCompletedEvent()
        {
            var saga = CreateHostOrderSaga();
            var fixture = new TestFixture();
            fixture.Add(saga);

            var message = await PublishAndConsume(fixture, new ActionFailedEvent(saga.PickActionGuid, saga.CorrelationId));
            message.CorrelationId.ShouldBe(saga.PickActionGuid);
        }

        [TestMethod]
        public async Task Handle_ActionFailedEvent()
        {
            var saga = CreateHostOrderSaga();
            var fixture = new TestFixture();
            fixture.Add(saga);

            var message = await PublishAndConsume(fixture, new ActionFailedEvent(saga.PickActionGuid, saga.CorrelationId));
            message.CorrelationId.ShouldBe(saga.PickActionGuid);
        }

        [TestMethod]
        public async Task Handle_ActionCancelledEvent()
        {
            var saga = CreateHostOrderSaga();
            var fixture = new TestFixture();
            fixture.Add(saga);

            var message = await PublishAndConsume(fixture, new ActionCancelledEvent(saga.PickActionGuid, saga.CorrelationId));
            message.CorrelationId.ShouldBe(saga.PickActionGuid);
        }

        [TestMethod]
        public async Task Handle_ActionCancelFailedEvent()
        {
            var saga = CreateHostOrderSaga();
            var fixture = new TestFixture();
            fixture.Add(saga);

            var message = await PublishAndConsume(fixture, new ActionCancelFailedEvent(saga.PickActionGuid, saga.CorrelationId));
            message.CorrelationId.ShouldBe(saga.PickActionGuid);
        }
    }
}
