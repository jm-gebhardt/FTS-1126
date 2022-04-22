using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages.Agv.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Agv.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
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
                    OrderId = "",
                    Source = "SRC1",
                    Destination = "DST1",
                    LeNo = "LeNo123",
                    Creator = HostOrderCreator.Simulation
                }
            );

            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<IHandleHostOrder>());

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(fixture.SagaHarness.StateMachine.AgvRequested, saga.CurrentState);
            Assert.IsNull(saga.AssignedAgv);
            Assert.IsNotNull(saga.Source);
            Assert.IsNotNull(saga.Destination);
            Assert.IsTrue(await fixture.TestHarness.Published.Any<IAssignAgv>());
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public async Task When_HandleHostOrderWithoutCorrelationId_NewCorrelationIdAssignedIsAssigned()
        {
            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>();
            string orderId = "Order123123";
            await fixture.TestHarness.Bus.Publish<IHandleHostOrder>(
                new
                {
                    OrderId = orderId,
                    Source = "SRC1",
                    Destination = "DST1",
                    LeNo = "LeNo123",
                    Creator = HostOrderCreator.Simulation
                }
            );

            Assert.IsNotNull(await fixture.SagaHarness.Match(o => o.OrderId == orderId));
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public async Task When_HandleHostOrderWithCorrelationId_KeepsProvidedCorrelationId()
        {
            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>();
            string orderId = "Order123123";
            var guid = Guid.NewGuid();
            await fixture.TestHarness.Bus.Publish<IHandleHostOrder>(
                new
                {
                    CorrelationId = guid,
                    OrderId = orderId,
                    Source = "SRC1",
                    Destination = "DST1",
                    LeNo = "LeNo123",
                    Creator = HostOrderCreator.Simulation
                }
            );
            Assert.AreEqual(guid, await fixture.SagaHarness.Exists(guid));
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public async Task When_AgvAssignSucceeded_ExecuteTransportOrderRequested()
        {
            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>();
            var orderSaga = new OrderSaga
            {
                CorrelationId = Guid.NewGuid(),
                CurrentState = fixture.Machine.AgvRequested,
                OrderId = "Order123"
            };

            fixture.Add(orderSaga);

            const string assignedAgv = "AGV1";
            await fixture.TestHarness.Bus.Publish<IAgvAssignSucceeded>(
                new
                {
                    orderSaga.CorrelationId,
                    orderSaga.OrderId,
                    AssignedAgv = assignedAgv
                }
            );
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<IAgvAssignSucceeded>());

            Assert.AreEqual(fixture.SagaHarness.StateMachine.TransportOrderSend, orderSaga.CurrentState);
            Assert.AreEqual(assignedAgv, orderSaga.AssignedAgv);
            Assert.IsTrue(await fixture.TestHarness.Published.Any<IExecuteTransportOrder>());
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public async Task When_TransportOrderCompleted_HostOrderHandlingSucceededPublished()
        {
            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>();
            var correlationId = Guid.NewGuid();
            var orderId = Guid.NewGuid();

            var orderState = new OrderSaga
            {
                CorrelationId = correlationId,
                CurrentState = fixture.Machine.TransportOrderSend
            };

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
        }
    }
}