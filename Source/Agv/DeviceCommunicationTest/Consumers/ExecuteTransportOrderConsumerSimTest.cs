using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
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
            var fixture = new ConsumerTestFixture<SimulationExecuteTransportOrderConsumer>(services =>
            {
                services.AddSingleton(_ => new SimulateOptions() { Interval = new[] { 1, 5 } });
            });

            string orderId = "TestOrder123";
            var correlationId = orderId.ToGuid();
            await fixture.TestHarness.Bus.Publish<IExecuteTransportOrder>(new
            {
                Order = new Order(correlationId, orderId,  "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation),
                AssignedAgv = "AGV_0"
            });

            Assert.IsTrue(await fixture.TestHarness.Published.Any<IExecuteTransportOrder>());
            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<IExecuteTransportOrder>());
            Assert.AreEqual(orderId, fixture.TestHarness.Published.Select<ITransportOrderCompleted>().First().Context.Message.OrderId);
        }
    }
}
