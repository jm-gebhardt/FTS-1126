using Gebhardt.StoreWare.Agv.Common.Messages;
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
using MassTransit.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class OrderStateMachineTest
    {
        /// <summary>
        /// When HostOrder received agv Is requested
        /// </summary>
        [TestMethod]
        public async Task When_HostOrderReceived_OrderReceived_RequestOrderAgvAssign_Published()
        {
            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>();
            const string orderId = "Order#1";
            var correlationId = orderId.ToGuid();
            var order = new Order(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new HandleHostOrder(correlationId, order));
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;

            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<HandleHostOrder>());
            Assert.IsTrue(await fixture.TestHarness.Published.Any<RequestOrderAgvAssign>());

            Assert.IsNull(saga.AssignedAgv);
            Assert.IsNotNull(saga.Order.OrderId);
            Assert.IsNotNull(saga.Order.Source);
            Assert.IsNotNull(saga.Order.Destination);
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
            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>();
            const string orderId = "Order123123";
            var correlationId = orderId.ToGuid();
            var order = new Order(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new HandleHostOrder(correlationId, order));

            Assert.IsTrue(await fixture.TestHarness.Published.Any<HandleHostOrder>());
            Assert.IsNull(await fixture.SagaHarness.NotExists(Guid.Empty, TimeSpan.FromMilliseconds(10)));
        }

        /// <summary>
        /// When HandleHostOrder received with CorrelationId keeps provided CorrelationId
        /// </summary>
        [TestMethod]
        public async Task When_HandleHostOrderWithCorrelationId_KeepsProvidedCorrelationId()
        {
            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>();
            const string orderId = "Order123123";
            var correlationId = Guid.NewGuid();
            var order = new Order(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new HandleHostOrder(correlationId, order));
            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
        }

        /// <summary>
        /// When AgvAssignSucceeded received ExecuteTransportOrder requested
        /// </summary>
        [TestMethod]
        public async Task When_AgvAssignmentSucceeded_ExecuteTransportOrderRequested()
        {
            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>();
            const string orderId = "Order123";
            var correlationId = orderId.ToGuid();
            var orderSaga = new OrderSaga(
                correlationId,
                new Order(correlationId, orderId, "s1", "d2", "leno1", HostOrderCreator.Simulation)
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
            Assert.IsTrue(await fixture.TestHarness.Published.Any<HostOrderStarted>());
        }
        
        /// <summary>
        /// When AgvAssignmentFailed with NoRouteFound received HostOrderHandlingFailed is published
        /// </summary>
        [TestMethod]
        public async Task When_AgvAssignmentFailed_NoRouteFound_HostOrderHandlingFailedPublished()
        {
            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>();
            const string orderId = "Order123";
            var correlationId = orderId.ToGuid();
            var orderSaga = new OrderSaga(
                    correlationId,
                    new Order(correlationId, orderId, "s1", "d2", "leno1", HostOrderCreator.Simulation)
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
            Assert.AreEqual(HostOrderErrorCode.Any, message.Error);
            
            Assert.IsNull(orderSaga.AssignedAgv);
            Assert.AreEqual(fixture.SagaHarness.StateMachine.OrderFailed.Name, orderSaga.CurrentState);
        }
        
        /// <summary>
        /// When AgvAssignmentFailed with NoIdleAgv received HostOrderHandlingFailed is published
        /// </summary>
        [TestMethod]
        public async Task When_AgvAssignmentFailed_NoFreeAgv_OrderStateIsOrderReceived()
        {
            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>();
            const string orderId = "Order123";
            var correlationId = orderId.ToGuid();
            var orderSaga = new OrderSaga(
                    correlationId,
                    new Order(correlationId, orderId, "s1", "d2", "leno1", HostOrderCreator.Simulation)
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
            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>();

            string orderId = Guid.NewGuid().ToString();
            var correlationId = orderId.ToGuid();

            var orderState = new OrderSaga(
                correlationId,
                new Order(correlationId, orderId, "s1", "d2", "leno1", HostOrderCreator.Simulation)
            )
            { CurrentState = fixture.Machine.TransportOrderSend.Name };

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
        /// OrderFailed results in sage state change to OrderFailed
        /// </summary>
        [TestMethod]
        public async Task When_TransportOrderFailed_HostOrderHandlingFailedPublished()
        {
            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>();

            string orderId = Guid.NewGuid().ToString();
            var correlationId = orderId.ToGuid();

            var orderState = new OrderSaga(
                correlationId,
                new Order(correlationId, orderId, "s1", "d2", "leno1", HostOrderCreator.Simulation)
            )
            { CurrentState = fixture.Machine.TransportOrderSend.Name };

            fixture.Add(orderState);
            await fixture.TestHarness.Bus.Publish(new TransportOrderFailed(correlationId, orderId, TransportOrderErrorCode.Any));
            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<TransportOrderFailed>());

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(fixture.SagaHarness.StateMachine.OrderFailed.Name, saga.CurrentState);
        }
    }
}
