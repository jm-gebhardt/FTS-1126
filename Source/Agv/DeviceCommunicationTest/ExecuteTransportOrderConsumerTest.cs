using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;
using MassTransit.Testing;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;
namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest
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
            await using var provider = new ServiceCollection()
                .AddScoped(_ => new SimulateOptions() {Interval = new[] {1, 5}})
                .AddMassTransitTestHarness(cfg => { cfg.AddConsumer<SimulationExecuteTransportOrderConsumer>(); })
                .BuildServiceProvider(true);

            var harness = provider.GetRequiredService<ITestHarness>();

            await harness.Start();

            string orderId = "TestOrder123";
            await harness.Bus.Publish<IExecuteTransportOrder>(new
            {
                OrderId = orderId,
                AssignedAgv = string.Empty,
                SourceId = string.Empty,
                DestinationId = string.Empty
            });

            Assert.IsTrue(await harness.Published.Any<IExecuteTransportOrder>());

            IConsumerTestHarness<SimulationExecuteTransportOrderConsumer> consumerHarness = harness.GetConsumerHarness<SimulationExecuteTransportOrderConsumer>();

            Assert.IsTrue(await consumerHarness.Consumed.Any<IExecuteTransportOrder>());

            Assert.AreEqual(orderId, harness.Published.Select<ITransportOrderCompleted>().First().Context.Message.OrderId);
        }
    }
}
