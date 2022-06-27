using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
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
        /// 
        /// </summary>
        [TestMethod]
        public async Task When_HostOrderReceived_AgvIsRequested()
        {
            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>();
            var correlationId = Guid.NewGuid();

            await fixture.TestHarness.Bus.Publish<IHandleHostOrder>(
                new
                {
                    correlationId,
                    Order = new Order("Order#1",  "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation)
                }
            );

            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<IHandleHostOrder>());

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(fixture.SagaHarness.StateMachine.AgvRequested, saga.CurrentState);
            Assert.IsNull(saga.AssignedAgv);
            Assert.IsNotNull(saga.Order.OrderId);
            Assert.IsNotNull(saga.Order.Source);
            Assert.IsNotNull(saga.Order.Destination);
            Assert.IsTrue(await fixture.TestHarness.Published.Any<IAssignAgv>());
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public async Task When_HandleHostOrder_EmptyCorrelationId_NoSagaCreated()
        {
            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>();
            string orderId = "Order123123";
            await fixture.TestHarness.Bus.Publish<IHandleHostOrder>(
                new
                {
                    Order = new Order(orderId,  "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation)
                }
            );

            Assert.IsNull(await fixture.SagaHarness.NotExists(Guid.Empty, TimeSpan.FromMilliseconds(10)));
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public async Task When_HandleHostOrderWithCorrelationId_KeepsProvidedCorrelationId()
        {
            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>();
            string orderId = "Order123123";
            var correlationId = Guid.NewGuid();
            await fixture.TestHarness.Bus.Publish<IHandleHostOrder>(new {
                    correlationId,
                    Order = new Order(orderId,  "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation)
            });
            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public async Task When_AgvAssignSucceeded_ExecuteTransportOrderRequested()
        {
            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>();
            var orderSaga = new OrderSaga(
                    Guid.NewGuid(),
                new Order("Order123", "s1", "d2", "leno1", HostOrderCreator.Simulation)
                ) 
                { CurrentState = fixture.Machine.AgvRequested};

            fixture.Add(orderSaga);

            const string assignedAgv = "AGV1";
            await fixture.TestHarness.Bus.Publish<IAgvAssignSucceeded>(
                new
                {
                    orderSaga.CorrelationId,
                    orderSaga.Order.OrderId,
                    AssignedAgv = assignedAgv
                }
            );
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<IAgvAssignSucceeded>());
            
            Assert.AreEqual(fixture.SagaHarness.StateMachine.TransportOrderSend, orderSaga.CurrentState);
            Assert.AreEqual(assignedAgv, orderSaga.AssignedAgv);
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
            var correlationId = Guid.NewGuid();
            string orderId = Guid.NewGuid().ToString();

            var orderState = new OrderSaga(
                correlationId,
                new Order(orderId, "s1", "d2", "leno1", HostOrderCreator.Simulation)
                ) 
                { CurrentState = fixture.Machine.TransportOrderSend };

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
            Assert.AreEqual(fixture.SagaHarness.StateMachine.Final, saga.CurrentState);
            
            IPublishedMessage<IHostOrderHandlingSucceeded>? message = fixture.TestHarness.Published.Select<IHostOrderHandlingSucceeded>().First();
            Assert.IsNotNull(message);
            Assert.AreEqual(correlationId, message.Context.Message.CorrelationId);
            Assert.AreEqual(orderId, message.Context.Message.OrderId);
        }
    }
}