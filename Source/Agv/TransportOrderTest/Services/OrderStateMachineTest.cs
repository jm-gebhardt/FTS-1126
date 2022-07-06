using Gebhardt.StoreWare.Agv.Common.Definitions;
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

            await fixture.TestHarness.Bus.Publish<IHandleHostOrder>(
                new
                {
                    correlationId,
                    Order = new Order(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation)
                }
            );

            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<IHandleHostOrder>());

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(fixture.SagaHarness.StateMachine.OrderReceived.Name, saga.CurrentState);
            Assert.IsTrue(await fixture.TestHarness.Published.Any<RequestOrderAgvAssign>());
            Assert.IsNull(saga.AssignedAgv);
            Assert.IsNotNull(saga.Order.OrderId);
            Assert.IsNotNull(saga.Order.Source);
            Assert.IsNotNull(saga.Order.Destination);
            Assert.IsTrue(await fixture.TestHarness.Published.Any<IAssignAgv>());
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
            await fixture.TestHarness.Bus.Publish<IHandleHostOrder>(
                new
                {
                    Order = new Order(correlationId, orderId,  "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation)
                }
            );
            
            Assert.IsTrue(await fixture.TestHarness.Published.Any<IHandleHostOrder>());

            Assert.IsNull(await fixture.SagaHarness.NotExists(Guid.Empty, TimeSpan.FromMilliseconds(10)));
        }

        /// <summary>
        /// When HandleHostOrder received with CorrelationId keeps provided CorrelationId
        /// </summary>
        [TestMethod]
        public async Task When_HandleHostOrderWithCorrelationId_KeepsProvidedCorrelationId()
        {
            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>();
            string orderId = "Order123123";
            var correlationId = Guid.NewGuid();
            await fixture.TestHarness.Bus.Publish<IHandleHostOrder>(new {
                    correlationId,
                    Order = new Order(correlationId, orderId,  "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation)
            });
            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
        }

        /// <summary>
        /// When AgvAssignSucceeded received ExecuteTransportOrder requested
        /// </summary>
        [TestMethod]
        public async Task When_AgvAssignSucceeded_ExecuteTransportOrderRequested()
        {
            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>();
            const string orderId = "Order123";
            var correlationId = orderId.ToGuid();
            var orderSaga = new OrderSaga(
                    correlationId,
                new Order(correlationId, orderId, "s1", "d2", "leno1", HostOrderCreator.Simulation)
                ) 
                { CurrentState = fixture.Machine.AgvRequested.Name};

            fixture.Add(orderSaga);

            const string assignedAgvId = "AGV1";
            var assignedAgvCorrelationId = assignedAgvId.ToGuid();
            await fixture.TestHarness.Bus.Publish<IAgvAssignmentSucceeded>(
                new
                {
                    orderSaga.CorrelationId,
                    orderSaga.Order.OrderId,
                    AgvId = assignedAgvId,
                    AgvCorrelationId =  assignedAgvCorrelationId
                }
            );
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<IAgvAssignmentSucceeded>());
            
            Assert.AreEqual(fixture.SagaHarness.StateMachine.TransportOrderSend.Name, orderSaga.CurrentState);
            Assert.AreEqual(assignedAgvCorrelationId, orderSaga.AssignedAgv);
            Assert.IsTrue(await fixture.TestHarness.Published.Any<IExecuteTransportOrder>());
            Assert.IsTrue(await fixture.TestHarness.Published.Any<IHostOrderStarted>());
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
            await fixture.TestHarness.Bus.Publish<ITransportOrderCompleted>(
                new
                {
                    CorrelationId = correlationId,
                    OrderId = orderId
                }
            );
            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<ITransportOrderCompleted>());

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(fixture.SagaHarness.StateMachine.Final.Name, saga.CurrentState);
            
            IPublishedMessage<IHostOrderHandlingSucceeded>? message = fixture.TestHarness.Published.Select<IHostOrderHandlingSucceeded>().First();
            Assert.IsNotNull(message);
            Assert.AreEqual(correlationId, message.Context.Message.CorrelationId);
            Assert.AreEqual(orderId, message.Context.Message.OrderId);
        }
    }
}