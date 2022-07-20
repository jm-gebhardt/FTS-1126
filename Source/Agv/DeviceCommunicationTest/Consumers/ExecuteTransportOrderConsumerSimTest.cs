using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers
{
    /// <summary>
    /// Test consumer for transport orders..
    /// </summary>
    [TestClass]
    public class ExecuteTransportOrderConsumerSimTest
    {
        /// <summary>
        /// Test if consumer responds to transport order.
        /// </summary>
        [TestMethod]
        public async Task SimulatedReceivePublishTransportOrder()
        {
            var fixture = new MassTransitTestFixture<SimulationExecuteTransportOrderConsumer>(services =>
            {
                services.AddSingleton(_ => new SimulateOptions() { Interval = new[] { 1, 5 } });
            });

            const string orderId = "TestOrder123";
            var correlationId = orderId.ToGuid();
            const string agvId = "AGV_0";
            var agvCorrelationId = agvId.ToGuid();

            var order = new Order(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);
            await fixture.TestHarness.Bus.Publish(new ExecuteTransportOrder(correlationId, order, agvId, agvCorrelationId));

            Assert.IsTrue(await fixture.TestHarness.Published.Any<ExecuteTransportOrder>());
            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<ExecuteTransportOrder>());

            var message = fixture.TestHarness.Published.Select<TransportOrderCompleted>().First().Context.Message;
            Assert.AreEqual(orderId, message.OrderId);
            Assert.AreEqual(correlationId, message.CorrelationId);
        }
    }
}
