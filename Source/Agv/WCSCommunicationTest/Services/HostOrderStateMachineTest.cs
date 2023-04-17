using Microsoft.VisualStudio.TestTools.UnitTesting;
using MassTransit;
using Shouldly;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;
using Gebhardt.StoreWare.Agv.WCSCommunication.States;
using Gebhardt.StoreWare.Agv.WCSCommunication.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;

using TestFixture = Gebhardt.StoreWare.Agv.Common.Testing.StateMachineTestFixture<Gebhardt.StoreWare.Agv.WCSCommunication.Services.HostOrderStateMachine, Gebhardt.StoreWare.Agv.WCSCommunication.States.HostOrderSaga>;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Services
{
    [TestClass]
    public class HostOrderStateMachineTest
    {
        #region Setup

        private HostOrderSaga CreateHostOrderSaga(string sagaState)
        {
            const string orderId = "Order#01";
            var correlationId = orderId.ToGuid();
            var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);
            return new HostOrderSaga(orderDetails, sagaState);
        }

        private async Task<TMessage> PublishAndConsume<TMessage>(TestFixture fixture, TMessage message) where TMessage : class, CorrelatedBy<Guid>, IActionEventParent<Guid>
        {
            await fixture.TestHarness.Bus.Publish(message);
            (await fixture.SagaHarness.Consumed.Any<TMessage>()).ShouldBeTrue();

            var consumed = fixture.TestHarness.Published.Select<TMessage>().FirstOrDefault()?.Context.Message;
            consumed.ShouldNotBeNull();

            return consumed;
        }

        private void CheckSagaState(TestFixture fixture, Guid correlationId, string sagaState)
        {
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            saga.ShouldNotBeNull();
            saga.CurrentState.ShouldBe(sagaState);
        }

        private void CheckSagaStateTransition<TMessage>(TestFixture fixture, Guid correlationId, string sagaState, Action<TMessage>? verifyMessage = null) where TMessage : class, CorrelatedBy<Guid>
        {
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            saga.ShouldNotBeNull();
            saga.CurrentState.ShouldBe(sagaState);

            var message = fixture.TestHarness.Published.Select<TMessage>().FirstOrDefault()?.Context.Message;
            message.ShouldNotBeNull();

            verifyMessage?.Invoke(message);
        }
        #endregion

        /// <summary>
        /// When HostOrder received agv Is requested
        /// </summary>
        [TestMethod]
        public async Task WhenCreateHostOrderReceived_HostOrderSagaCreated()
        {
            var fixture = new TestFixture();

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
        public async Task Handle_PickStartedEvent()
        {
            var saga = CreateHostOrderSaga(nameof(HostOrderStateMachine.Initial));
            var fixture = new TestFixture();
            fixture.Add(saga);

            var message = await PublishAndConsume(fixture, new ActionStartedEvent(saga.PickActionGuid, saga.CorrelationId, "AGV001"));
            message.CorrelationId.ShouldBe(saga.PickActionGuid);

            // Check new saga state
            CheckSagaStateTransition<HostOrderStarted>(fixture, saga.CorrelationId, nameof(HostOrderStateMachine.Started));
        }

        [TestMethod]
        public async Task Handle_PickCompletedEvent()
        {
            var saga = CreateHostOrderSaga(nameof(HostOrderStateMachine.Started));
            var fixture = new TestFixture();
            fixture.Add(saga);

            var message = await PublishAndConsume(fixture, new ActionCompletedEvent(saga.PickActionGuid, saga.CorrelationId));
            message.CorrelationId.ShouldBe(saga.PickActionGuid);

            // Check new saga state
            CheckSagaState(fixture, saga.CorrelationId, nameof(HostOrderStateMachine.Picked));
        }

        [TestMethod]
        public async Task Handle_DropStartedEvent()
        {
            var saga = CreateHostOrderSaga(nameof(HostOrderStateMachine.Picked));
            var fixture = new TestFixture();
            fixture.Add(saga);

            var message = await PublishAndConsume(fixture, new ActionStartedEvent(saga.DropActionGuid, saga.CorrelationId, "AGV001"));
            message.CorrelationId.ShouldBe(saga.DropActionGuid);

            // Check new saga state
            CheckSagaState(fixture, saga.CorrelationId, nameof(HostOrderStateMachine.Picked)); // No change
        }

        [TestMethod]
        public async Task Handle_DropCompletedEvent()
        {
            var saga = CreateHostOrderSaga(nameof(HostOrderStateMachine.Picked));
            var fixture = new TestFixture();
            fixture.Add(saga);

            var message = await PublishAndConsume(fixture, new ActionCompletedEvent(saga.DropActionGuid, saga.CorrelationId));
            message.CorrelationId.ShouldBe(saga.DropActionGuid);

            // Check new saga state
            CheckSagaStateTransition<HostOrderHandlingSucceeded>(fixture, saga.CorrelationId, nameof(HostOrderStateMachine.Final));
        }

        [TestMethod]
        [DataRow(nameof(HostOrderStateMachine.Initial), HostOrderHandlingFailed.ErrorCode.InvalidOrderData)]
        [DataRow(nameof(HostOrderStateMachine.Started), HostOrderHandlingFailed.ErrorCode.FailedByTransportLayer)]
        [DataRow(nameof(HostOrderStateMachine.Picked), HostOrderHandlingFailed.ErrorCode.FailedByTransportLayer)]
        public async Task Handle_ActionFailedEvent(string sagaState, HostOrderHandlingFailed.ErrorCode errorCode)
        {
            var saga = CreateHostOrderSaga(sagaState);
            var fixture = new TestFixture();
            fixture.Add(saga);

            var message = await PublishAndConsume(fixture, new ActionFailedEvent(saga.PickActionGuid, saga.CorrelationId));
            message.CorrelationId.ShouldBe(saga.PickActionGuid);

            // Check new saga state
            CheckSagaStateTransition<HostOrderHandlingFailed>(fixture, saga.CorrelationId, nameof(HostOrderStateMachine.Failed),
                m => m.Error.ShouldBe(errorCode)
            );
        }

        [TestMethod]
        [DataRow(nameof(HostOrderStateMachine.Initial))]
        [DataRow(nameof(HostOrderStateMachine.Started))]
        [DataRow(nameof(HostOrderStateMachine.Picked))]
        public async Task Handle_ActionCancelledEvent(string sagaState)
        {
            var saga = CreateHostOrderSaga(sagaState);
            var fixture = new TestFixture();
            fixture.Add(saga);

            var message = await PublishAndConsume(fixture, new ActionCancelledEvent(saga.PickActionGuid, saga.CorrelationId));
            message.CorrelationId.ShouldBe(saga.PickActionGuid);

            // Check new saga state
            CheckSagaStateTransition<HostOrderCancelSucceeded>(fixture, saga.CorrelationId, nameof(HostOrderStateMachine.Cancelled));
        }

        [TestMethod]
        [DataRow(nameof(HostOrderStateMachine.Initial), HostOrderCancelFailed.ErrorCode.OrderDoesNotExist)]
        [DataRow(nameof(HostOrderStateMachine.Started), HostOrderCancelFailed.ErrorCode.OrderAlreadyStarted)]
        [DataRow(nameof(HostOrderStateMachine.Picked), HostOrderCancelFailed.ErrorCode.OrderAlreadyFinished)]
        public async Task Handle_ActionCancelFailedEvent(string sagaState, HostOrderCancelFailed.ErrorCode errorCode)
        {
            var saga = CreateHostOrderSaga(sagaState);
            var fixture = new TestFixture();
            fixture.Add(saga);

            var message = await PublishAndConsume(fixture, new ActionCancelFailedEvent(saga.PickActionGuid, saga.CorrelationId));
            message.CorrelationId.ShouldBe(saga.PickActionGuid);

            // Check new saga state
            CheckSagaStateTransition<HostOrderCancelFailed>(fixture, saga.CorrelationId, nameof(HostOrderStateMachine.CancelFailed),
                m => m.Error.ShouldBe(errorCode)
            );
        }
    }
}