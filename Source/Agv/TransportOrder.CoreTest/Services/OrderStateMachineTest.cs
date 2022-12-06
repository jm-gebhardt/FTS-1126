using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
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

        /// <summary>
        /// Create a standardized AgvSaga for testing purposes
        /// </summary>
        /// <param name="previousState"></param>
        private AgvSaga CreateTestAgvSaga(string previousState)
        {

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvSaga = new AgvSaga(
                correlationId,
                agvId,
                currentState: previousState,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true)
            );

            return agvSaga;
        }
        #endregion

        /// <summary>
        /// When HostOrder received agv Is requested
        /// </summary>
        [TestMethod]
        public async Task When_HostOrderReceived_OrderReceived_RequestOrderAgvAssign_Published()
        {
            var testFixtureObject = new TestFixture();
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();

            const string orderId = "Order#1";
            var correlationId = orderId.ToGuid();
            var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new HandleHostOrder(correlationId, orderDetails));
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;

            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<HandleHostOrder>());
            Assert.IsTrue(await fixture.TestHarness.Published.Any<RequestOrderAgvAssign>());

            Assert.IsNull(saga.AssignedAgv);
            Assert.IsNotNull(saga.OrderDetails.OrderId);
            Assert.IsNotNull(saga.OrderDetails.Source);
            Assert.IsNotNull(saga.OrderDetails.Destination);
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AssignAgv>());

            IList<Guid> sagas = await fixture.SagaHarness.Match(s => s.CorrelationId == correlationId && s.CurrentState == fixture.SagaHarness.StateMachine.AgvRequested.Name);
            Assert.AreEqual(1, sagas.Count);
            Assert.AreEqual(fixture.SagaHarness.StateMachine.AgvRequested.Name, saga.CurrentState);
        }

        [DataRow(nameof(OrderStateMachine.AgvRequested))]
        [DataRow(nameof(OrderStateMachine.TransportOrderSend))]
        [DataRow(nameof(OrderStateMachine.TransportOrderStarted))]
        [TestMethod]
        public async Task Handle_RequestOrderAgvAssign_when_not_OrderReceived(string previousState)
        {
            // Arrange
            var orderSaga = CreateTestOrderSaga(previousState);

            var agvSaga = CreateTestAgvSaga("Idle");
            var agvList = new List<IAgv> { agvSaga };

            var mockAgvRegistry = new Mock<IAgvRegistry>();
            mockAgvRegistry.Setup(s => s.GetIdleAgvs()).Returns(new EnumerableQuery<IAgv>(agvList));

            var testFixtureObject = new TestFixture(mockAgvRegistry: mockAgvRegistry);
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();
            fixture.Add(orderSaga);

            // Act
            await fixture.TestHarness.Bus.Publish(new RequestOrderAgvAssign(orderSaga.CorrelationId));

            // Assert
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvBecameIdle>());
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

            Assert.IsTrue(await fixture.TestHarness.Published.Any<HandleHostOrder>());
            Assert.IsNull(await fixture.SagaHarness.NotExists(Guid.Empty, TimeSpan.FromMilliseconds(10)));
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
            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));

            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<HandleHostOrder>());

            orderEventObserver.Verify(s => s.PreExecute(It.IsAny<BehaviorContext<OrderSaga, HandleHostOrder>>()), Times.Once);
            orderEventObserver.Verify(s => s.PostExecute(It.IsAny<BehaviorContext<OrderSaga, HandleHostOrder>>()), Times.Once);
        }

        [TestMethod]
        public async Task When_AgvAssignSucceeded_ExecuteTransportOrderRequested()
        {
            var testFixtureObject = new TestFixture();
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();
            Mock<IEventObserver<OrderSaga>> orderEventObserver = testFixtureObject.GetOrderEventObserverMock();

            const string orderId = "Order123";
            var correlationId = orderId.ToGuid();
            var orderSaga = new OrderSaga(
                correlationId,
                new OrderDetails(correlationId, orderId, "s1", "d2", "leno1", HostOrderCreator.Simulation)
            )
            { CurrentState = fixture.Machine.AgvRequested.Name };

            fixture.Add(orderSaga);

            const string assignedAgvId = "AGV1";
            var assignedAgvCorrelationId = assignedAgvId.ToGuid();
            await fixture.TestHarness.Bus.Publish(new AgvAssignmentSucceeded
                (
                    orderSaga.CorrelationId,
                    assignedAgvId,
                    assignedAgvCorrelationId,
                    Array.Empty<string>()
                )
            );
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvAssignmentSucceeded>());

            Assert.AreEqual(fixture.SagaHarness.StateMachine.TransportOrderSend.Name, orderSaga.CurrentState);
            Assert.AreEqual(assignedAgvCorrelationId, orderSaga.AssignedAgv);
            Assert.IsTrue(await fixture.TestHarness.Published.Any<ExecuteTransportOrder>());

            orderEventObserver.Verify(s => s.PreExecute(It.IsAny<BehaviorContext<OrderSaga, AgvAssignmentSucceeded>>()), Times.Once);
            orderEventObserver.Verify(s => s.PostExecute(It.IsAny<BehaviorContext<OrderSaga, AgvAssignmentSucceeded>>()), Times.Once);
        }

        [TestMethod]
        public async Task When_AgvAssignmentFailed_NoRouteFound_HostOrderHandlingFailedPublished()
        {
            var agvSaga = CreateTestAgvSaga("Discovered");
            var testFixtureObject = new TestFixture(agvSaga);
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();
            const string orderId = "Order123";
            var correlationId = orderId.ToGuid();
            var orderSaga = new OrderSaga(
                    correlationId,
                    new OrderDetails(correlationId, orderId, "s1", "d2", "leno1", HostOrderCreator.Simulation)
                )
            { CurrentState = fixture.Machine.AgvRequested.Name };

            fixture.Add(orderSaga);

            Mock<IEventObserver<OrderSaga>> orderEventObserver = testFixtureObject.GetOrderEventObserverMock();

            await fixture.TestHarness.Bus.Publish(new AgvAssignmentFailed
                (
                    orderSaga.CorrelationId,
                    AgvAssignmentFailed.ErrorCode.NoRouteFound
                )
            );
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvAssignmentFailed>());
            Assert.IsTrue(await fixture.TestHarness.Published.Any<HostOrderHandlingFailed>());

            var message = fixture.TestHarness.Published.Select<HostOrderHandlingFailed>().First().Context.Message;
            Assert.AreEqual(correlationId, message.CorrelationId);
            Assert.AreEqual(HostOrderHandlingFailed.ErrorCode.InvalidOrderData, message.Error);

            Assert.IsNull(orderSaga.AssignedAgv);
            Assert.AreEqual(fixture.SagaHarness.StateMachine.OrderFailed.Name, orderSaga.CurrentState);

            orderEventObserver.Verify(s => s.PreExecute(It.IsAny<BehaviorContext<OrderSaga, AgvAssignmentFailed>>()), Times.Once);
            orderEventObserver.Verify(s => s.PostExecute(It.IsAny<BehaviorContext<OrderSaga, AgvAssignmentFailed>>()), Times.Once);
        }

        [TestMethod]
        public async Task When_AgvAssignmentFailed_NoFreeAgv_OrderStateIsOrderReceived()
        {
            var testFixtureObject = new TestFixture();
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();
            const string orderId = "Order123";
            var correlationId = orderId.ToGuid();
            var orderSaga = new OrderSaga(
                    correlationId,
                    new OrderDetails(correlationId, orderId, "s1", "d2", "leno1", HostOrderCreator.Simulation)
                )
            { CurrentState = fixture.Machine.AgvRequested.Name };

            fixture.Add(orderSaga);

            await fixture.TestHarness.Bus.Publish(new AgvAssignmentFailed
                (
                    orderSaga.CorrelationId,
                    AgvAssignmentFailed.ErrorCode.NoIdleAgv
                )
            );
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvAssignmentFailed>());

            Assert.IsNull(orderSaga.AssignedAgv);
            Assert.AreEqual(fixture.SagaHarness.StateMachine.OrderReceived.Name, orderSaga.CurrentState);
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
            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<TransportOrderCompleted>());

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(fixture.SagaHarness.StateMachine.Final.Name, saga.CurrentState);

            IPublishedMessage<HostOrderHandlingSucceeded>? message = fixture.TestHarness.Published.Select<HostOrderHandlingSucceeded>().First();
            Assert.IsNotNull(message);
            Assert.AreEqual(correlationId, message.Context.Message.CorrelationId);
            Assert.AreEqual(orderId, message.Context.Message.OrderId);
            Assert.IsTrue(await fixture.TestHarness.Published.Any<ReleaseAgv>());

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
            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<TransportOrderFailed>());

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(fixture.SagaHarness.StateMachine.OrderFailed.Name, saga.CurrentState);

            orderEventObserver.Verify(s => s.PreExecute(It.IsAny<BehaviorContext<OrderSaga, TransportOrderFailed>>()), Times.Once);
            orderEventObserver.Verify(s => s.PostExecute(It.IsAny<BehaviorContext<OrderSaga, TransportOrderFailed>>()), Times.Once);
        }

        [DataRow(nameof(OrderStateMachine.OrderReceived), nameof(OrderStateMachine.OrderReceived))]
        [DataRow(nameof(OrderStateMachine.AgvRequested), nameof(OrderStateMachine.AgvRequested))]
        [DataRow(nameof(OrderStateMachine.TransportOrderSend), nameof(OrderStateMachine.TransportOrderSend))]
        [DataRow(nameof(OrderStateMachine.TransportOrderStarted), nameof(OrderStateMachine.TransportOrderStarted))]
        [DataRow(nameof(OrderStateMachine.TransportOrderPicked), nameof(OrderStateMachine.TransportOrderPicked))]
        [DataRow(nameof(OrderStateMachine.OrderFailed), nameof(OrderStateMachine.OrderReceived))]
        [DataRow(nameof(OrderStateMachine.Final), nameof(OrderStateMachine.OrderReceived))]
        [TestMethod]
        public async Task Handle_TransportOrderReset_Message(string previousState, string resultState)
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

            await fixture.TestHarness.Bus.Publish(new TransportOrderReset(correlationId));
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<TransportOrderReset>());

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(resultState, saga.CurrentState);
        }

        /// <summary>
        /// Examining HostOrderStarted message and state transition after consuming TransportOrderStarted message
        /// </summary>
        [DataRow("AGV_01", true, "TransportOrderStarted")]
        [DataRow("AGV_02", false, "TransportOrderSend")]
        [TestMethod]
        public async Task Handling_Consumption_of_TransportOrderStarted(string agvId, bool hostOrderStartedHasBeenSent, string expectedState)
        {
            // Arrange
            var agvSaga = new AgvSaga(agvId.ToGuid(), agvId, "Discovered");

            var testFixtureObject = new TestFixture(agvSaga);
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();

            string orderId = "Order123";
            var correlationId = orderId.ToGuid();
            string agvWhichIsSupposedToHandleTheOrder = "AGV_01";

            var orderSaga = new OrderSaga(
                correlationId,
                new OrderDetails(correlationId, orderId, "S1", "D1", "012345", HostOrderCreator.Wms),
                fixture.Machine.TransportOrderSend.Name,
                agvWhichIsSupposedToHandleTheOrder.ToGuid()
                );

            fixture.Add(orderSaga);

            Mock<IEventObserver<OrderSaga>> orderEventObserver = testFixtureObject.GetOrderEventObserverMock();

            // Act
            await fixture.TestHarness.Bus.Publish(new TransportOrderStarted(correlationId, orderId));

            // Assert
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<TransportOrderStarted>());
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(expectedState, saga.CurrentState);
            Assert.AreEqual(hostOrderStartedHasBeenSent, await fixture.TestHarness.Published.Any<HostOrderStarted>());

            orderEventObserver.Verify(s => s.PreExecute(It.IsAny<BehaviorContext<OrderSaga, TransportOrderStarted>>()), Times.Once);
            orderEventObserver.Verify(s => s.PostExecute(It.IsAny<BehaviorContext<OrderSaga, TransportOrderStarted>>()), Times.Once);
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
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<TransportOrderPickCompleted>());
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(fixture.Machine.TransportOrderPicked.Name, saga.CurrentState);

            orderEventObserver.Verify(s => s.PreExecute(It.IsAny<BehaviorContext<OrderSaga, TransportOrderPickCompleted>>()), Times.Once);
            orderEventObserver.Verify(s => s.PostExecute(It.IsAny<BehaviorContext<OrderSaga, TransportOrderPickCompleted>>()), Times.Once);
        }

        [TestMethod]
        public async Task No_HostOrderStarted_Message_Is_Sent_After_Consuming_TransportOrderStarted()
        {
            // Arrange
            var testFixtureObject = new TestFixture();
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();
            string orderId = "Order123";
            var correlationId = orderId.ToGuid();

            var orderSaga = new OrderSaga(
                correlationId,
                new OrderDetails(correlationId, orderId, "S1", "D1", "012345", HostOrderCreator.Wms),
                fixture.Machine.TransportOrderSend.Name,
                null
            );

            fixture.Add(orderSaga);

            // Act
            await fixture.TestHarness.Bus.Publish(new TransportOrderStarted(correlationId, orderId));

            // Assert
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<TransportOrderStarted>());
            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(fixture.Machine.TransportOrderSend.Name, saga.CurrentState);
            Assert.IsFalse(await fixture.TestHarness.Published.Any<HostOrderStarted>());
        }

        /// <summary>
        /// Checks the outgoing FailedReason strings after consuming the given AgvAssignmentErrorCode
        /// </summary>
        [DataRow(AgvAssignmentFailed.ErrorCode.AgvRegistryEmpty)]
        [DataRow(AgvAssignmentFailed.ErrorCode.NoIdleAgv)]
        [DataRow(AgvAssignmentFailed.ErrorCode.NoRouteFound)]
        [TestMethod]
        public async Task AgvAssignmentErrorCode_EnumElements_Corresponds_to_its_string(AgvAssignmentFailed.ErrorCode sentErrorCode)
        {
            // Arrange
            var agvSaga = CreateTestAgvSaga(It.IsAny<string>());
            var testFixtureObject = new TestFixture(agvSaga);
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();
            var order = CreateTestOrderSaga(fixture.Machine.AgvRequested.Name);
            fixture.Add(order);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvAssignmentFailed(order.CorrelationId, sentErrorCode));

            // Assert
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvAssignmentFailed>());
            Assert.AreEqual(order.CorrelationId, await fixture.SagaHarness.Exists(order.CorrelationId));
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == order.CorrelationId).First().Saga;
            string expectedFailedReason = $"{nameof(AgvAssignmentFailed)}.{sentErrorCode}";
            Assert.AreEqual(expectedFailedReason, saga.FailedReason);
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
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<TransportOrderFailed>());
            Assert.AreEqual(order.CorrelationId, await fixture.SagaHarness.Exists(order.CorrelationId));
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == order.CorrelationId).First().Saga;
            Assert.AreEqual(nameof(TransportOrderFailed), saga.FailedReason);
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
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<TransportOrderCompleted>());

            mockObserver.Verify(s => s.StateChanged(It.IsAny<BehaviorContext<OrderSaga>>(), It.IsAny<State>(), It.IsAny<State>()), Times.Once);
        }

        /// <summary>
        /// AgvRegistry contains no AGVS despite inbound transport order
        /// AgvAssignmentFailed telegram is sent
        /// Checks if FailedReason and AgvAssignmentErrorCode are set correctly
        /// </summary>
        [TestMethod]
        public async Task No_AGV_found_in_Registry_then_OrderState_set_back_to_OrderReceived()
        {
            // Arrange
            var testFixtureObject = new TestFixture();
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();
            var order = CreateTestOrderSaga(fixture.Machine.AgvRequested.Name);
            fixture.Add(order);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvAssignmentFailed(order.CorrelationId, AgvAssignmentFailed.ErrorCode.AgvRegistryEmpty));

            // Assert
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvAssignmentFailed>());
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == order.CorrelationId).First().Saga;
            Assert.AreEqual($"{nameof(AgvAssignmentFailed)}.{AgvAssignmentFailed.ErrorCode.AgvRegistryEmpty}", saga.FailedReason);
            Assert.AreEqual(fixture.Machine.OrderReceived.Name, saga.CurrentState);
        }

        /// <summary>
        /// AgvRegistry gets a new AGV but there is no suitable route for given order
        /// AgvAssignmentFailed telegram is sent
        /// Checks if FailedReason and AgvAssignmentErrorCode are set correctly
        /// </summary>
        [TestMethod]
        public async Task Incoming_AGV_but_NoRouteFound_with_given_orders()
        {
            // Arrange
            var agvSaga = CreateTestAgvSaga(It.IsAny<string>());
            var testFixtureObject = new TestFixture(agvSaga);
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = testFixtureObject.GetTestFixture();
            var order = CreateTestOrderSaga(fixture.Machine.AgvRequested.Name);
            fixture.Add(order);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvAssignmentFailed(order.CorrelationId, AgvAssignmentFailed.ErrorCode.NoRouteFound));

            // Assert
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvAssignmentFailed>());
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == order.CorrelationId).First().Saga;
            Assert.AreEqual($"{nameof(AgvAssignmentFailed)}.{AgvAssignmentFailed.ErrorCode.NoRouteFound}", saga.FailedReason);
            Assert.AreEqual(fixture.Machine.OrderFailed.Name, saga.CurrentState);
        }

        /// <summary>
        /// Checks the message sending according to each state after incoming CancelTransportOrder
        /// </summary>
        [DataRow(nameof(OrderStateMachine.OrderReceived), null)]
        [DataRow(nameof(OrderStateMachine.Cancelled), HostOrderCancelFailed.ErrorCode.OrderAlreadyCancelled)]
        [DataRow(nameof(OrderStateMachine.Final), HostOrderCancelFailed.ErrorCode.OrderAlreadyFinished)]
        [DataRow(nameof(OrderStateMachine.Initial), HostOrderCancelFailed.ErrorCode.OrderDoesNotExist)]
        [DataRow(nameof(OrderStateMachine.OrderFailed), HostOrderCancelFailed.ErrorCode.OrderAlreadyFailed)]
        [DataRow(nameof(OrderStateMachine.AgvRequested), HostOrderCancelFailed.ErrorCode.OrderAlreadyStarted)]
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
            Assert.IsTrue(await fixture.TestHarness.Published.Any<CancelHostOrder>());
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<CancelHostOrder>());

            if (previousState == nameof(OrderStateMachine.OrderReceived))
            {
                Assert.IsTrue(await fixture.TestHarness.Published.Any<HostOrderCancelSucceeded>());
                var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == order.CorrelationId).First().Saga;
                Assert.AreEqual(nameof(OrderStateMachine.Cancelled), saga.CurrentState);
            }
            else
            {
                Assert.IsTrue(await fixture.TestHarness.Published.Any<HostOrderCancelFailed>());
                var msg = fixture.TestHarness.Published.Select<HostOrderCancelFailed>().First().Context.Message;
                Assert.AreEqual(errorCode, msg.Error);
            }
        }
    }
}
