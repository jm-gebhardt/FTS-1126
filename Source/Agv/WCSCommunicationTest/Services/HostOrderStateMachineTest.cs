using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;
using Gebhardt.StoreWare.Agv.WCSCommunication.States;
using Gebhardt.StoreWare.Agv.WCSCommunication.Messages;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Services
{
    [TestClass]
    public class HostOrderStateMachineTest
    {
        /// <summary>
        /// When HostOrder received agv Is requested
        /// </summary>
        [TestMethod]
        public async Task WhenCreateHostOrderReceived_HostOrderSagaCreated()
        {
            var fixture = new StateMachineTestFixture<HostOrderStateMachine, HostOrderSaga>();

            const string orderId = "Order#01";
            var correlationId = orderId.ToGuid();
            var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new CreateHostOrder(orderDetails));
            (await fixture.SagaHarness.Consumed.Any<CreateHostOrder>()).ShouldBeTrue();

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;

            saga.CurrentState.ShouldBe("Initial");
            saga.OrderDetails.CorrelationId.ShouldBe(correlationId);
            saga.OrderDetails.OrderId.ShouldBe(orderId);
        }
    }
}
