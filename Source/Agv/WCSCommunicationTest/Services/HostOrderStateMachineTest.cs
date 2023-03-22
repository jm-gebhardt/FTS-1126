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
using TestFixture = Gebhardt.StoreWare.Agv.Common.Testing.StateMachineTestFixture<Gebhardt.StoreWare.Agv.WCSCommunication.Services.HostOrderStateMachine, Gebhardt.StoreWare.Agv.WCSCommunication.States.HostOrderSaga>;
using MassTransit;
using static MassTransit.Logging.OperationName;
using Gebhardt.StoreWare.Agv.Common.Testing;
using JetBrains.Annotations;
using MassTransit.Testing;
using static Gebhardt.StoreWare.Agv.WCSCommunicationTest.Services.HostOrderStateMachineTest;

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
        }

        [TestMethod]
        public async Task Handle_ActionStartedEvent()
        {
            var saga = CreateHostOrderSaga();
            var fixture = new TestFixture();
            fixture.Add(saga);

            await fixture.TestHarness.Bus.Publish(new ActionStartedEvent(saga.CorrelationId, saga.CorrelationId));
            (await fixture.SagaHarness.Consumed.Any<ActionStartedEvent>()).ShouldBeTrue();

            //var message = fixture.TestHarness.Published.Select<HostOrderStarted>().FirstOrDefault()?.Context.Message;
            //message.ShouldNotBeNull();
            //message.CorrelationId.ShouldBe(saga.CorrelationId);

            // Get updated saga
            saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == saga.CorrelationId).First().Saga;
            saga.CurrentState.ShouldBe(nameof(HostOrderStateMachine.Started));
        }

        [TestMethod]
        public async Task Corralate_Action_By_Different_Id()
        {
            var fixture = new TestFixture();

            string orderId = "ORDER01";
            var orderCorrelationId = orderId.ToGuid();
            var orderDetails = new OrderDetails(orderCorrelationId, orderId, "s1", "d2", "leno1", HostOrderCreator.Simulation);
            var orderState = new HostOrderSaga(orderDetails);

            fixture.Add(orderState);

            await fixture.TestHarness.Bus.Publish(new CreateHostOrder(orderDetails));

            var actionId = Guid.NewGuid();
            await fixture.TestHarness.Bus.Publish(new ActionStatusChangedEvent(actionId, orderCorrelationId));
            (await fixture.TestHarness.Consumed.Any<ActionStatusChangedEvent>()).ShouldBeTrue();

            IPublishedMessage<HostOrderHandlingSucceeded>? message = fixture.TestHarness.Published.Select<HostOrderHandlingSucceeded>().FirstOrDefault();
            message.ShouldNotBeNull();
            message.Context.Message.CorrelationId.ShouldBe(orderCorrelationId);
            message.Context.Message.OrderId.ShouldBe(orderId);
        }
    }
}
