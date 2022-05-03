using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands;
using Gebhardt.StoreWare.Agv.WCSCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Services
{
    /// <summary>
    /// Test for WorkerHostOrderSim
    /// </summary>
    [TestClass]
    public class SimulationHostOrderServiceTest
    {
        /// <summary>
        /// Test if backgroundservice WorkerHostOrderSim can be started and stopped
        /// </summary>
        [TestMethod]
        public async Task StartStopBackgroundService()
        {
            var mockLogger = new Mock<ILogger<SimulationHostOrderService>>();
            var mockBus = new Mock<IBus>();
            var options = new SimulateOptions()
            {
                Interval = 0, // Use interval 0 to skip delay
                Sources = new[] { "S01", "S02", "S03" },
                Destinations = new[] { "D01", "D02", "D03" }
            };

            var workerHostOrderSim = new SimulationHostOrderService(mockLogger.Object, mockBus.Object, options);
            var source = new CancellationTokenSource();

            source.CancelAfter(1); // Test canceling worker after 1ms
            await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => workerHostOrderSim.StartAsync(source.Token));

            // Since we pass an anonymous type into Publish, we need to use 'IsAny<object>' for the first para
            mockBus.Verify(w => w.Publish<IHandleHostOrder>(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);

            mockBus.Reset();

            source.Cancel(); // Additional test for exiting while loop with canceled token
            await workerHostOrderSim.StartAsync(source.Token);

            // Loop is never run, so Publish should never be called
            mockBus.Verify(w => w.Publish<IHandleHostOrder>(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Test if WorkerHostOrderSim can publish simulated HostOrders
        /// </summary>
        [TestMethod]
        public async Task SimulatedPublishHostOrder()
        {
            var fixture = new BackgroundServiceTestFixture<SimulationHostOrderService>(services =>
            {
                services.AddSingleton(_ => new SimulateOptions()
                {
                    Interval = 60,
                    Sources = new[] { "S01", "S02", "S03" },
                    Destinations = new[] { "D01", "D02", "D03" }
                });
            });

            Assert.IsTrue(await fixture.TestHarness.Published.Any<IHandleHostOrder>());
        }
    }
}