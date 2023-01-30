using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using CancelHostOrder = Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands.CancelHostOrder;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services
{
    [TestClass]
    public class OrderStateMachineTest
    {
        #region Setup
        /// <summary>
        /// Create OrderStateMachine test fixture with mocked services
        /// </summary>
        private class TestFixture
        {
            private readonly StateMachineTestFixture<OrderStateMachine, OrderSaga> _stateMachineTestFixture;
            private readonly Mock<IStateObserver<OrderSaga>> _mockStateObserver;
            private readonly Mock<IEventObserver<OrderSaga>> _mockOrderEventObserver;

            /// <summary>
            /// TestFixture constructor
            /// </summary>
            /// <param name="agvSaga"></param>
            /// <param name="mockAgvRegistry"></param>
            public TestFixture(AgvSaga? agvSaga = null, Mock<IAgvRegistry>? mockAgvRegistry = null)
            {
                if (mockAgvRegistry == null)
                {
                    mockAgvRegistry = new Mock<IAgvRegistry>();
                    mockAgvRegistry.Setup(s => s.GetIdleAgvs()).Returns(new EnumerableQuery<IAgv>(new List<IAgv>()));
                    mockAgvRegistry.Setup(s => s.Count).Returns(0);

                    if (agvSaga != null)
                    {
                        mockAgvRegistry.Setup(s => s.GetAgv(agvSaga.AgvId.ToGuid()))
                            .Returns(agvSaga);
                        mockAgvRegistry.Setup(s => s.Count).Returns(1);
                    }
                }

                _mockStateObserver = new Mock<IStateObserver<OrderSaga>>();
                _mockOrderEventObserver = new Mock<IEventObserver<OrderSaga>>();

                _stateMachineTestFixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>(services: s =>
                {
                    s.AddTransient(_ => mockAgvRegistry.Object);
                    s.AddSingleton(_ => _mockStateObserver.Object);
                    s.AddSingleton(_ => _mockOrderEventObserver.Object);
                });
            }

            /// <summary>
            /// Get standardized TestFixture instance
            /// </summary>
            public StateMachineTestFixture<OrderStateMachine, OrderSaga> GetTestFixture()
            {
                return _stateMachineTestFixture;
            }

            /// <summary>
            /// Get the Mock of the IStateObserver<ObserverSaga/> which is packed as a service in TestFixture 
            /// </summary>
            public Mock<IStateObserver<OrderSaga>> GetStateObserverMock()
            {
                return _mockStateObserver;
            }

            /// <summary>
            /// Get the Mock of the IEventObserver<ObserverSaga/> which is packed as a service in TestFixture 
            /// </summary>
            public Mock<IEventObserver<OrderSaga>> GetOrderEventObserverMock()
            {
                return _mockOrderEventObserver;
            }
        }

        /// <summary>
        /// Get example OrderSaga
        /// </summary>
        /// <param name="previousState">Boundary state prior to each transition</param>
        private static OrderSaga CreateTestOrderSaga(string previousState)
        {
            string orderId = "Order123";
            var orderSaga = new OrderSaga(
                orderId.ToGuid(),
                new OrderDetails(orderId.ToGuid(), orderId, "S1", "D1", "1234", HostOrderCreator.Simulation),
                previousState,
                null
            );

            return orderSaga;
        }
        #endregion

        /// <summary>
        /// When HostOrder received agv Is requested
        /// </summary>
        [TestMethod]
        public async Task When_HostOrderReceived_OrderReceived_RequestOrderQueueingCommand_Published()
        {
            var testFixtureObject = new TestFixture();
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();

            const string orderId = "Order#1";
            var correlationId = orderId.ToGuid();
            var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new HandleHostOrder(correlationId, orderDetails));
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;

            (await fixture.SagaHarness.Consumed.Any<HandleHostOrder>()).ShouldBeTrue();
            (await fixture.TestHarness.Published.Any<OrderQueuingCommand>()).ShouldBeTrue();

            saga.AssignedAgv.ShouldBeNull();
            saga.OrderDetails.OrderId.ShouldNotBeNull();
            saga.OrderDetails.Source.ShouldNotBeNull();
            saga.OrderDetails.Destination.ShouldNotBeNull();
        }

        [TestMethod]
        public async Task When_HandleHostOrder_EmptyCorrelationId_NoSagaCreated()
        {
            var testFixtureObject = new TestFixture();
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();

            const string orderId = "Order123123";
            var correlationId = orderId.ToGuid();
            var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new HandleHostOrder(correlationId, orderDetails));

            (await fixture.TestHarness.Published.Any<HandleHostOrder>()).ShouldBeTrue();
            (await fixture.SagaHarness.NotExists(Guid.Empty, TimeSpan.FromMilliseconds(10))).ShouldBeNull();
        }

        [TestMethod]
        public async Task When_HandleHostOrderWithCorrelationId_KeepsProvidedCorrelationId()
        {
            var testFixtureObject = new TestFixture();
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();
            Mock<IEventObserver<OrderSaga>> orderEventObserver = testFixtureObject.GetOrderEventObserverMock();

            const string orderId = "Order123123";
            var correlationId = Guid.NewGuid();
            var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new HandleHostOrder(correlationId, orderDetails));
            (await fixture.SagaHarness.Exists(correlationId)).ShouldBe(correlationId);

            (await fixture.SagaHarness.Consumed.Any<HandleHostOrder>()).ShouldBeTrue();

            orderEventObserver.Verify(s => s.PreExecute(It.IsAny<BehaviorContext<OrderSaga, HandleHostOrder>>()), Times.Once);
            orderEventObserver.Verify(s => s.PostExecute(It.IsAny<BehaviorContext<OrderSaga, HandleHostOrder>>()), Times.Once);
        }

        [TestMethod]
        public async Task When_TransportOrderCompleted_HostOrderHandlingSucceededPublished()
        {
            var testFixtureObject = new TestFixture();
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();

            string orderId = Guid.NewGuid().ToString();
            var correlationId = orderId.ToGuid();

            var orderState = new OrderSaga(
                correlationId,
                new OrderDetails(correlationId, orderId, "s1", "d2", "leno1", HostOrderCreator.Simulation)
            )
            {
                CurrentState = fixture.Machine.TransportOrderPicked.Name,
                AssignedAgv = "AGV-01".ToGuid()
            };

            fixture.Add(orderState);
            Mock<IEventObserver<OrderSaga>> orderEventObserver = testFixtureObject.GetOrderEventObserverMock();

            await fixture.TestHarness.Bus.Publish(new TransportOrderCompleted(correlationId, orderId));
            (await fixture.SagaHarness.Exists(correlationId)).ShouldBe(correlationId);
            (await fixture.SagaHarness.Consumed.Any<TransportOrderCompleted>()).ShouldBeTrue();

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            saga.CurrentState.ShouldBe(fixture.SagaHarness.StateMachine.Final.Name);

            IPublishedMessage<HostOrderHandlingSucceeded>? message = fixture.TestHarness.Published.Select<HostOrderHandlingSucceeded>().First();
            message.ShouldNotBeNull();
            message.Context.Message.CorrelationId.ShouldBe(correlationId);
            message.Context.Message.OrderId.ShouldBe(orderId);

            orderEventObserver.Verify(s => s.PreExecute(It.IsAny<BehaviorContext<OrderSaga, TransportOrderCompleted>>()), Times.Once);
            orderEventObserver.Verify(s => s.PostExecute(It.IsAny<BehaviorContext<OrderSaga, TransportOrderCompleted>>()), Times.Once);
        }

        [DataRow("TransportOrderSend")]
        [DataRow("TransportOrderPicked")]
        [TestMethod]
        public async Task When_TransportOrderFailed_HostOrderHandlingFailedPublished(string previousState)
        {
            var testFixtureObject = new TestFixture();
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();

            string orderId = Guid.NewGuid().ToString();
            var correlationId = orderId.ToGuid();

            var orderState = new OrderSaga(
                correlationId,
                new OrderDetails(correlationId, orderId, "s1", "d2", "leno1", HostOrderCreator.Simulation)
            )
            { CurrentState = previousState };

            fixture.Add(orderState);
            Mock<IEventObserver<OrderSaga>> orderEventObserver = testFixtureObject.GetOrderEventObserverMock();

            await fixture.TestHarness.Bus.Publish(new TransportOrderFailed(correlationId, orderId));
            (await fixture.SagaHarness.Exists(correlationId)).ShouldBe(correlationId);
            (await fixture.SagaHarness.Consumed.Any<TransportOrderFailed>()).ShouldBeTrue();

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            saga.CurrentState.ShouldBe(fixture.SagaHarness.StateMachine.OrderFailed.Name);

            orderEventObserver.Verify(s => s.PreExecute(It.IsAny<BehaviorContext<OrderSaga, TransportOrderFailed>>()), Times.Once);
            orderEventObserver.Verify(s => s.PostExecute(It.IsAny<BehaviorContext<OrderSaga, TransportOrderFailed>>()), Times.Once);
        }

        [DataRow(nameof(OrderStateMachine.OrderReceived), nameof(OrderStateMachine.OrderReceived))]
        [DataRow(nameof(OrderStateMachine.OrderFailed), nameof(OrderStateMachine.OrderReceived))]
        [DataRow(nameof(OrderStateMachine.Final), nameof(OrderStateMachine.OrderReceived))]
        [TestMethod]
        public async Task Handle_ResetTransportOrder_Message(string previousState, string resultState)
        {
            var testFixtureObject = new TestFixture();
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();

            const string orderId = "Order123";
            var correlationId = orderId.ToGuid();

            var orderState = new OrderSaga(
                correlationId,
                new OrderDetails(correlationId, orderId, "s1", "d2", "leno1", HostOrderCreator.Simulation)
            )
            { CurrentState = previousState };

            fixture.Add(orderState);

            await fixture.TestHarness.Bus.Publish(new ResetTransportOrder(correlationId));
            (await fixture.SagaHarness.Consumed.Any<ResetTransportOrder>()).ShouldBeTrue();

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            saga.CurrentState.ShouldBe(resultState);

            var sentCommand = fixture.TestHarness.Published.Select<OrderQueuingCommand>().First().Context.Message;
            sentCommand.CorrelationId.ShouldBe(correlationId);
        }

        [DataRow(nameof(OrderStateMachine.OrderReceived), nameof(OrderStateMachine.Final))]
        [DataRow(nameof(OrderStateMachine.OrderFailed), nameof(OrderStateMachine.Final))]
        [DataRow(nameof(OrderStateMachine.Final), nameof(OrderStateMachine.Final))]
        [TestMethod]
        public async Task Handle_ManuallyCompleteTransportOrder_Message_SendsHostOrderHandlingSucceeding(string previousState, string resultState)
        {
            var testFixtureObject = new TestFixture();
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();

            const string orderId = "Order123";
            var correlationId = orderId.ToGuid();

            var orderState = new OrderSaga(
                    correlationId,
                    new OrderDetails(correlationId, orderId, "s1", "d2", "leno1", HostOrderCreator.Simulation)
                )
                { CurrentState = previousState };

            fixture.Add(orderState);

            await fixture.TestHarness.Bus.Publish(new ManuallyCompleteTransportOrder(correlationId));
            (await fixture.SagaHarness.Consumed.Any<ManuallyCompleteTransportOrder>()).ShouldBeTrue();

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            saga.CurrentState.ShouldBe(resultState);

            var sentCommand = fixture.TestHarness.Published.Select<HostOrderHandlingSucceeded>().First().Context.Message;
            sentCommand.CorrelationId.ShouldBe(correlationId);
        }

        /// <summary>
        /// Examining state transition after consuming TransportOrderPickCompleted message
        /// </summary>
        [TestMethod]
        public async Task Handling_Consumption_of_TransportOrderPickCompleted()
        {
            // Arrange
            var testFixtureObject = new TestFixture();
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();
            string orderId = "Order123";
            var correlationId = orderId.ToGuid();

            var orderSaga = new OrderSaga(
                correlationId,
                new OrderDetails(correlationId, orderId, "S1", "D1", "012345", HostOrderCreator.Wms),
                fixture.Machine.TransportOrderStarted.Name,
                "AGV_01".ToGuid()
            );

            fixture.Add(orderSaga);

            Mock<IEventObserver<OrderSaga>> orderEventObserver = testFixtureObject.GetOrderEventObserverMock();

            // Act
            await fixture.TestHarness.Bus.Publish(new TransportOrderPickCompleted(correlationId, orderId));

            // Assert
            (await fixture.SagaHarness.Consumed.Any<TransportOrderPickCompleted>()).ShouldBeTrue();
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            saga.CurrentState.ShouldBe(fixture.Machine.TransportOrderPicked.Name);

            orderEventObserver.Verify(s => s.PreExecute(It.IsAny<BehaviorContext<OrderSaga, TransportOrderPickCompleted>>()), Times.Once);
            orderEventObserver.Verify(s => s.PostExecute(It.IsAny<BehaviorContext<OrderSaga, TransportOrderPickCompleted>>()), Times.Once);
        }

        /// <summary>
        /// Checks the outgoing FailedReason strings after consuming TransportOrderFailed
        /// </summary>
        [TestMethod]
        [DataRow(nameof(OrderStateMachine.TransportOrderSend))]
        [DataRow(nameof(OrderStateMachine.TransportOrderPicked))]
        public async Task TransportOrderFailed_EnumElements_Corresponds_to_its_string(string prevStateWhichHandlesTransportOrderFailed)
        {
            // Arrange
            var testFixtureObject = new TestFixture();
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();
            var order = CreateTestOrderSaga(prevStateWhichHandlesTransportOrderFailed);
            fixture.Add(order);

            // Act
            await fixture.TestHarness.Bus.Publish(new TransportOrderFailed(order.CorrelationId, order.OrderDetails.OrderId));

            // Assert
            (await fixture.SagaHarness.Consumed.Any<TransportOrderFailed>()).ShouldBeTrue();
            (await fixture.SagaHarness.Exists(order.CorrelationId)).ShouldBe(order.CorrelationId);
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == order.CorrelationId).First().Saga;
            saga.FailedReason.ShouldBe(nameof(TransportOrderFailed));
        }

        [TestMethod]
        public async Task When_OrderSaga_State_Changed_Trigger_Observer()
        {
            var testFixtureObject = new TestFixture();
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();
            Mock<IStateObserver<OrderSaga>> mockObserver = testFixtureObject.GetStateObserverMock();

            string orderId = Guid.NewGuid().ToString();
            var correlationId = orderId.ToGuid();

            var orderSaga = new OrderSaga(
                correlationId,
                new OrderDetails(correlationId, orderId, "s1", "d2", "leno1", HostOrderCreator.Simulation)
            )
            { CurrentState = fixture.Machine.TransportOrderPicked.Name };

            fixture.Add(orderSaga);

            // Trigger state change: TransportOrderPicked -> Final
            await fixture.TestHarness.Bus.Publish(new TransportOrderCompleted(correlationId, orderId));
            (await fixture.SagaHarness.Consumed.Any<TransportOrderCompleted>()).ShouldBeTrue();

            mockObserver.Verify(s => s.StateChanged(It.IsAny<BehaviorContext<OrderSaga>>(), It.IsAny<State>(), It.IsAny<State>()), Times.Once);
        }

        /// <summary>
        /// Checks the message sending according to each state after incoming CancelTransportOrder
        /// </summary>
        [DataRow(nameof(OrderStateMachine.OrderReceived), null)]
        [DataRow(nameof(OrderStateMachine.Cancelled), HostOrderCancelFailed.ErrorCode.OrderAlreadyCancelled)]
        [DataRow(nameof(OrderStateMachine.Final), HostOrderCancelFailed.ErrorCode.OrderAlreadyFinished)]
        [DataRow(nameof(OrderStateMachine.Initial), HostOrderCancelFailed.ErrorCode.OrderDoesNotExist)]
        [DataRow(nameof(OrderStateMachine.OrderFailed), HostOrderCancelFailed.ErrorCode.OrderAlreadyFailed)]
        [DataRow(nameof(OrderStateMachine.OrderQueued), HostOrderCancelFailed.ErrorCode.OrderAlreadyStarted)]
        [DataRow(nameof(OrderStateMachine.TransportOrderSend), HostOrderCancelFailed.ErrorCode.OrderAlreadyStarted)]
        [DataRow(nameof(OrderStateMachine.TransportOrderStarted), HostOrderCancelFailed.ErrorCode.OrderAlreadyStarted)]
        [DataRow(nameof(OrderStateMachine.TransportOrderPicked), HostOrderCancelFailed.ErrorCode.OrderAlreadyStarted)]
        [TestMethod]
        public async Task StateExamination_after_OrderCancel(string previousState, HostOrderCancelFailed.ErrorCode? errorCode)
        {
            // Arrange
            var testFixtureObject = new TestFixture();
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();
            var order = CreateTestOrderSaga(previousState);
            fixture.Add(order);

            // Act
            await fixture.TestHarness.Bus.Publish(new CancelHostOrder(order.CorrelationId, order.OrderDetails.OrderId));

            // Assert
            (await fixture.TestHarness.Published.Any<CancelHostOrder>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<CancelHostOrder>()).ShouldBeTrue();

            if (previousState == nameof(OrderStateMachine.OrderReceived))
            {
                (await fixture.TestHarness.Published.Any<HostOrderCancelSucceeded>()).ShouldBeTrue();
                var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == order.CorrelationId).First().Saga;
                saga.CurrentState.ShouldBe(nameof(OrderStateMachine.Cancelled));
            }
            else
            {
                (await fixture.TestHarness.Published.Any<HostOrderCancelFailed>()).ShouldBeTrue();
                var msg = fixture.TestHarness.Published.Select<HostOrderCancelFailed>().First().Context.Message;
                errorCode.ShouldBe(msg.Error);
            }
        }
    }
}
