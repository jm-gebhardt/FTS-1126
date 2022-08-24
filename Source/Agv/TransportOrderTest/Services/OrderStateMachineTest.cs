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
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    /// <summary>
    /// Test class dedicated to the OrderStateMachine
    /// </summary>
    [TestClass]
    public class OrderStateMachineTest
    {
        #region Setup
        /// <summary>
        /// Create OrderStateMachine test fixture with mocked services
        /// </summary>
        private StateMachineTestFixture<OrderStateMachine, OrderSaga> GetFixture(Mock<IAgvRegistry>? mockRegistry = null)
        {
            if (mockRegistry == null)
            {
                mockRegistry = new Mock<IAgvRegistry>();
                mockRegistry.Setup(s => s.GetIdleAgvs()).Returns(new List<IAgv>());
            }

            var mockObserver = new Mock<IStateObserver<OrderSaga>>();

            return new StateMachineTestFixture<OrderStateMachine, OrderSaga>(services: s =>
            {
                s.AddTransient(_ => mockRegistry.Object);
                s.AddSingleton(_ => mockObserver.Object);
            });
        }

        private OrderSaga CreateTestOrderSaga(string previousState)
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
        public async Task When_HostOrderReceived_OrderReceived_RequestOrderAgvAssign_Published()
        {
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = GetFixture();

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

        /// <summary>
        /// When HandleHostOrder is received with empty CorrelationId no saga created
        /// </summary>
        [TestMethod]
        public async Task When_HandleHostOrder_EmptyCorrelationId_NoSagaCreated()
        {
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = GetFixture();

            const string orderId = "Order123123";
            var correlationId = orderId.ToGuid();
            var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new HandleHostOrder(correlationId, orderDetails));

            Assert.IsTrue(await fixture.TestHarness.Published.Any<HandleHostOrder>());
            Assert.IsNull(await fixture.SagaHarness.NotExists(Guid.Empty, TimeSpan.FromMilliseconds(10)));
        }

        /// <summary>
        /// When HandleHostOrder received with CorrelationId keeps provided CorrelationId
        /// </summary>
        [TestMethod]
        public async Task When_HandleHostOrderWithCorrelationId_KeepsProvidedCorrelationId()
        {
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = GetFixture();

            const string orderId = "Order123123";
            var correlationId = Guid.NewGuid();
            var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new HandleHostOrder(correlationId, orderDetails));
            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
        }

        /// <summary>
        /// When AgvAssignSucceeded received ExecuteTransportOrder requested
        /// </summary>
        [TestMethod]
        public async Task When_AgvAssignSucceeded_ExecuteTransportOrderRequested()
        {
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = GetFixture();

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
        }

        /// <summary>
        /// When AgvAssignmentFailed with NoRouteFound received HostOrderHandlingFailed is published
        /// </summary>
        [TestMethod]
        public async Task When_AgvAssignmentFailed_NoRouteFound_HostOrderHandlingFailedPublished()
        {
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = GetFixture();
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
                    AgvAssignmentErrorCode.NoRouteFound
                )
            );
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvAssignmentFailed>());
            Assert.IsTrue(await fixture.TestHarness.Published.Any<HostOrderHandlingFailed>());

            var message = fixture.TestHarness.Published.Select<HostOrderHandlingFailed>().First().Context.Message;
            Assert.AreEqual(correlationId, message.CorrelationId);
            Assert.AreEqual(HostOrderErrorCode.InvalidOrderData, message.Error);

            Assert.IsNull(orderSaga.AssignedAgv);
            Assert.AreEqual(fixture.SagaHarness.StateMachine.OrderFailed.Name, orderSaga.CurrentState);
        }

        /// <summary>
        /// When AgvAssignmentFailed with NoIdleAgv received HostOrderHandlingFailed is published
        /// </summary>
        [TestMethod]
        public async Task When_AgvAssignmentFailed_NoFreeAgv_OrderStateIsOrderReceived()
        {
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = GetFixture();
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
                    AgvAssignmentErrorCode.NoIdleAgv
                )
            );
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvAssignmentFailed>());

            Assert.IsNull(orderSaga.AssignedAgv);
            Assert.AreEqual(fixture.SagaHarness.StateMachine.OrderReceived.Name, orderSaga.CurrentState);
        }

        /// <summary>
        /// TransportOrderCompleted results in HostOrderHandlingSucceededPublished
        /// </summary>
        [TestMethod]
        public async Task When_TransportOrderCompleted_HostOrderHandlingSucceededPublished()
        {
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = GetFixture();

            string orderId = Guid.NewGuid().ToString();
            var correlationId = orderId.ToGuid();

            var orderState = new OrderSaga(
                correlationId,
                new OrderDetails(correlationId, orderId, "s1", "d2", "leno1", HostOrderCreator.Simulation)
            )
            { CurrentState = fixture.Machine.TransportOrderPicked.Name };

            fixture.Add(orderState);
            await fixture.TestHarness.Bus.Publish(new TransportOrderCompleted(correlationId, orderId));
            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<TransportOrderCompleted>());

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(fixture.SagaHarness.StateMachine.Final.Name, saga.CurrentState);

            IPublishedMessage<HostOrderHandlingSucceeded>? message = fixture.TestHarness.Published.Select<HostOrderHandlingSucceeded>().First();
            Assert.IsNotNull(message);
            Assert.AreEqual(correlationId, message.Context.Message.CorrelationId);
            Assert.AreEqual(orderId, message.Context.Message.OrderId);
        }

        /// <summary>
        /// TransportOrderFailed results in saga state change to TransportOrderFailed
        /// </summary>
        [DataRow("TransportOrderSend")]
        [DataRow("TransportOrderPicked")]
        [TestMethod]
        public async Task When_TransportOrderFailed_HostOrderHandlingFailedPublished(string previousState)
        {
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = GetFixture();

            string orderId = Guid.NewGuid().ToString();
            var correlationId = orderId.ToGuid();

            var orderState = new OrderSaga(
                correlationId,
                new OrderDetails(correlationId, orderId, "s1", "d2", "leno1", HostOrderCreator.Simulation)
            )
            { CurrentState = previousState };

            fixture.Add(orderState);
            await fixture.TestHarness.Bus.Publish(new TransportOrderFailed(correlationId, orderId, TransportOrderErrorCode.Any));
            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<TransportOrderFailed>());

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(fixture.SagaHarness.StateMachine.OrderFailed.Name, saga.CurrentState);
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
            var mockRegistry = new Mock<IAgvRegistry>();
            mockRegistry.Setup(s => s.GetAgv(agvId.ToGuid()))
                .Returns(new AgvSaga(agvId.ToGuid(), agvId, "Discovered"));

            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = GetFixture(mockRegistry);

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

            // Act
            await fixture.TestHarness.Bus.Publish(new TransportOrderStarted(correlationId, orderId));

            // Assert
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<TransportOrderStarted>());
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(expectedState, saga.CurrentState);
            Assert.AreEqual(hostOrderStartedHasBeenSent, await fixture.TestHarness.Published.Any<HostOrderStarted>());
        }

        /// <summary>
        /// Examining state transition after consuming TransportOrderPickCompleted message
        /// </summary>
        [TestMethod]
        public async Task Handling_Consumption_of_TransportOrderPickCompleted()
        {
            // Arrange
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = GetFixture();
            string orderId = "Order123";
            var correlationId = orderId.ToGuid();

            var orderSaga = new OrderSaga(
                correlationId,
                new OrderDetails(correlationId, orderId, "S1", "D1", "012345", HostOrderCreator.Wms),
                fixture.Machine.TransportOrderStarted.Name,
                "AGV_01".ToGuid()
            );

            fixture.Add(orderSaga);

            // Act
            await fixture.TestHarness.Bus.Publish(new TransportOrderPickCompleted(correlationId, orderId));

            // Assert
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<TransportOrderPickCompleted>());
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(fixture.Machine.TransportOrderPicked.Name, saga.CurrentState);
        }

        /// <summary>
        /// No HostOrderStarted message is sent after consuming TransportOrderStarted message
        /// </summary>
        [TestMethod]
        public async Task No_HostOrderStarted_Message_Is_Sent_After_Consuming_TransportOrderStarted()
        {
            // Arrange
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = GetFixture();
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
        [DataRow(AgvAssignmentErrorCode.NoIdleAgv,  $"{nameof(AgvAssignmentErrorCode)}.NoIdleAgv")]
        [DataRow(AgvAssignmentErrorCode.NoRouteFound, $"{nameof(AgvAssignmentErrorCode)}.NoRouteFound")]
        [TestMethod]
        public async Task AgvAssignmentErrorCode_EnumElements_Corresponds_to_its_string(AgvAssignmentErrorCode sentErrorCode, string expectedErrorCodeInOrderSaga)
        {
            // Arrange
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = GetFixture();
            var order = CreateTestOrderSaga(fixture.Machine.AgvRequested.Name);
            fixture.Add(order);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvAssignmentFailed(order.CorrelationId, sentErrorCode));

            // Assert
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvAssignmentFailed>());
            Assert.AreEqual(order.CorrelationId, await fixture.SagaHarness.Exists(order.CorrelationId));
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == order.CorrelationId).First().Saga;
            Assert.AreEqual(expectedErrorCodeInOrderSaga, saga.FailedReason );
        }
        
        /// <summary>
        /// Checks the outgoing FailedReason strings after consuming the given TransportOrderErrorCode
        /// </summary>
        [TestMethod]
        [DataRow("TransportOrderSend", TransportOrderErrorCode.AbortedByUser, "AbortedByUser")]
        [DataRow("TransportOrderSend", TransportOrderErrorCode.Any, "Any")]
        [DataRow("TransportOrderPicked", TransportOrderErrorCode.AbortedByUser, "AbortedByUser")]
        [DataRow("TransportOrderPicked", TransportOrderErrorCode.Any, "Any")]
        public async Task TransportOrderErrorCode_EnumElements_Corresponds_to_its_string(string previousState, TransportOrderErrorCode sentErrorCode, string expectedErrorCodeInOrderSaga)
        {
            // Arrange
            StateMachineTestFixture<OrderStateMachine, OrderSaga> fixture = GetFixture();
            var order = CreateTestOrderSaga(previousState);
            fixture.Add(order);

            // Act
            await fixture.TestHarness.Bus.Publish(new TransportOrderFailed(order.CorrelationId, order.OrderDetails.OrderId, sentErrorCode));

            // Assert
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<TransportOrderFailed>());
            Assert.AreEqual(order.CorrelationId, await fixture.SagaHarness.Exists(order.CorrelationId));
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == order.CorrelationId).First().Saga;
            Assert.AreEqual(expectedErrorCodeInOrderSaga, saga.FailedReason);
        }

        /// <summary>
        /// When OrderSaga state has changed check if observer was triggered
        /// </summary>
        [TestMethod]
        public async Task When_OrderSaga_State_Changed_Trigger_Observer()
        {
            var mockRegistry = new Mock<IAgvRegistry>();
            mockRegistry.Setup(s => s.GetIdleAgvs()).Returns(new List<IAgv>());
            var mockObserver = new Mock<IStateObserver<OrderSaga>>();

            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>(services: s =>
            {
                s.AddTransient(_ => mockRegistry.Object);
                s.AddSingleton(_ => mockObserver.Object);
            });

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
    }
}
